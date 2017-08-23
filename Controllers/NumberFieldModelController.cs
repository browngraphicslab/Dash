using System;
using Windows.Security.Authentication.Web.Provider;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using DashShared;

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
            Data = numberFieldModel.Data;
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
        public override FieldModelController GetDefaultController()
        {
            return new NumberFieldModelController(0);
        }

        public override object GetValue()
        {
            return Data;
        }
        public override void SetValue(object value)
        {
            var data = value as double?;
            if (data != null)
            {
                Data = data.Value;
            }
            if (value is double)
                Data = (double)data;
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
                OnFieldModelUpdated(null);
            }
        }
        public override TypeInfo TypeInfo => TypeInfo.Number;

        public override string ToString()
        {
            return Data.ToString();
        }

        public override FieldModelController Copy()
        {
            return new NumberFieldModelController(Data);
        }
    }
}
