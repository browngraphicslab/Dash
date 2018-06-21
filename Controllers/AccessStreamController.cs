using Windows.Storage.Streams;
using DashShared;

namespace Dash
{
    public class AccessStreamController : FieldModelController<AccessStreamModel>
    {
        //OVERLOADED CONSTRUCTORS
        public AccessStreamController() : this(new AccessStreamModel(null)) { }

        public AccessStreamController(IRandomAccessStream data = null) : base(new AccessStreamModel(data)) => SaveOnServer();

        public AccessStreamController(AccessStreamModel accessStreamFieldModel) : base(accessStreamFieldModel) { }

        public override void Init() { }

        /// <summary>
        ///     The <see cref="AccessStreamFieldModel" /> associated with this <see cref="Dash.AccessStreamController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public AccessStreamModel AccessStreamFieldModel => Model as AccessStreamModel;

        public override FieldControllerBase GetDefaultController() => new AccessStreamController(new AccessStreamModel(null));

        public override object GetValue(Context context) => Data;

        public override bool TrySetValue(object value)
        {
            var data = value as IRandomAccessStream;
            if (!(value is IRandomAccessStream)) return false;
            if (Data != data) Data = data;
            return true;
        }

        public IRandomAccessStream Data
        {
            get => AccessStreamFieldModel.Data;
            set
            {
                if (value.Equals(AccessStreamFieldModel.Data)) return;
                AccessStreamFieldModel.Data = value;
                UpdateOnServer();
                OnFieldModelUpdated(null);
            }
        }
        public override TypeInfo TypeInfo => TypeInfo.AccessStream;

        public override string ToString() => Data.ToString();

        public override FieldControllerBase Copy() => new AccessStreamController(Data);

        public override StringSearchModel SearchForString(string searchString)
        {
            var reg = new System.Text.RegularExpressions.Regex(searchString);
            return searchString == null || (Data.ToString().Contains(searchString.ToLower()) || reg.IsMatch(Data.ToString())) ? new StringSearchModel(Data.ToString()) : StringSearchModel.False;
        }
    }
}
