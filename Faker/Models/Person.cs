using System;
using Bogus;
using Bogus.DataSets;
using System.Collections.Generic;
using System.Text;

namespace DrxFaker
{
    class Person
    {
        public int Id { get; set; }
        public Guid Discriminator = Guid.Parse("f5509cdc-ac0c-4507-a4d3-61d7a0a9b6f6");
        public string Status = "Active";
        public string Phone { get; set; }
        public int Code { get; set; }
        public string Lastname { get; set; }
        public string Firstname { get; set; }
        public string Name { get; set; }
        public DateTime Dateofbirth { get; set; }
        public Name.Gender Sex { get; set; }
        public string Shortname { get; set; }
        public string Login { get; set; }
        public string Email { get; set; }
    }
}
