using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name.add, Op.Name.concat, Op.Name.operator_add)]
    public sealed class ConcatOperatorController : OperatorController
    {
        public static readonly KeyController AKey = KeyController.Get("A");
        public static readonly KeyController BKey = KeyController.Get("B");

        public static readonly KeyController OutputKey = KeyController.Get("Output");


        public ConcatOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public ConcatOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Concat", new Guid("F69DF9CF-5B51-482D-AE1E-40B3266930CB"));

        public override FieldControllerBase GetDefaultController()
        {
            return new ConcatOperatorController();
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(AKey, new IOInfo(TypeInfo.Text, true)),
            new KeyValuePair<KeyController, IOInfo>(BKey, new IOInfo(TypeInfo.Text, true)),
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [OutputKey] = TypeInfo.Text
        };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var a = (inputs[AKey] as TextController)?.Data;
            var b = (inputs[BKey] as TextController)?.Data;
            outputs[OutputKey] = new TextController(a + b);
            return Task.CompletedTask;
        }
    }
}
