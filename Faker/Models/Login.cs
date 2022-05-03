using System;
using System.Collections.Generic;
using System.Text;

namespace DrxFaker
{
    class Login
    {
        public int Id { get; set; }
        public Guid Discriminator = Guid.Parse("55f542e9-4645-4f8d-999e-73cc71df62fd");
        public string Status = "Active";
        public string TypeAuthentication = "Windows";
        public string LoginName { get; set; }
    }
}
