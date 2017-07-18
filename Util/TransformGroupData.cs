using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Dash
{
    public struct TransformGroupData
    {
        public Point Translate { get; set; }
        public Point ScaleCenter { get; set; }
        public Point ScaleAmount { get; set; }

        public TransformGroupData(Point translate, Point scaleCenter, Point scaleAmount)
        {
            Translate = translate;
            ScaleCenter = scaleCenter;
            ScaleAmount = scaleAmount;
        }
    }
}
