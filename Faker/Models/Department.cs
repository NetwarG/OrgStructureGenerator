using System;
using System.Collections.Generic;
using System.Text;

namespace DrxFaker
{
    class Department
    {
        public int Id { get; set; }
        public string Sid { get; set; }
        public Guid Discriminator = Guid.Parse("61b1c19f-26e2-49a5-b3d3-0d3618151e12");
        public string Status = "Active";
        public string Name { get; set; }
        public int Code { get; set; }
        public int BusinessUnitId { get; set; }
        public string Phone { get; set; }
    }
}
