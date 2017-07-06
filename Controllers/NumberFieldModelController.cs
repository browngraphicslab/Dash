using System.Globalization;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Dash
{
    public class NumberFieldModelController : FieldModelController
    {
        /// <summary>
        ///     Create a new <see cref="NumberFieldModelController"/> associated with the passed in <see cref="Dash.NumberFieldModel" />
        /// </summary>
        /// <param name="numberFieldModel">The model which this controller will be operating over</param>
        public NumberFieldModelController(NumberFieldModel numberFieldModel) : base(numberFieldModel)
        {
            NumberFieldModel = numberFieldModel;
        }

        /// <summary>
        ///     The <see cref="NumberFieldModel" /> associated with this <see cref="NumberFieldModelController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public NumberFieldModel NumberFieldModel { get; }

        protected override void UpdateValue(FieldModelController fieldModel)
        {
            Data = (fieldModel as NumberFieldModelController).Data;
        }

        public override FrameworkElement GetTableCellView()
        {
            var textBlockText = NumberFieldModel.Data.ToString(CultureInfo.InvariantCulture);
            return GetTableCellViewOfScrollableText(textBlockText);
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
