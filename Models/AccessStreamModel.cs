using Windows.Storage.Streams;
using DashShared;

namespace Dash
{
    [FieldModelType(TypeInfo.AccessStream)]
    public class AccessStreamModel : FieldModel
    {
        public IRandomAccessStream Data;

        public AccessStreamModel(IRandomAccessStream data, string id = null) : base(id) => Data = data;

        public override string ToString() => $"AccessStreamFieldModel: {Data}";
    }
}
