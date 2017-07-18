using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using DashShared;

namespace Dash
{
    public abstract class OperatorFieldModelController : FieldModelController
    {
        /// <summary>
        /// Keys of all inputs to the operator Document 
        /// </summary>
        public abstract ObservableDictionary<Key, TypeInfo> Inputs { get; }

        /// <summary>
        /// Keys of all outputs of the operator Document 
        /// </summary>
        public abstract ObservableDictionary<Key, TypeInfo> Outputs { get; }

        /// <summary>
        /// Abstract method to execute the operator.
        /// </summary>
        /// <returns></returns>
        public abstract void Execute(Dictionary<Key, FieldModelController> inputs, Dictionary<Key, FieldModelController> outputs);

        /// <summary>
        /// Create a new <see cref="OperatorFieldModelController"/> associated with the passed in <see cref="OperatorFieldModel" />
        /// </summary>
        /// <param name="operatorFieldModel">The model which this controller will be operating over</param>
        protected OperatorFieldModelController(OperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
            OperatorFieldModel = operatorFieldModel;
        }

        /// <summary>
        /// The <see cref="OperatorFieldModel" /> associated with this <see cref="OperatorFieldModelController" />,
        /// You should only set values on the controller, never directly on the model!
        /// </summary>
        protected OperatorFieldModel OperatorFieldModel { get; set; }
        public override TypeInfo TypeInfo => TypeInfo.Operator;

        public override FrameworkElement GetTableCellView()
        {
            return GetTableCellViewOfScrollableText(BindTextOrSetOnce);
        }

        private void BindTextOrSetOnce(TextBlock textBlock)
        {
            textBlock.Text = $"Operator of type: {OperatorFieldModel.Type}";
        }

        public override FieldModelController GetDefaultController()
        {
            throw new NotImplementedException();
        }
    }
}
