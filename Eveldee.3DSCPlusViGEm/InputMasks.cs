using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eveldee._3DSCPlusViGEm
{
    public static class InputMasks
    {
        public const int LeftStickLeft = 0x2000_0000;
        public const int LeftStickUp = 0x4000_0000;
        public const int LeftStickRight = 0x1000_0000;
        public const int LeftStickDown = int.MinValue;

        public const int RightStickLeft = 0x0200_0000;
        public const int RightStickUp = 0x0400_0000;
        public const int RightStickRight = 0x0100_0000;
        public const int RightStickDown = 0x0800_0000;

        public const int A = 0x0000_0001;
        public const int B = 0x0000_0002;
        public const int X = 0x0000_0400;
        public const int Y = 0x0000_0800;

        public const int Left = 0x0000_0020;
        public const int Up = 0x0000_0040;
        public const int Right = 0x0000_0010;
        public const int Down = 0x0000_0080;

        public const int L = 0x0000_0200;
        public const int R = 0x0000_0100;
        public const int ZL = 0x0000_4000;
        public const int ZR = 0x0000_8000;

        public const int Start = 0x0000_0008;
        public const int Select = 0x0000_0004;

        public const int Touch = 0x0010_0000;
    }
}
