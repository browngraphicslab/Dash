using System.Collections.Generic;
using System.Collections.ObjectModel;
using DashShared;

namespace Dash
{
    public abstract class BinaryOperatorControllerBase<T, U> : OperatorController where T : FieldControllerBase where U : FieldControllerBase
    {
        protected BinaryOperatorControllerBase(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        //Input keys
        public static readonly KeyController LeftKey = new KeyController("Left");
        public static readonly KeyController RightKey = new KeyController("Right");

        //Output keys
        public static readonly KeyController ComputedResultKey = new KeyController("Computed Result");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(LeftKey, new IOInfo(FieldControllerFactory.GetTypeInfo<T>(), true)),
            new KeyValuePair<KeyController, IOInfo>(RightKey, new IOInfo(FieldControllerFactory.GetTypeInfo<U>(), true))
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
