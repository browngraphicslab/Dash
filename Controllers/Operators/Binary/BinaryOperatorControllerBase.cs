using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public abstract class BinaryOperatorControllerBase<T, U> : OperatorController where T : FieldControllerBase where U : FieldControllerBase
    {
        protected BinaryOperatorControllerBase(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        //Input keys
        public static readonly KeyController LeftKey = KeyController.Get("Left");
        public static readonly KeyController RightKey = KeyController.Get("Right");

        //Output keys
        public static readonly KeyController ComputedResultKey = KeyController.Get("Computed Result");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(LeftKey, new IOInfo(FieldControllerFactory.GetTypeInfo<T>(), true)),
            new KeyValuePair<KeyController, IOInfo>(RightKey, new IOInfo(FieldControllerFactory.GetTypeInfo<U>(), true))
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo> { [ComputedResultKey] = TypeInfo.Number };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            outputs[ComputedResultKey] = Compute((T)inputs[LeftKey], (U)inputs[RightKey]);
            return Task.CompletedTask;
        }

        public abstract FieldControllerBase Compute(T left, U right);

        public abstract override FieldControllerBase GetDefaultController();
    }
}
