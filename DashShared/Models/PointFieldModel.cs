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
    }
}
