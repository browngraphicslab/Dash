﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;

namespace Dash
{
    public class IOInfo : ISerializable
    {
        /// <summary>
        /// The Data Type of the parameter, number, text, image for example
        /// </summary>
        public TypeInfo Type { get; set; }

        /// <summary>
        /// True if the parameter is required for the operator to run
        /// false otherwise, operators will not executed until all their required
        /// parameters have been set
        /// </summary>
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
        /// The unique type of the operator, necessary for persistence and serialization
        /// </summary>
        public abstract KeyController OperatorType { get; }

        /// <summary>
        /// Function which provides an optional layout for the operator no need to override this unless you
        ///  are using a custom layout
        /// </summary>
        public virtual Func<ReferenceController, CourtesyDocument> LayoutFunc { get; } = null;

        /// <summary>
        /// Abstract method to execute the operator.
        /// </summary>
        /// <returns></returns>
        public abstract void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args);

        /// <summary>
        /// Create a new <see cref="OperatorController"/> associated with the passed in <see cref="OperatorFieldModel" />
        /// </summary>
        /// <param name="operatorFieldModel">The model which this controller will be operating over</param>
        protected OperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
            OperatorFieldModel = operatorFieldModel;
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

        /// <summary>
        /// Get the type of the field, operators are always of the same type
        /// </summary>
        public sealed override TypeInfo TypeInfo => TypeInfo.Operator;

        public override StringSearchModel SearchForString(string searchString)
        {
            return StringSearchModel.False;
        }

        public sealed override object GetValue(Context context)
        {
            // getvalue does not mean anything on an operator since
            // operators don't have any implicit value, the value is defind
            // by the input references passed to it in execute
            throw new System.NotImplementedException();
        }

        public sealed override bool TrySetValue(object value)
        {
            // cannot set a value on an operator
            return false;
        }

        public override void Init()
        {
            // operators can optionally override the init method if they have
            // to chase down pointers
        }

        public sealed override FieldModelController<OperatorModel> Copy()
        {
            var operatorCopy = GetDefaultController();
            Debug.Assert(operatorCopy is FieldModelController<OperatorModel>);
            return (FieldModelController <OperatorModel> ) operatorCopy;
        }
    }
}
