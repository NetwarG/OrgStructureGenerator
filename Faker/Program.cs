using Bogus;
using Bogus.DataSets;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Npgsql;
using CommandLine;
using System.Reflection;
using System.Diagnostics;

namespace DrxFaker
{
    class Program
    {
        //postgresConnStr "Server=192.168.3.237;Port=5432;Database=directum;Uid=directum;Pwd=1Qwerty"
        //msConnStr "Server=192.168.1.82;Initial Catalog=S_NESTLE_RX3523_Dev_MAA;User Id=sa;Password=1Qwerty"
        //-t p -s "192.168.3.237" -d directum -u directum -p 1Qwerty --emp 10000 --bus 3 --dep 60

        static string connectionString;
        enum sqlTypes { Postgres, MS };
        static sqlTypes sqlType;

        static void Main()
        {
            Parser.Default.ParseArguments<Options>(new List<string>() { "--help" });

            bool keepLooping = true;
            while (keepLooping)
            {
                var args = Console.ReadLine().Split();
                var parser = Parser.Default.ParseArguments<Options>(args)
                    .WithParsed(GenerationStart);

                if (Console.ReadKey().Key == ConsoleKey.Escape)
                    keepLooping = false;
            }
        }

        /// <summary>
        /// Запуск генерации оргструктуры
        /// </summary>
        /// <param name="opts">Параметры передаваемые пользователем</param>
        static void GenerationStart(Options opts)
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
            try
            {
                Console.WriteLine("Creation business units");
                var businesUnitsIds = GenerateBusinessUnits(opts.BusinessCount);
                if (businesUnitsIds == null)
                {
                    Console.WriteLine("Error while creating Business Units");
                    return;
                }

                Console.WriteLine("Creation departments");
                var departmentsIds = GenerateDepartments(opts.DepartmentsCount, businesUnitsIds);
                if (departmentsIds == null)
                {
                    Console.WriteLine("Error while creating Departments");
                    return;
                }

                Console.WriteLine("Creation persons");
                var persons = GeneratePersons(opts.EmployeesCount);
                if (persons == null)
                {
                    Console.WriteLine("Error while creating Persons");
                    return;
                }

                Console.WriteLine("Creation logins");
                var loginsIds = GenerateLogins(opts.EmployeesCount, persons);
                if (loginsIds == null)
                {
                    Console.WriteLine("Error while creating Logins");
                    return;
                }

                Console.WriteLine("Creation employees");
                GenerateEmployees(opts.EmployeesCount, persons, loginsIds, departmentsIds);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError: {ex.Message}");
            }
            stopWatch.Stop();
            var ts = stopWatch.Elapsed;
            var elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            Console.WriteLine("RunTime " + elapsedTime);
        }

        #region Создание записей
        static List<Person> GeneratePersons(int count)
        {
            var tableName = "sungero_parties_counterparty";
            int maxId = GetNextId(tableName);
            if (maxId == 0)
                return null;

            var genders = new List<Name.Gender>() { Name.Gender.Male, Name.Gender.Female };

            var newPersons = new Faker<Person>("ru")
            .RuleFor(u => u.Id, (f, u) => f.IndexFaker + maxId)
            .RuleFor(u => u.Phone, (f, u) => "+7" + f.Phone.PhoneNumber().ToString())
            .RuleFor(u => u.Code, (f, u) => f.Random.Number(10, 100000))
            .RuleFor(u => u.Sex, f => f.PickRandom(genders))
            .RuleFor(u => u.Lastname, (f, u) => f.Name.LastName(u.Sex))
            .RuleFor(u => u.Firstname, (f, u) => f.Name.FirstName(u.Sex))
            .RuleFor(u => u.Name, (f, u) => u.Lastname + " " + u.Firstname)
            .RuleFor(u => u.Dateofbirth, (f, u) => f.Date.Between(DateTime.Today.AddYears(-60), DateTime.Today.AddYears(-18)))
            .RuleFor(u => u.Shortname, (f, u) => u.Lastname + " " + u.Firstname.Substring(0, 1) + " ")
            .RuleFor(u => u.Login, (f, u) => f.Internet.UserName($"{u.Firstname}{u.Id}", u.Lastname))
            .RuleFor(u => u.Email, (f, u) => f.Internet.Email($"{u.Firstname}{u.Id}", u.Lastname));

            var query = $"INSERT INTO {tableName}" +
            "(Id, discriminator, status, name, phones, code, lastname, firstname, dateofbirth, sex, shortname)" +
            "VALUES ";

            var persons = new List<Person>();
            foreach (var person in newPersons.Generate(count))
            {
                query += $"\n({person.Id}, '{person.Discriminator}', '{person.Status}', '{person.Name}', '{person.Phone}', "
                    + $"'{person.Code}', '{person.Lastname}', '{person.Firstname}', '{person.Dateofbirth}', '{person.Sex}', '{person.Shortname}'),";
                persons.Add(person);
            }

            query = query.Substring(0, query.Length - 1);

            if (InsertData(query))
                Console.WriteLine($"Success creating {count} Persons");
            else
                return null;

            return persons;
        }

