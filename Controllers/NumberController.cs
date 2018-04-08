using DashShared;

namespace Dash
{
    public class NumberController : FieldModelController<NumberModel>
    {
        public NumberController() : this(0) { }

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
                Data = (double)data.Value;
                return true;
            }
            if (value is double dub)
            {
                Data = dub;
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
                if (!value.Equals(NumberFieldModel.Data))
                {
                    NumberFieldModel.Data = value;
                    UpdateOnServer();
                    OnFieldModelUpdated(null);
                }
            }
        }
        public override TypeInfo TypeInfo => TypeInfo.Number;

        public override string ToString()
        {
            return Data.ToString();
        }

        public override StringSearchModel SearchForString(string searchString)
        {
            return Data.ToString().Contains(searchString) ? new StringSearchModel(Data.ToString()) :StringSearchModel.False; 
        }

        public override FieldModelController<NumberModel> Copy()
        {
            return new NumberController(Data);
        }
    }
}
