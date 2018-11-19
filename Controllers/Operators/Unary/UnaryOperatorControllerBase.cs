using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    public abstract class UnaryOperatorControllerBase<T> : OperatorController where T : FieldControllerBase
    {
        protected UnaryOperatorControllerBase(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Unary Base");

        //Input keys
        public static readonly KeyController InKey = KeyController.Get("In");

        //Output keys
        public static readonly KeyController ComputedResultKey = KeyController.Get("Computed Result");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(InKey, new IOInfo(TypeInfo.Number, true)),
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo> { [ComputedResultKey] = TypeInfo.Number };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            outputs[ComputedResultKey] = Compute((T)inputs[InKey]);
            return Task.CompletedTask;
        }

        public abstract FieldControllerBase Compute(T inContent);

        public abstract override FieldControllerBase GetDefaultController();
    }
}