        static List<int> GenerateLogins(int count, List<Person> persons)
        {
            var tableName = "sungero_core_login";
            int maxId = GetNextId(tableName);
            if (maxId == 0)
                return null;

            var newLogin = new Faker<Login>("ru")
            .RuleFor(u => u.Id, (f, u) => f.IndexFaker + maxId);

            var query = $"INSERT INTO {tableName}" +
            "(Id, discriminator, status, typeauthentication, loginname)" +
            "VALUES ";

            var loginsIds = new List<int>();
            for (var i = 0; i < count; i++)
            {
                var login = newLogin.Generate();
                query += $"\n({login.Id}, '{login.Discriminator}', '{login.Status}', '{login.TypeAuthentication}', '{persons[i].Login}'),";
                loginsIds.Add(login.Id);
            }

            query = query.Substring(0, query.Length - 1);

            if (InsertData(query))
                Console.WriteLine($"Success creating {count} Logins");
            else
                return null;

            return loginsIds;
        }

        static List<int> GenerateBusinessUnits(int count)
        {
            var tableName = "sungero_core_recipient";
            int maxId = GetNextId(tableName);
            if (maxId == 0)
                return null;

            var newBusinessUnit = new Faker<BusinessUnit>("ru")
            .RuleFor(u => u.Id, (f, u) => f.IndexFaker + maxId)
            .RuleFor(u => u.Sid, (f, u) => f.Random.Uuid().ToString())
            .RuleFor(u => u.Name, (f, u) => f.Company.CompanyName())
            .RuleFor(u => u.LegalName, (f, u) => u.Name)
            .RuleFor(u => u.Code, (f, u) => f.Random.Number(10, 100000))
            .RuleFor(u => u.Phone, (f, u) => "+7" + f.Phone.PhoneNumber().ToString())
            .RuleFor(u => u.TIN, (f, u) => f.Random.Number(10, 100000).ToString())
            .RuleFor(u => u.TRRC, (f, u) => f.Random.Number(10, 100000).ToString())
            .RuleFor(u => u.PSRN, (f, u) => f.Random.Number(10, 100000).ToString());

            var query = $"INSERT INTO {tableName}" +
                "(Id, sid, discriminator, status, name, legalname_company_sungero, code_company_sungero, phone_company_sungero, "
                + "tin_company_sungero, trrc_company_sungero, psrn_company_sungero)" +
                "VALUES ";

            var businessUnitsIds = new List<int>();
            foreach (var businessUnit in newBusinessUnit.GenerateLazy(count))
            {
                query += $"\n({businessUnit.Id}, '{businessUnit.Sid}', '{businessUnit.Discriminator}', '{businessUnit.Status}', '{businessUnit.Name}', "
                    + $"'{businessUnit.LegalName}', '{businessUnit.Code}', '{businessUnit.Phone}', '{businessUnit.TIN}', "
                    + $"'{businessUnit.TRRC}', '{businessUnit.PSRN}'),";
                businessUnitsIds.Add(businessUnit.Id);
            }

            query = query.Substring(0, query.Length - 1);

            if (InsertData(query))
                Console.WriteLine($"Success creating {count} Business Units");
            else
                return null;

            return businessUnitsIds;
        }

