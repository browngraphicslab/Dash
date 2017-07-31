using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Dash
{
    public class RectFieldModel : FieldModel
    {
        public RectFieldModel() { }

        public RectFieldModel(Rect data) : this()
        {
            Data = data;
        }

        public RectFieldModel(double x, double y, double width, double height) : this(new Rect(x, y, width, height))
        {
        }

        public Rect Data;

        public override string ToString()
        {
            return $"RectFieldModel: {Data}";
        }
    }
}
