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
        //-t p -s "192.168.3.237" -d directum -u directum -p 1Qwerty --emp 100 --bus 1 --dep 2
        //-t m -s "192.168.1.82" -d S_NESTLE_RX3523_Dev_MAA -u sa -p 1Qwerty --emp 100 --bus 1 --dep 2

        static string connectionString;
        enum sqlTypes { Postgres, MS };
        static sqlTypes sqlType;

        static void Main()
        {
            Parser.Default.ParseArguments<Options>(new List<string>() { "--help" });

            try
            {
                bool keepLooping = true;
                while (keepLooping)
                {
                    var args = Console.ReadLine().Trim().Split();
                    var parser = Parser.Default.ParseArguments<Options>(args)
                        .WithParsed(PreparationToStart);

                    if (Console.ReadKey().Key == ConsoleKey.Escape)
                        keepLooping = false;
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
            Console.WriteLine("\nRunTime " + elapsedTime);
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
                Console.WriteLine("\nCreation business units");
                var businesUnitsIds = GenerateBusinessUnits(businessCount, command);
                if (businesUnitsIds == null)
                {
                    throw new Exception("Error while creating Business Units");
                }

                Console.WriteLine("Creation departments");
                var departmentsIds = GenerateDepartments(departmentsCount, businesUnitsIds, command);
                if (departmentsIds == null)
                {
                    throw new Exception("Error while creating Departments");
                }

                Console.WriteLine("Creation persons");
                var persons = GeneratePersons(employeesCount, command);
                if (persons == null)
                {
                    throw new Exception("Error while creating Persons");
                }

                Console.WriteLine("Creation logins");
                var loginsIds = GenerateLogins(employeesCount, persons, command);
                if (loginsIds == null)
                {
                    throw new Exception("Error while creating Logins");
                }

                Console.WriteLine("Creation employees");
                var employees = GenerateEmployees(employeesCount, persons, loginsIds, departmentsIds, command);
                if (loginsIds == null)
                {
                    throw new Exception("Error while creating employees");
                }

                Console.WriteLine("Creation recipien links");
                GenerateRecipienLlink(employees, command);

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

        #region Создание записей

        static List<int> GenerateBusinessUnits(int count, object command)
        {
            var tableName = "sungero_core_recipient";
            int maxId = GetNextId(tableName, command);
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

            if (InsertData(query, command))
                Console.WriteLine($"Success creating {count} Business Units");
            else
                return null;

            UpdateId(command, tableName);

            return businessUnitsIds;
        }

        static List<int> GenerateDepartments(int count, List<int> businessUnitIds, object command)
        {
            var tableName = "sungero_core_recipient";
            int maxId = GetNextId(tableName, command);
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

            if (InsertData(query, command))
                Console.WriteLine($"Success creating {count} Departments");
            else
                return null;

            UpdateId(command, tableName);

            return departmentsIds;
        }

        static List<Person> GeneratePersons(int count, object command)
        {
            var tableName = "sungero_parties_counterparty";
            int maxId = GetNextId(tableName, command);
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

            if (InsertData(query, command))
                Console.WriteLine($"Success creating {count} Persons");
            else
                return null;

            UpdateId(command, tableName);

            return persons;
        }

        static List<int> GenerateLogins(int count, List<Person> persons, object command)
        {
            var tableName = "sungero_core_login";
            int maxId = GetNextId(tableName, command);
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

            if (InsertData(query, command))
                Console.WriteLine($"Success creating {count} Logins");
            else
                return null;

            UpdateId(command, tableName);

            return loginsIds;
        }

        static List<Employee> GenerateEmployees(int count, List<Person> persons, List<int> loginsIds, List<int> departmentsIds, object command)
        {
            var tableName = "sungero_core_recipient";
            int maxId = GetNextId(tableName, command);
            if (maxId == 0)
                return null;

            var falseType = sqlType == sqlTypes.Postgres ? "false" : "0";
            var isNeedNotifySum = IsContainsColumn(command, tableName, "neednotifysuma_company_sungero");
            var needNotifySumStr = isNeedNotifySum ?
                ", neednotifysuma_company_sungero" :
                string.Empty;
            var needNotifySumValue = isNeedNotifySum ?
                $", {falseType}" :
                string.Empty;

            var newEmployee = new Faker<Employee>("ru")
            .RuleFor(u => u.Id, (f, u) => f.IndexFaker + maxId)
            .RuleFor(u => u.Sid, (f, u) => f.Random.Uuid().ToString())
            .RuleFor(u => u.TabNumber, (f, u) => f.Random.Number(10, 100000))
            .RuleFor(u => u.DepartmentId, (f, u) => f.PickRandom(departmentsIds));

            var query = $"INSERT INTO {tableName}" +
                "(Id, sid, discriminator, status, name, person_company_sungero, login, department_company_sungero, persnumber_company_sungero, " +
                $"email_company_sungero, neednotifyexpi_company_sungero, neednotifynewa_company_sungero{needNotifySumStr})" +
                "VALUES ";

            var employees = new List<Employee>();
            for (var i = 0; i < count; i++)
            {
                var employee = newEmployee.Generate();
                query += $"\n({employee.Id}, '{employee.Sid}', '{employee.Discriminator}', '{employee.Status}', '{persons[i].Name}', "
                    + $"'{persons[i].Id}', '{loginsIds[i]}', '{employee.DepartmentId}', '{employee.TabNumber}', '{persons[i].Email}', {falseType}, {falseType}{needNotifySumValue}),";
                employees.Add(employee);
            }

            query = query.Substring(0, query.Length - 1);

            if (InsertData(query, command))
                Console.WriteLine($"Success creating {count} Employees");
            else
                return null;

            UpdateId(command, tableName);

            return employees;
        }

        static void GenerateRecipienLlink(List<Employee> employees, object command)
        {
            var tableName = "sungero_core_recipientlink";
            int maxId = GetNextId(tableName, command);
            if (maxId == 0)
                return;

            var query = $"INSERT INTO {tableName}" +
                "(id, recipient, member, discriminator)" +
                "VALUES ";

            for (var i = 0; i < employees.Count; i++)
            {
                query += $"\n({maxId + i}, '{employees[i].DepartmentId}', '{employees[i].Id}', 'a9e935d5-3b72-4e3a-9e43-711d8f32b84e'),";
            }

            query = query.Substring(0, query.Length - 1);

            if (InsertData(query, command))
                Console.WriteLine($"Success creating {employees.Count} Recipient links");
            else
                return;

            UpdateId(command, tableName);
        }

        #endregion

        #region Работа с базой данных

        /// <summary>
        /// Получить следующий id для указанной таблицы
        /// </summary>
        /// <param name="tableName">Название таблицы</param>
        /// <param name="command">SQL команда</param>
        /// <returns>Следующий id</returns>
        static int GetNextId(string tableName, object command)
        {
            int maxId = 0;
            if (sqlType == sqlTypes.Postgres)
            {
                var postgresCommand = command as NpgsqlCommand;
                postgresCommand.CommandText = $"select Max(Id) from {tableName}";
                var reader = postgresCommand.ExecuteReader();
                while (reader.Read())
                {
                    if (!int.TryParse(reader[0].ToString(), out maxId))
                        return 0;
                }
                reader.Close();
            }
            else
            {
                var sqlCommand = command as SqlCommand;
                sqlCommand.CommandText = $"select Max(Id) from {tableName}";
                var reader = sqlCommand.ExecuteReader();
                while (reader.Read())
                {
                    if (!int.TryParse(reader[0].ToString(), out maxId))
                        return 0;
                }
                reader.Close();
            }

            return maxId + 1;
        }

        /// <summary>
        /// Вставка данных
        /// </summary>
        /// <param name="query">Строка запроса</param>
        /// <param name="command">SQL команда</param>
        /// <returns>True - при успешном выполнении комманды, иначе false</returns>
        static bool InsertData(string query, object command)
        {
            try
            {
                if (sqlType == sqlTypes.Postgres)
                {
                    var postgresCommand = command as NpgsqlCommand;
                    postgresCommand.CommandText = query;
                    postgresCommand.ExecuteNonQuery();
                }
                else
                {
                    var sqlCommand = command as SqlCommand;
                    sqlCommand.CommandText = query;
                    sqlCommand.ExecuteNonQuery();
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }

        /// <summary>
        /// Обновление данных в таблице связанной с генерацией ID
        /// </summary>
        /// <param name="command">SQL команда</param>
        /// <param name="tablename">Название таблицы</param>
        /// <returns>True - при успешном выполнении комманды, иначе false</returns>
        static bool UpdateId(object command, string tablename)
        {
            var query = string.Format("update sungero_system_ids set lastid = (select max(id) from {0}) where tablename = '{0}'", tablename);
            try
            {
                if (sqlType == sqlTypes.Postgres)
                {
                    var postgresCommand = command as NpgsqlCommand;
                    postgresCommand.CommandText = query;
                    postgresCommand.ExecuteNonQuery();
                }
                else
                {
                    var sqlCommand = command as SqlCommand;
                    sqlCommand.CommandText = query;
                    sqlCommand.ExecuteNonQuery();
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }

        /// <summary>
        /// Проверка на наличие столбца в таблице
        /// </summary>
        /// <param name="command">SQL команда</param>
        /// <param name="tablename">Название таблицы</param>
        /// <param name="column">Название столбца</param>
        /// <returns>True - при наличии столбца, иначе false</returns>
        static bool IsContainsColumn(object command, string tablename, string column)
        {
            var query = "SELECT column_name FROM information_schema.columns " +
                $"WHERE table_name = '{tablename}' AND column_name = '{column}'";
            try
            {
                var isHasColumn = false;
                if (sqlType == sqlTypes.Postgres)
                {
                    var postgresCommand = command as NpgsqlCommand;
                    postgresCommand.CommandText = query;
                    var reader = postgresCommand.ExecuteReader();
                    isHasColumn = reader.HasRows;
                    reader.Close();
                }
                else
                {
                    var sqlCommand = command as SqlCommand;
                    sqlCommand.CommandText = query;
                    var reader = sqlCommand.ExecuteReader();
                    isHasColumn = reader.HasRows;
                    reader.Close();
                }

                return isHasColumn;
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
