using Windows.Foundation;
using DashShared;

namespace Dash
{
    [FieldModelTypeAttribute(TypeInfo.Rectangle)]
    public class RectModel : FieldModel
    {

        public RectModel()
        {
            
        }

        public RectModel(Rect data, string id = null) : base(id)
        {
            Data = data;
        }

        public RectModel(double x, double y, double width, double height) : this(new Rect(x, y, width, height))
        {
        }

        public Rect Data;

        public override string ToString()
        {
            return $"RectFieldModel: {Data}";
        }
    }
}
