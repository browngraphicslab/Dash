using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.for_in_lp)]
    public class ForInOperatorController : OperatorController
    {
        public ForInOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public ForInOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("For In");

        //Input keys
        public static readonly KeyController SubVarNameKey             = KeyController.Get("Invokable name of dummy variable");
        public static readonly KeyController SubVarDeclarationKey      = KeyController.Get("Declaration of dummy variable");
        public static readonly KeyController ListNameKey               = KeyController.Get("List over which to iterate");
        public static readonly KeyController ForInBlockKey             = KeyController.Get("The body contained in the for in block");
        public static readonly KeyController CounterKey                = KeyController.Get("The phantom variable counter");
        public static readonly KeyController CounterDeclarationKey     = KeyController.Get("The phantom variable counter declaration");
        public static readonly KeyController IncrementAndAssignmentKey = KeyController.Get("The incrementation of the phantom variable counter");
        public static readonly KeyController WriteToListKey            = KeyController.Get("Takes the output of manipulation and stores it in list");

        //Output keys
        public static readonly KeyController ResultKey = KeyController.Get("Result");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(SubVarNameKey, new IOInfo(TypeInfo.Any, true)),
            new KeyValuePair<KeyController, IOInfo>(ListNameKey, new IOInfo(TypeInfo.Any, true)),
            new KeyValuePair<KeyController, IOInfo>(ForInBlockKey, new IOInfo(TypeInfo.Any, true)),
            new KeyValuePair<KeyController, IOInfo>(CounterDeclarationKey, new IOInfo(TypeInfo.Any, true)),
            new KeyValuePair<KeyController, IOInfo>(IncrementAndAssignmentKey, new IOInfo(TypeInfo.Any, true))
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ResultKey] = TypeInfo.Any,
        };

        //TODO: remove requirement that output exists
        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            outputs[ResultKey] = new NumberController(0);
            return Task.CompletedTask;
        }

        public override FieldControllerBase GetDefaultController() => new ForOperatorController();
    }
}
