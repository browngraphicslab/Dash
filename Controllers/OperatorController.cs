using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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


    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class OperatorTypeAttribute : Attribute
    {
        private string _name;
        public double version;

        public OperatorTypeAttribute(string name)
        {
            this._name = name;

            // Default value.
            version = 1.0;
        }

        public string GetType()
        {
            return _name;
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
        public abstract ObservableDictionary<KeyController, TypeInfo> Outputs { get; }

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

        public override FieldControllerBase GetDefaultController()
        {
            throw new NotImplementedException();
        }

        public virtual void SetDocumentController(DocumentController dc)
        {

        }

        public override StringSearchModel SearchForString(string searchString)
        {
            return StringSearchModel.False;
        }


        public bool IsCompound()
        {
            return OperatorFieldModel.IsCompound;
        }
    }
}
