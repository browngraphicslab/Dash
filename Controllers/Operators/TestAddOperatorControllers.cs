using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType()]
    public class TestAddOperatorControllerFour : OperatorController
    {
        public static readonly KeyController AKey = KeyController.Get("A");
        public static readonly KeyController BKey = KeyController.Get("B");
        public static readonly KeyController CKey = KeyController.Get("C");
        public static readonly KeyController DKey = KeyController.Get("D");

        public static readonly KeyController OutputKey = KeyController.Get("Output");

        public TestAddOperatorControllerFour() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public TestAddOperatorControllerFour(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Test Add four param 2", new Guid("0DB9818C-75C4-4EE6-A237-8DBCDCA7C28F"));

        public override FieldControllerBase GetDefaultController()
        {
            return new TestAddOperatorControllerFour();
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(AKey, new IOInfo(TypeInfo.Number, true)),
            new KeyValuePair<KeyController, IOInfo>(BKey, new IOInfo(TypeInfo.Number, true)),
            new KeyValuePair<KeyController, IOInfo>(CKey, new IOInfo(TypeInfo.Number, true)),
            new KeyValuePair<KeyController, IOInfo>(DKey, new IOInfo(TypeInfo.Number, true))
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [OutputKey] = TypeInfo.Number
        };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var a = ((NumberController)inputs[AKey]).Data;
            var b = ((NumberController)inputs[BKey]).Data;
            var c = ((NumberController)inputs[CKey]).Data;
            var d = ((NumberController)inputs[DKey]).Data;
            outputs[OutputKey] = new NumberController(a + b + c + d);
            return Task.CompletedTask;
        }
    }

    [OperatorType()]
    public class TestAddOperatorControllerOne : OperatorController
    {
        public static readonly KeyController AKey = KeyController.Get("A");

        public static readonly KeyController OutputKey = KeyController.Get("Output");

        public TestAddOperatorControllerOne() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public TestAddOperatorControllerOne(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Test Add one param 1", new Guid("DABC33AD-193C-4434-AF77-33CA87F26BB5"));

        public override FieldControllerBase GetDefaultController()
        {
            return new TestAddOperatorControllerOne();
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(AKey, new IOInfo(TypeInfo.Number, true)),
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [OutputKey] = TypeInfo.Number
        };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var a = ((NumberController)inputs[AKey]).Data;
            outputs[OutputKey] = new NumberController(a);
            return Task.CompletedTask;
        }
    }

    [OperatorType()]
    public class TestAddOperatorControllerInheritanceA : OperatorController
    {
        public static readonly KeyController AKey = KeyController.Get("A");
        public static readonly KeyController BKey = KeyController.Get("B");

        public static readonly KeyController OutputKey = KeyController.Get("Output");

        public TestAddOperatorControllerInheritanceA() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public TestAddOperatorControllerInheritanceA(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Test Add inheritance-based ambiguity 2", new Guid("21509C51-0134-421B-A1A7-4833DCAFCDC4"));

        public override FieldControllerBase GetDefaultController() => new TestAddOperatorControllerInheritanceA();

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(AKey, new IOInfo(TypeInfo.Any, true)),
            new KeyValuePair<KeyController, IOInfo>(BKey, new IOInfo(TypeInfo.Number, true)),
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [OutputKey] = TypeInfo.Number
        };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var a = ((NumberController)inputs[AKey]).Data;
            var b = ((NumberController)inputs[BKey]).Data;
            outputs[OutputKey] = new NumberController(a + b);
            return Task.CompletedTask;
        }
    }

    [OperatorType()]
    public class TestAddOperatorControllerInheritanceB : OperatorController
    {
        public static readonly KeyController AKey = KeyController.Get("A");
        public static readonly KeyController BKey = KeyController.Get("B");

        public static readonly KeyController OutputKey = KeyController.Get("Output");

        public TestAddOperatorControllerInheritanceB() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public TestAddOperatorControllerInheritanceB(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Test Add inheritance-based ambiguity 1", new Guid("BC3963BC-9AFC-4122-823F-2427FE54B336"));

        public override FieldControllerBase GetDefaultController() => new TestAddOperatorControllerInheritanceB();

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(AKey, new IOInfo(TypeInfo.Number, true)),
            new KeyValuePair<KeyController, IOInfo>(BKey, new IOInfo(TypeInfo.Any, true)),
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [OutputKey] = TypeInfo.Number
        };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var a = ((NumberController)inputs[AKey]).Data;
            var b = ((NumberController)inputs[BKey]).Data;
            outputs[OutputKey] = new NumberController(a + b);
            return Task.CompletedTask;
        }
    }
}
