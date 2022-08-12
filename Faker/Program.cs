using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Npgsql;
using CommandLine;
using System.Diagnostics;
using static DrxFaker.DataGeneration;

namespace DrxFaker
{
    class Program
    {

        static string connectionString;
        public enum sqlTypes { Postgres, MS };
        public static sqlTypes sqlType;

        static void Main()
        {
            Parser.Default.ParseArguments<Options>(new List<string>() { "--help" });

            try
            {
                while (true)
                {
                    var args = Console.ReadLine().Trim().Split();
                    var parser = Parser.Default.ParseArguments<Options>(args)
                        .WithParsed(PreparationToStart);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError: {ex.Message}");
            }
        }

        /// <summary>
        /// Проверка введенных параметров и создание транзакции
        /// </summary>
        /// <param name="opts">Параметры передаваемые пользователем</param>
        static void PreparationToStart(Options opts)
        {
            var types = opts.GetType().GetProperties();
            foreach (var type in types)
            {
                if (type.PropertyType == typeof(string))
                    type.SetValue(opts, type.GetValue(opts).ToString().Trim('"'));
                else if (type.PropertyType == typeof(int) && (int)type.GetValue(opts) < 1)
                {
                    Console.WriteLine($"Error: {type.Name} should be more than 0");
                    return;
                }
            }

            if (opts.SqlType != "p" && opts.SqlType != "m")
            {
                Console.WriteLine("Error: Wrong type of Sql");
                return;
            }

            sqlType = opts.SqlType == "p" ? sqlTypes.Postgres : sqlTypes.MS;

            connectionString = sqlType == sqlTypes.Postgres ?
                $"Server={opts.Server};Port={opts.Port};Database={opts.Database};Uid={opts.UserId};Pwd={opts.Password}" :
                $"Server={opts.Server};Initial Catalog={opts.Database};User Id={opts.UserId};Password={opts.Password}";

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            if (sqlType == sqlTypes.Postgres)
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    var transaction = connection.BeginTransaction();
                    command.Connection = connection;
                    command.Transaction = transaction;
                    GenerationStart(opts.BusinessCount, opts.DepartmentsCount, opts.EmployeesCount, command, transaction);
                }
            else
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    var transaction = connection.BeginTransaction();
                    command.Connection = connection;
                    command.Transaction = transaction;
                    GenerationStart(opts.BusinessCount, opts.DepartmentsCount, opts.EmployeesCount, command, transaction);
                }

            stopWatch.Stop();
            var ts = stopWatch.Elapsed;
            var elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            Console.WriteLine($"\nRunTime {elapsedTime}\n");
        }

        /// <summary>
        /// Запуск генерации оргструктуры
        /// </summary>
        /// <param name="businessCount">Количество создаваемых организаций</param>
        /// <param name="departmentsCount">Количество создаваемых подразделений</param>
        /// <param name="employeesCount">Количество создаваемых сотрудников</param>
        static void GenerationStart(int businessCount, int departmentsCount, int employeesCount, object command, object transaction)
        {
            try
            {
                Console.WriteLine("\n\tCreation business units");
                var businesUnitsIds = GenerateBusinessUnits(businessCount, command);
                if (businesUnitsIds == null)
                {
                    throw new Exception("Error while creating Business Units");
                }

                Console.WriteLine("\tCreation departments");
                var departmentsIds = GenerateDepartments(departmentsCount, businesUnitsIds, command);
                if (departmentsIds == null)
                {
                    throw new Exception("Error while creating Departments");
                }

                var loops = employeesCount / 1000;
                for (var i = 0; i <= loops; i++)
                {
                    int count;
                    if (employeesCount < 1000)
                        count = employeesCount;
                    else if (employeesCount - 1000 * i < 1000)
                        count =  employeesCount - 1000 * i;
                    else
                        count = 1000;

                    if (count <= 0)
                        continue;

                    Console.WriteLine("\tCreation persons");
                    var persons = GeneratePersons(count, command);
                    if (persons == null)
                    {
                        throw new Exception("Error while creating Persons");
                    }

                    Console.WriteLine("\tCreation logins");
                    var loginsIds = GenerateLogins(count, persons, command);
                    if (loginsIds == null)
                    {
                        throw new Exception("Error while creating Logins");
                    }

                    Console.WriteLine("\tCreation employees");
                    var employees = GenerateEmployees(count, persons, loginsIds, departmentsIds, command);
                    if (loginsIds == null)
                    {
                        throw new Exception("Error while creating employees");
                    }

                    Console.WriteLine("\tCreation recipient links");
                    GenerateRecipientLink(employees, command);
                }

                if (sqlType == sqlTypes.Postgres)
                    (transaction as NpgsqlTransaction).Commit();
                else
                    (transaction as SqlTransaction).Commit();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError: {ex.Message}");

                try
                {
                    if (sqlType == sqlTypes.Postgres)
                        (transaction as NpgsqlTransaction).Rollback();
                    else
                        (transaction as SqlTransaction).Rollback();
                }
                catch (Exception ex2)
                {
                    Console.WriteLine("Rollback Exception Type: {0}", ex2.GetType());
                    Console.WriteLine("  Error: {0}", ex2.Message);
                }
            }
        }
    }
}
