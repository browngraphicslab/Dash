using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Dash
{
    public class NumberFieldModel : FieldModel
    {
        public NumberFieldModel() { }

        public NumberFieldModel(double data)
        {
            Data = data;
        }

        public double Data;

        public override string ToString()
        {
            return $"NumberFieldModel: {Data}";
        }
    }
}
