using System.Globalization;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

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
            return GetTableCellViewOfScrollableText(BindTextOrSetOnce);
        }

        protected void BindTextOrSetOnce(TextBlock textBlock)
        {
            var textBinding = new Binding
            {
                Source = this,
                Path = new PropertyPath(nameof(Data)),
                Mode = BindingMode.OneWay
            };
            textBlock.SetBinding(TextBlock.TextProperty, textBinding);
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
