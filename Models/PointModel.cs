using System;
using DashShared;
using Windows.Foundation;
using DashShared.Models;

namespace Dash
{
    [FieldModelTypeAttribute(TypeInfo.Point)]
    public class PointModel : FieldModel
    { 

        public PointModel(Point data, string id = null) : base(id)
        {
            Data = data;
        }

        public PointModel():this(0,0)
        {
            
        }

        public PointModel(double x, double y) : this(new Point(x, y))
        {
        }

        public Point Data;

        public override string ToString()
        {
            return $"PointFieldModel: {Data}";
        }
    }
}
