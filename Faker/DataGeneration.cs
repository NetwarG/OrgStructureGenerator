using System;
using System.Collections.Generic;
using System.Text;
using Bogus;
using Bogus.DataSets;
using static DrxFaker.Program;
using static DrxFaker.WorkWithDB;

namespace DrxFaker
{
    class DataGeneration
    {

        public static List<int> GenerateBusinessUnits(int count, object command)
        {
            var tableName = "sungero_core_recipient";
            int maxId = GetNextId(tableName, command);
            if (maxId < 0)
                return null;

            var falseType = sqlType == sqlTypes.Postgres ? "false" : "0";
            var isNeedNonresident = IsContainsColumn(command, tableName, "nonresident_company_sungero");
            var needNonresidentStr = isNeedNonresident ?
                ", nonresident_company_sungero" :
                string.Empty;
            var needNonresidentValue = isNeedNonresident ?
                $", {falseType}" :
                string.Empty;

            var newBusinessUnit = new Faker<BusinessUnit>("ru")
            .RuleFor(u => u.Id, (f, u) => f.IndexFaker + maxId)
            .RuleFor(u => u.Sid, (f, u) => f.Random.Uuid().ToString())
            .RuleFor(u => u.Name, (f, u) => $"ООО \"{f.Company.CompanyName()}\"")
            .RuleFor(u => u.LegalName, (f, u) => u.Name)
            .RuleFor(u => u.Code, (f, u) => f.Random.Number(1000, 100000))
            .RuleFor(u => u.Phone, (f, u) => "+7" + f.Phone.PhoneNumber().ToString())
            .RuleFor(u => u.TIN, (f, u) => f.Random.Number(10, 100000).ToString())
            .RuleFor(u => u.TRRC, (f, u) => f.Random.Number(10, 100000).ToString())
            .RuleFor(u => u.PSRN, (f, u) => f.Random.Number(10, 100000).ToString());

            var query = $"INSERT INTO {tableName}" +
                "(Id, sid, discriminator, status, name, legalname_company_sungero, code_company_sungero, phone_company_sungero, "
                + $"tin_company_sungero, trrc_company_sungero, psrn_company_sungero{needNonresidentStr})" +
                "VALUES ";

            var businessUnitsIds = new List<int>();
            foreach (var businessUnit in newBusinessUnit.Generate(count))
            {
                query += $"\n({businessUnit.Id}, '{businessUnit.Sid}', '{businessUnit.Discriminator}', '{businessUnit.Status}', '{businessUnit.Name}', "
                    + $"'{businessUnit.LegalName}', '{businessUnit.Code}', '{businessUnit.Phone}', '{businessUnit.TIN}', "
                    + $"'{businessUnit.TRRC}', '{businessUnit.PSRN}'{needNonresidentValue}),";
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

        public static List<int> GenerateDepartments(int count, List<int> businessUnitIds, object command)
        {
            var tableName = "sungero_core_recipient";
            int maxId = GetNextId(tableName, command);
            if (maxId < 0)
                return null;

            var newDepartment = new Faker<Department>("ru")
            .RuleFor(u => u.Id, (f, u) => f.IndexFaker + maxId)
            .RuleFor(u => u.Sid, (f, u) => f.Random.Uuid().ToString())
            .RuleFor(u => u.Name, (f, u) => f.Commerce.Department())
            .RuleFor(u => u.Code, (f, u) => f.Random.Number(1000, 100000))
            .RuleFor(u => u.Phone, (f, u) => "+7" + f.Phone.PhoneNumber().ToString())
            .RuleFor(u => u.BusinessUnitId, (f, u) => f.PickRandom(businessUnitIds));

            var query = $"INSERT INTO {tableName}" +
                "(Id, sid, discriminator, status, name, code_company_sungero, phone_company_sungero, businessunit_company_sungero)" +
                "VALUES ";

            var departmentsIds = new List<int>();
            foreach (var department in newDepartment.Generate(count))
            {
                query += $"\n({department.Id}, '{department.Sid}', '{department.Discriminator}', '{department.Status}', '{department.Name} ({department.Code})', "
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

        public static List<Person> GeneratePersons(int count, object command)
        {
            var tableName = "sungero_parties_counterparty";
            int maxId = GetNextId(tableName, command);
            if (maxId < 0)
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

        public static List<int> GenerateLogins(int count, List<Person> persons, object command)
        {
            var tableName = "sungero_core_login";
            int maxId = GetNextId(tableName, command);
            if (maxId < 0)
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

        public static List<Employee> GenerateEmployees(int count, List<Person> persons, List<int> loginsIds, List<int> departmentsIds, object command)
        {
            var tableName = "sungero_core_recipient";
            int maxId = GetNextId(tableName, command);
            if (maxId < 0)
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

        public static void GenerateRecipientLink(List<Employee> employees, object command)
        {
            var tableName = "sungero_core_recipientlink";
            int maxId = GetNextId(tableName, command);
            if (maxId < 0)
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
    }
}
