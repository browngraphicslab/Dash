using Windows.UI;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [FieldModelType(TypeInfo.Color)]
    public class ColorModel : FieldModel
    {
        public Color Data;

        public ColorModel(Color data, string id = null) : base(id) => Data = data;

        public override string ToString() => $"ColorFieldModel: {Data}";
    }
}
