using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace DrxFaker
{
    class Options
    {
        [Option('t', "type", Required = true, HelpText = "Type of Sql (Postgres - p, MS - m).")]
        public string SqlType { get; set; }

        [Option('s', "server", Default = "localhost", HelpText = "Server address.")]
        public string Server { get; set; }

        [Option("port", Default = "5432", HelpText = "Postgres server port.")]
        public string Port { get; set; }

        [Option('d', "database", Required = true, HelpText = "Name of database.")]
        public string Database { get; set; }

        [Option('u', "user", Required = true, HelpText = "User login.")]
        public string UserId { get; set; }

        [Option('p', "password", Required = true, HelpText = "User password.")]
        public string Password { get; set; }


        [Option("emp", Required = true, HelpText = "Count of generated employees.")]
        public int EmployeesCount { get; set; }

        [Option("bus", Required = true, HelpText = "Count of generated businesses.")]
        public int BusinessCount { get; set; }

        [Option("dep", Required = true, HelpText = "Count of generated departments")]
        public int DepartmentsCount { get; set; }
    }
}
