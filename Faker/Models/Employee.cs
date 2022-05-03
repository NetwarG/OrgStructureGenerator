using System;
using System.Collections.Generic;
using System.Text;

namespace DrxFaker
{
    class Employee
    {
        public int Id { get; set; }
        public string Sid { get; set; }
        public Guid Discriminator = Guid.Parse("b7905516-2be5-4931-961c-cb38d5677565");
        public string Status = "Active";
        public string Name { get; set; }
        public int PersonId { get; set; }
        public int LoginId { get; set; }
        public int DepartmentId { get; set; }
        public int TabNumber { get; set; }
        public string Email { get; set; }
    }
}
