using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash.Controllers
{
    class OperatorFieldModelController : FieldModelController
    {
        /// <summary>
        ///     Create a new <see cref="OperatorFieldModelController"/> associated with the passed in <see cref="OperatorFieldModel" />
        /// </summary>
        /// <param name="operatorFieldModel">The model which this controller will be operating over</param>
        public OperatorFieldModelController(OperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
            OperatorFieldModel = operatorFieldModel;
        }

        /// <summary>
        ///     The <see cref="OperatorFieldModel" /> associated with this <see cref="OperatorFieldModelController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public OperatorFieldModel OperatorFieldModel { get; }

    }
}
