using System;
using DashShared;
using Windows.Foundation;

namespace Dash
{
    public class PointFieldModel : FieldModel
    {
        public PointFieldModel() { }

        public PointFieldModel(Point data) : this()
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

        public override FieldModelDTO GetFieldDTO()
        {
            return new FieldModelDTO(TypeInfo.Reference, Data);
        }
    }
}
