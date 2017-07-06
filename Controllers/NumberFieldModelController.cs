namespace Dash
{
    public class NumberFieldModelController : FieldModelController
    {
        public NumberFieldModelController(double data = 0) : base(new NumberFieldModel(data))
        {
        }

        /// <summary>
        ///     Create a new <see cref="NumberFieldModelController"/> associated with the passed in <see cref="Dash.NumberFieldModel" />
        /// </summary>
        /// <param name="numberFieldModel">The model which this controller will be operating over</param>
        private NumberFieldModelController(NumberFieldModel numberFieldModel) : base(numberFieldModel)
        {
        }

        /// <summary>
        ///     The <see cref="NumberFieldModel" /> associated with this <see cref="NumberFieldModelController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public NumberFieldModel NumberFieldModel => FieldModel as NumberFieldModel;

        protected override void UpdateValue(FieldModelController fieldModel)
        {
            Data = (fieldModel as NumberFieldModelController).Data;
        }

        public double Data
        {
            get { return NumberFieldModel.Data; }
            set
            {
                if (SetProperty(ref NumberFieldModel.Data, value))
                {
                    // update local
                    // update server
                }
                FireFieldModelUpdated();
            }
        }

        public override string ToString()
        {
            return Data.ToString();
        }
    }
}
