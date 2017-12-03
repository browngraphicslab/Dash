﻿using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;

namespace Dash
{
    public class IOInfo : ISerializable
    {
        public TypeInfo Type { get; set; }

        public bool IsRequired { get; set; }

        public IOInfo(TypeInfo type, bool isRequired)
        {
            Type = type;
            IsRequired = isRequired;
        }
    }

    public abstract class OperatorController : FieldModelController<OperatorModel>
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
        public abstract void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs);

        /// <summary>
        /// Create a new <see cref="OperatorController"/> associated with the passed in <see cref="OperatorFieldModel" />
        /// </summary>
        /// <param name="operatorFieldModel">The model which this controller will be operating over</param>
        protected OperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
            OperatorFieldModel = operatorFieldModel;
        }

        public override void Init()
        {
        }

        /// <summary>
        /// The <see cref="OperatorFieldModel" /> associated with this <see cref="OperatorController" />,
        /// You should only set values on the controller, never directly on the model!
        /// </summary>
        protected OperatorModel OperatorFieldModel { get; set; }

        /// <summary>
        /// Returns the string-representation name of the operator's type.
        /// </summary>
        /// <returns></returns>
        public string GetOperatorType() { return OperatorFieldModel.Type.ToString(); }

        public override TypeInfo TypeInfo => TypeInfo.Operator;

        public override FrameworkElement GetTableCellView(Context context)
        {
            return GetTableCellViewOfScrollableText(BindTextOrSetOnce);
        }

        private void BindTextOrSetOnce(TextBlock textBlock)
        {
            textBlock.Text = $"Operator of type: {OperatorFieldModel.Type}";
        }

        public override FieldControllerBase GetDefaultController()
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
