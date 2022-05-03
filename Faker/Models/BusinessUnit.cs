using System;
using System.Collections.Generic;
using System.Text;

namespace DrxFaker
{
    class BusinessUnit
    {
        public int Id { get; set; }
        public string Sid { get; set; }
        public Guid Discriminator = Guid.Parse("eff95720-181f-4f7d-892d-dec034c7b2ab");
        public string Status = "Active";
        public string Name { get; set; }
        public string LegalName { get; set; }
        public int Code { get; set; }
        public string Phone { get; set; }
        public string TIN { get; set; } //max 50
        public string TRRC { get; set; } //max 50
        public string PSRN { get; set; } //max 50
    }
}
