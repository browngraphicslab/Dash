using System.Collections.Generic;
using DashShared;

namespace Dash
{
    public abstract class OperatorFieldModelController : FieldModelController
    {
        /// <summary>
        /// Keys of all inputs to the operator Document 
        /// </summary>
        public abstract List<Key> InputKeys { get; }

        /// <summary>
        /// Keys of all outputs of the operator Document 
        /// </summary>
        public abstract List<Key> OutputKeys { get; }

        /// <summary>
        /// Abstract method to execute the operator.
        /// </summary>
        /// <returns></returns>
        public abstract void Execute(DocumentController doc);

        public abstract List<FieldModel> GetNewInputFields();

        /// <summary>
        /// Returns a list of fieldmodels corresponsing to the keys of OutputKeys.
        /// </summary>
        /// <returns></returns>
        public abstract List<FieldModel> GetNewOutputFields();

        /// <summary>
        /// Create a new <see cref="OperatorFieldModelController"/> associated with the passed in <see cref="OperatorFieldModel" />
        /// </summary>
        /// <param name="operatorFieldModel">The model which this controller will be operating over</param>
        public OperatorFieldModelController(OperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
            OperatorFieldModel = operatorFieldModel;
        }

        /// <summary>
        /// The <see cref="OperatorFieldModel" /> associated with this <see cref="OperatorFieldModelController" />,
        /// You should only set values on the controller, never directly on the model!
        /// </summary>
        protected OperatorFieldModel OperatorFieldModel { get; set; }

    }
}
