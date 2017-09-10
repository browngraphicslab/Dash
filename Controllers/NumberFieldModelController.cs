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
        public NumberFieldModelController(double data = 0) : base(new NumberFieldModel(data), false)
        {
        }

        private NumberFieldModelController(NumberFieldModel numberFieldModel) : base(numberFieldModel, true)
        {
            
        }

        public static NumberFieldModelController CreateFromServer(NumberFieldModel numberFieldModel)
        {
            return ContentController.GetController<NumberFieldModelController>(numberFieldModel.Id) ??
                     new NumberFieldModelController(numberFieldModel);
        }

        /// <summary>
        ///     The <see cref="NumberFieldModel" /> associated with this <see cref="NumberFieldModelController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public NumberFieldModel NumberFieldModel => FieldModel as NumberFieldModel;


        public override FieldModelController GetDefaultController()
        {
            return new NumberFieldModelController(0);
        }

        public override object GetValue(Context context)
        {
            return Data;
        }
        public override bool SetValue(object value)
        {
            var data = value as double?;
            if (data != null)
            {
                Data = (double)data.Value;
                return true;
            }
            if (value is double)
            {
                Data = (double)data;
                return true;
            }
            return false;
        }

        public double Data
        {
            get { return NumberFieldModel.Data; }
            set
            {
                if (SetProperty(ref NumberFieldModel.Data, value))
                {
                    // Update the server
                    RESTClient.Instance.Fields.UpdateField(FieldModel, dto =>
                    {

                    }, exception =>
                    {

                    });
                    OnFieldModelUpdated(null);
                    // update local
                    // update server
                }
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
