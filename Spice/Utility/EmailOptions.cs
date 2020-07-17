using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spice.Utility
{
    public class EmailOptions
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string SmtpHost { get; set; }
        public string SmtpPort { get; set; }
    }
}
