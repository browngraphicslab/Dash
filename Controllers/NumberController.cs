using System;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web.Provider;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using DashShared;
using DashShared.Models;

namespace Dash
{
    public class NumberController : FieldModelController<NumberModel>
    {
        public NumberController(double data = 0) : base(new NumberModel(data))
        {
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
            get => NumberFieldModel.Data;
            set
            {
                if (!value.Equals(NumberFieldModel.Data))
                {
                    NumberFieldModel.Data = value;
                    OnFieldModelUpdated(null);
                }
            }
        }
        public override TypeInfo TypeInfo => TypeInfo.Number;

        public override string ToString()
        {
            return Data.ToString();
        }

        public override FieldModelController<NumberModel> Copy()
        {
            return new NumberController(Data);
        }
    }
}
