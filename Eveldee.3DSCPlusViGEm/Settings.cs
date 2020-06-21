using MarcusD._3DSCPlusDummy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Eveldee._3DSCPlusViGEm
{
    public class Settings
    {
        public string IP { get; set; }
        public TargetType TargetType { get; set; }

        public Settings()
        {
            IP = "";
            TargetType = TargetType.Xbox360;
        }
    }
}
