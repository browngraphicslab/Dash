﻿using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;

namespace Dash
{
    public class IOInfo
    {
        public TypeInfo Type { get; set; }

        public bool IsRequired { get; set; }

        public IOInfo(TypeInfo type, bool isRequired)
        {
            Type = type;
            IsRequired = isRequired;
        }
    }

    public abstract class OperatorFieldModelController : FieldModelController
    {
        /// <summary>
        /// Keys of all inputs to the operator Document 
        /// </summary>
        public abstract ObservableDictionary<KeyController, IOInfo> Inputs { get; }

        /// <summary>
        /// Keys of all outputs of the operator Document 
        /// </summary>
        public abstract ObservableDictionary<KeyController, TypeInfo> Outputs { get; }

        /// <summary>
        /// Abstract method to execute the operator.
        /// </summary>
        /// <returns></returns>
        public abstract void Execute(Dictionary<KeyController, FieldModelController> inputs, Dictionary<KeyController, FieldModelController> outputs);

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

        /// <summary>
        /// Returns the string-representation name of the operator's type.
        /// </summary>
        /// <returns></returns>
        public string GetOperatorType() { return OperatorFieldModel.Type; }

        public override TypeInfo TypeInfo => TypeInfo.Operator;

        public override FrameworkElement GetTableCellView(Context context)
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

        public virtual void SetDocumentController(DocumentController dc)
        {

        }

        public bool IsCompound()
        {
            return OperatorFieldModel.IsCompound;
        }
    }
}
