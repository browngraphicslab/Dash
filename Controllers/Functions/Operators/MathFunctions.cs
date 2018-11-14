using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public static class MathFunctions
    {
        public static NumberController Max(NumberController a, NumberController b)
        {
            return a.Data > b.Data ? a : b;
        }

        public static NumberController Min(NumberController a, NumberController b)
        {
            return a.Data < b.Data ? a : b;
        }

        public static NumberController Sin(NumberController theta)
        {
            return new NumberController(Math.Sin(theta.Data));
        }

        public static NumberController Cos(NumberController theta)
        {
            return new NumberController(Math.Cos(theta.Data));
        }

        public static NumberController Tan(NumberController theta)
        {
            return new NumberController(Math.Tan(theta.Data));
        }

        private static Random r = new Random();

        public static NumberController Rand()
        {
            return new NumberController(r.NextDouble());
        }

        [OperatorFunctionName("rand_i")]
        public static NumberController RandI()
        {
            return new NumberController(r.Next());
        }

        [OperatorFunctionName("rand_i")]
        public static NumberController RandIMax(NumberController max)
        {
            return new NumberController(r.Next((int)max.Data));
        }

        [OperatorFunctionName("rand_i")]
        public static NumberController RandIRange(NumberController min, NumberController max)
        {
            return new NumberController(r.Next((int) min.Data, (int)max.Data));
        }
    }
}
