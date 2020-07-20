using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eveldee._3DSCPlusViGEm
{
    public class TouchMap
    {
        public double X1 { get; set; }
        public double X2 { get; set; }
        public double Y1 { get; set; }
        public double Y2 { get; set; }
        public N3DSInputs Inputs { get; set; }

        public bool HasValidArea()
        {
            bool IsPercentage(double number)
            {
                return number >= 0 && number <= 1.0;
            }

            return IsPercentage(X1) && IsPercentage(X2) && IsPercentage(Y1) && IsPercentage(Y2);
        }
    }
}
