using DashShared;

namespace Dash
{
    public class BoolController : FieldModelController<BoolModel>
    {
        //OVERLOADED CONSTRUCTORS
        public BoolController() : this(false) { }

        public BoolController(bool data = false) : base(new BoolModel(data)) { }

        public BoolController(BoolModel boolFieldModel) : base(boolFieldModel) { }

        /// <summary>
        ///     The <see cref="BoolFieldModel" /> associated with this <see cref="Dash.BoolController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public BoolModel BoolFieldModel => Model as BoolModel;

        public override FieldControllerBase GetDefaultController() => new BoolController(false);

        public override object GetValue(Context context) => Data;

        public override bool TrySetValue(object value)
        {
            if (value is bool b)
            {
                Data = b;
                return true;
            }

            return false;
        }

        public bool Data
        {
            get => BoolFieldModel.Data;
            set
            {
                if (BoolFieldModel.Data != value)
                {
                    bool data = BoolFieldModel.Data;
                    UndoCommand newEvent = new UndoCommand(() => Data = value, () => Data = data);

                    BoolFieldModel.Data = value;
                    UpdateOnServer(newEvent);
                    OnFieldModelUpdated(null);
                }
            }
        }

        public override TypeInfo TypeInfo => TypeInfo.Bool;

        public override string ToString() => Data.ToString();

        public override FieldControllerBase Copy() => new BoolController(Data);

        public override StringSearchModel SearchForString(string searchString)
        {
            var reg = new System.Text.RegularExpressions.Regex(searchString);
            return searchString == null || (Data.ToString().Contains(searchString.ToLower()) || reg.IsMatch(Data.ToString())) ? new StringSearchModel(Data.ToString()) : StringSearchModel.False;
        }

        public override string ToScriptString(DocumentController thisDoc)
        {
            return Data.ToString();
        }
    }
}
