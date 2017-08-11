using System;
using DashShared;
using Windows.Foundation;

namespace Dash
{
    public class PointFieldModel : FieldModel
    { 

        public PointFieldModel(Point data, string id = null) : base(id)
        {
            Data = data;
        }

        public PointFieldModel(double x, double y) : this(new Point(x, y))
        {
        }

        public Point Data;

        public override string ToString()
        {
            return $"PointFieldModel: {Data}";
        }

        protected override FieldModelDTO GetFieldDTOHelper()
        {
            return new FieldModelDTO(TypeInfo.Point, Data, Id);
        }
    }
}
