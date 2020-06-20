using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eveldee._3DSCPlusViGEm
{
    [Flags]
    public enum N3DSInputs : int
    {
        LeftStickLeft = 0x2000_0000,
        LeftStickUp = 0x4000_0000,
        LeftStickRight = 0x1000_0000,
        LeftStickDown = int.MinValue,

        RightStickLeft = 0x0200_0000,
        RightStickUp = 0x0400_0000,
        RightStickRight = 0x0100_0000,
        RightStickDown = 0x0800_0000,

        A = 0x0000_0001,
        B = 0x0000_0002,
        X = 0x0000_0400,
        Y = 0x0000_0800,

        Left = 0x0000_0020,
        Up = 0x0000_0040,
        Right = 0x0000_0010,
        Down = 0x0000_0080,

        L = 0x0000_0200,
        R = 0x0000_0100,
        ZL = 0x0000_4000,
        ZR = 0x0000_8000,

        Start = 0x0000_0008,
        Select = 0x0000_0004,

        Touch = 0x0010_0000
    }
}