        static List<int> GenerateDepartments(int count, List<int> businessUnitIds)
        {
            var tableName = "sungero_core_recipient";
            int maxId = GetNextId(tableName);
            if (maxId == 0)
                return null;

            var newDepartment = new Faker<Department>("ru")
            .RuleFor(u => u.Id, (f, u) => f.IndexFaker + maxId)
            .RuleFor(u => u.Sid, (f, u) => f.Random.Uuid().ToString())
            .RuleFor(u => u.Name, (f, u) => f.Commerce.Department())
            .RuleFor(u => u.Code, (f, u) => f.Random.Number(10, 100000))
            .RuleFor(u => u.Phone, (f, u) => "+7" + f.Phone.PhoneNumber().ToString())
            .RuleFor(u => u.BusinessUnitId, (f, u) => f.PickRandom(businessUnitIds));

            var query = $"INSERT INTO {tableName}" +
                "(Id, sid, discriminator, status, name, code_company_sungero, phone_company_sungero, businessunit_company_sungero)" +
                "VALUES ";

            var departmentsIds = new List<int>();
            foreach (var department in newDepartment.GenerateLazy(count))
            {
                query += $"\n({department.Id}, '{department.Sid}', '{department.Discriminator}', '{department.Status}', '{department.Name}', "
                    + $"'{department.Code}', '{department.Phone}', '{department.BusinessUnitId}'),";
                departmentsIds.Add(department.Id);
            }

            query = query.Substring(0, query.Length - 1);

            if (InsertData(query))
                Console.WriteLine($"Success creating {count} Departments");
            else
                return null;

            return departmentsIds;
        }

        static void GenerateEmployees(int count, List<Person> persons, List<int> loginsIds, List<int> departmentsIds)
        {
            var tableName = "sungero_core_recipient";
            int maxId = GetNextId(tableName);
            if (maxId == 0)
                return;

            var newEmployee = new Faker<Employee>("ru")
            .RuleFor(u => u.Id, (f, u) => f.IndexFaker + maxId)
            .RuleFor(u => u.Sid, (f, u) => f.Random.Uuid().ToString())
            .RuleFor(u => u.TabNumber, (f, u) => f.Random.Number(10, 100000))
            .RuleFor(u => u.DepartmentId, (f, u) => f.PickRandom(departmentsIds));

            var query = $"INSERT INTO {tableName}" +
                "(Id, sid, discriminator, status, name, person_company_sungero, login, department_company_sungero, persnumber_company_sungero, " +
                "email_company_sungero, neednotifyexpi_company_sungero, neednotifynewa_company_sungero)" +
                "VALUES ";

            var falseType = sqlType == sqlTypes.Postgres ? "false" : "0";
            for (var i = 0; i < count; i++)
            {
                var employee = newEmployee.Generate();
                query += $"\n({employee.Id}, '{employee.Sid}', '{employee.Discriminator}', '{employee.Status}', '{persons[i].Name}', "
                    + $"'{persons[i].Id}', '{loginsIds[i]}', '{employee.DepartmentId}', '{employee.TabNumber}', '{persons[i].Email}', {falseType}, {falseType}),";
            }

            query = query.Substring(0, query.Length - 1);

            if (InsertData(query))
                Console.WriteLine($"Success creating {count} Employees");
            else
                return;
        }
        #endregion

        #region Полезный функционал
        /// <summary>
        /// Получить следующий id для указанной таблицы
        /// </summary>
        /// <param name="TableName">Название таблицы</param>
        /// <returns>Следующий id</returns>
        static int GetNextId(string TableName)
        {
            int maxId = 0;
            if (sqlType == sqlTypes.Postgres)
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    var command = new NpgsqlCommand($"select Max(Id) from {TableName}", connection);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        if (!int.TryParse(reader[0].ToString(), out maxId))
                            return 0;
                    }
                    connection.Close();
                }
            }
            else
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    var command = new SqlCommand($"select Max(Id) from {TableName}", connection);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        if (!int.TryParse(reader[0].ToString(), out maxId))
                            return 0;
                    }
                    connection.Close();
                }
            }

            return maxId + 1;
        }

        /// <summary>
        /// Вставка данных
        /// </summary>
        /// <param name="query">Строка запроса</param>
        /// <returns>True - при успешном выполнении комманды, иначе false</returns>
        static bool InsertData(string query)
        {
            try
            {
                if (sqlType == sqlTypes.Postgres)
                {
                    using (var connection = new NpgsqlConnection(connectionString))
                    {
                        var command = new NpgsqlCommand(query, connection);
                        connection.Open();
                        command.ExecuteNonQuery();
                        connection.Close();
                    }
                }
                else
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        var command = new SqlCommand(query, connection);
                        connection.Open();
                        command.ExecuteNonQuery();
                        connection.Close();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }
        #endregion
    }
}
