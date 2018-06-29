using System.Collections.Generic;
using System.Collections.ObjectModel;
using DashShared;

namespace Dash
{
    public abstract class BinaryOperatorControllerBase<T, U> : OperatorController where T : FieldControllerBase where U : FieldControllerBase
    {
        protected BinaryOperatorControllerBase(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("6FDEC187-64E7-4926-923E-8E383AB5A72B", "Binary Base");

        //Input keys
        public static readonly KeyController LeftKey = new KeyController("942F7A38-3E5D-4CD7-9A88-C61B962511B8", "Left");
        public static readonly KeyController RightKey = new KeyController("F9B2192D-3DFD-41B8-9A37-56D818153B59", "Right");

        //Output keys
        public static readonly KeyController ComputedResultKey = new KeyController("7431D567-7582-477B-A372-5964C2D26AE6", "Computed Result");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(LeftKey, new IOInfo(TypeInfo.Number, true)),
            new KeyValuePair<KeyController, IOInfo>(RightKey, new IOInfo(TypeInfo.Number, true))
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo> { [ComputedResultKey] = TypeInfo.Number };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            outputs[ComputedResultKey] = Compute((T)inputs[LeftKey], (U)inputs[RightKey]);
        }

        public abstract FieldControllerBase Compute(T left, U right);

        public abstract override FieldControllerBase GetDefaultController();
    }
}