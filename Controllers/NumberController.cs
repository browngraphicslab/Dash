using DashShared;

namespace Dash
{
    public class NumberController : FieldModelController<NumberModel>
    {
        public NumberController() : this(0)
        {

        }

        public NumberController(double data = 0) : base(new NumberModel(data))
        {
        }

        public NumberController(NumberModel numberFieldModel) : base(numberFieldModel)
        {

        }

        /// <summary>
        ///     The <see cref="NumberFieldModel" /> associated with this <see cref="NumberController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public NumberModel NumberFieldModel => Model as NumberModel;


        public override FieldControllerBase GetDefaultController()
        {
            return new NumberController(0);
        }

        public override object GetValue(Context context)
        {
            return Data;
        }
        public override bool TrySetValue(object value)
        {
            if (value is double dub)
            {
                Data = dub;
                return true;
            }
            if (value is float flt)
            {
                Data = flt;
                return true;
            }
            if (value is int intn)
            {
                Data = intn;
                return true;
            }
            return false;
        }

        public double Data
        {
            get => NumberFieldModel.Data;
            set
            {
                if (NumberFieldModel.Data != value)
                {
                    double data = NumberFieldModel.Data;
                    UndoCommand newEvent = new UndoCommand(() => Data = value, () => Data = data);

                    NumberFieldModel.Data = value;
                    UpdateOnServer(newEvent);
                    OnFieldModelUpdated(null);
                }
            }
        }

        public override TypeInfo TypeInfo => TypeInfo.Number;

        public override string ToString()
        {
            return Data.ToString();
        }

        public override StringSearchModel SearchForString(Search.SearchMatcher matcher)
        {
            return matcher.Matches(Data.ToString());
        }

        public override string ToScriptString(DocumentController thisDoc = null)
        {
            return Data.ToString();
        }

        public override FieldControllerBase Copy()
        {
            return new NumberController(Data);
        }
    }
}
