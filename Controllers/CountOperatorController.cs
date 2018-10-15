using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.count, Op.Name.len)]
    public sealed class CountOperatorController : OperatorController
    {
        //Input keys
        public static readonly KeyController InputKey = KeyController.Get("Element With Length");

        //Output keys
        public static readonly KeyController LengthKey = KeyController.Get("Computed Length");
        
        public CountOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public CountOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public override FieldControllerBase GetDefaultController() => new CountOperatorController();

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(InputKey, new IOInfo(TypeInfo.Length, true))
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>()
        {
            [LengthKey] = TypeInfo.Number
        };

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Length", new Guid("D8368297-5417-40D8-902F-7F1D431EF227"));

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            switch (inputs[InputKey])
            {
                case BaseListController list:
                    outputs[LengthKey] = new NumberController(list.Count);
                    break;
                case TextController text:
                    outputs[LengthKey] = new NumberController(text.Data.Length);
                    break;
                default:
                    outputs[LengthKey] = new NumberController(-1);
                    break;
            }
            return Task.CompletedTask;
        }
    }
}
