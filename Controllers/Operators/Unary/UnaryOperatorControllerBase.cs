using System.Collections.Generic;
using System.Collections.ObjectModel;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    public abstract class UnaryOperatorControllerBase<T> : OperatorController where T : FieldControllerBase
    {
        protected UnaryOperatorControllerBase(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("606DC55B-3D0B-4058-AB94-690A60C58183", "Unary Base");

        //Input keys
        public static readonly KeyController InKey = new KeyController("A4C5F8BC-1DB6-436E-B5B0-03296140B798", "In");

        //Output keys
        public static readonly KeyController ComputedResultKey = new KeyController("1124B2E1-8BF9-489F-B416-6F26D31DDE5A", "Computed Result");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(InKey, new IOInfo(TypeInfo.Number, true)),
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo> { [ComputedResultKey] = TypeInfo.Number };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            outputs[ComputedResultKey] = Compute((T)inputs[InKey]);
        }

        public abstract FieldControllerBase Compute(T inContent);

        public abstract override FieldControllerBase GetDefaultController();
    }
}
