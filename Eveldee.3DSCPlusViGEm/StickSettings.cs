using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eveldee._3DSCPlusViGEm
{
    public class StickSettings
    {
        public class AxisSettings
        {
            public bool InvertX { get; set; }
            public bool InvertY { get; set; }
            public int DeadzoneX { get; set; } = 12;
            public int DeadzoneY { get; set; } = 12;
            public double SensibiltyX { get; set; } = 1.0;
            public double SensibiltyY { get; set; } = 1.0;
        }

        public AxisSettings Left { get; set; }
        public AxisSettings Right { get; set; }

        public StickSettings()
        {
            Left = new AxisSettings();
            Right = new AxisSettings();
        }
    }
}
