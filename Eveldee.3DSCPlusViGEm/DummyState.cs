using Eveldee._3DSCPlusViGEm.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eveldee._3DSCPlusViGEm
{
    public class DummyState
    {
        public class InputState
        {
            public short LeftStickX { get; set; }
            public short LeftStickY { get; set; }
            public bool LeftStick { get; set; }

            public short RightStickX { get; set; }
            public short RightStickY { get; set; }
            public bool RightStick { get; set; }

            public bool Left { get; set; }
            public bool Up { get; set; }
            public bool Right { get; set; }
            public bool Down { get; set; }

            public bool A { get; set; }
            public bool B { get; set; }
            public bool X { get; set; }
            public bool Y { get; set; }

            public bool L { get; set; }
            public bool R { get; set; }
            public bool ZL { get; set; }
            public bool ZR { get; set; }

            public bool Start { get; set; }
            public bool Select { get; set; }

            public bool IsTouch { get; set; }
        }

        public class TouchState
        {
            public bool IsTouch { get; set; }
            public short TouchX { get; set; }
            public short TouchY { get; set; }
        }

        public Option<InputState> Inputs { get; set; }
        public Option<TouchState> Touch { get; set; }

        public DummyState()
        {
            Inputs = None.Value;
            Touch = None.Value;
        }
    }
}
