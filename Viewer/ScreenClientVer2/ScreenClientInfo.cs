using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenClientVer2
{
    public class ScreenClientInfo
    {
        public string MachineName { get; set; } = "";
        public Image? LastImage { get; set; }
        public DateTime LastSeenUtc { get; set; } = DateTime.MinValue;
        public bool IsOnline => (DateTime.UtcNow - LastSeenUtc) < TimeSpan.FromSeconds(30); // default 30s
        public string Lab => MachineName?.Split('-', StringSplitOptions.RemoveEmptyEntries).Length > 0
            ? MachineName.Split('-', StringSplitOptions.RemoveEmptyEntries)[0]
            : "unknown";
    }
}
