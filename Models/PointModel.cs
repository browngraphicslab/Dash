using DashShared;
using Windows.Foundation;

namespace Dash
{
    [FieldModelTypeAttribute(TypeInfo.Point)]
    public class PointModel : FieldModel
    { 

        public PointModel(Point data, string id = null) : base(id)
        {
            Data = data;
        }

        public PointModel():this(new Point(0, 0))
        {
            
        }

        public Point Data;

        public override string ToString()
        {
            return $"PointFieldModel: {Data}";
        }
    }
}
