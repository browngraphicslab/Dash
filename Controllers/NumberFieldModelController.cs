using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public double Data
        {
            get { return NumberFieldModel.Data; }
            set
            {
                if (SetProperty(ref NumberFieldModel.Data, value))
                {
                    OnDataUpdated();
                    // update local
                    // update server
                }
            }
        }
    }
}
