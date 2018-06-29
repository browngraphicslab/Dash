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
            SaveOnServer();
        }

        public NumberController(NumberModel numberFieldModel) : base(numberFieldModel)
        {

        }

        public override void Init()
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
            var data = value as double?;
            if (value is double?)
            {
                if (Data != (double)data.Value)
                    Data = (double)data.Value;
                return true;
            }
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
            set {
                if (NumberFieldModel.Data != value) {
                    SetData(value);
                }
            }
        }

        /*
       * Sets the data property and gives UpdateOnServer an UndoCommand 
       */
        private void SetData(double val, bool withUndo = true)
        {
            double data = NumberFieldModel.Data;
            UndoCommand newEvent = new UndoCommand(() => SetData(val, false), () => SetData(data, false));

            NumberFieldModel.Data = val;
            UpdateOnServer(withUndo ? newEvent : null);
            OnFieldModelUpdated(null);
        }

        public override TypeInfo TypeInfo => TypeInfo.Number;

        public override string ToString()
        {
            return Data.ToString();
        }

        public override StringSearchModel SearchForString(string searchString)
        {
            var reg = new System.Text.RegularExpressions.Regex(searchString);
            return searchString == null || (Data.ToString().Contains(searchString.ToLower()) || reg.IsMatch(Data.ToString())) ? new StringSearchModel(Data.ToString()) :StringSearchModel.False; 
        }

        public override FieldControllerBase Copy()
        {
            return new NumberController(Data);
        }
    }
}
