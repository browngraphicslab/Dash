using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    public class IOInfo : ISerializable
    {
        /// <summary>
        /// The Data Type of the parameter, number, text, image for example
        /// </summary>
        public DashShared.TypeInfo Type { get; set; }

        /// <summary>
        /// True if the parameter is required for the operator to run
        /// false otherwise, operators will not executed until all their required
        /// parameters have been set
        /// </summary>
        public bool IsRequired { get; set; }

        public IOInfo(DashShared.TypeInfo type, bool isRequired)
        {
            Type = type;
            IsRequired = isRequired;
        }
    }


    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class OperatorTypeAttribute : Attribute
    {
        private readonly Op.Name[] _names;
        public double Version;

        public OperatorTypeAttribute(params Op.Name[] names)
        {
            var namesWithUniversalEnums = names.ToList();
            //namesWithUniversalEnums.Add(Op.Name.help);
            _names = namesWithUniversalEnums.ToArray();

            // Default value.
            Version = 1.0;
        }

        public Op.Name[] GetTypeNames()
        {
            return _names;
        }
    }

    public abstract class OperatorController : FieldModelController<OperatorModel>
    {
        /// <summary>
        /// Keys of all inputs to the operator Document 
        /// </summary>
        public abstract ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; }

        /*
        /// <summary>
        /// returns the Inputs in specific order for the operator
        /// </summary>
        public virtual List<KeyController> InputKeyOrder
        {
            get
            {
                return Inputs.Keys.ToList(); 
            }
        }

        public KeyController GetKeyAtIndex(int index)
        {
            return InputKeyOrder[index];
        }*/

        /// <summary>
        /// Keys of all outputs of the operator Document 
        /// </summary>
        public abstract ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; }

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
        public abstract void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null);

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
        public sealed override DashShared.TypeInfo TypeInfo => DashShared.TypeInfo.Operator;

        public override StringSearchModel SearchForString(string searchString)
        {
            return StringSearchModel.False;
        }

        public sealed override object GetValue(Context context)
        {
            // getvalue does not mean anything on an operator since
            // operators don't have any implicit value, the value is defind
            // by the input references passed to it in execute
            return "";
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

        public sealed override FieldControllerBase Copy()
        {
            var operatorCopy = GetDefaultController();
            Debug.Assert(operatorCopy is FieldModelController<OperatorModel>);
            return (FieldModelController<OperatorModel>)operatorCopy;
        }

        public override string ToScriptString(DocumentController thisDoc)
        {
            return DSL.GetFuncName(this).ToString();
        }
    }
}
