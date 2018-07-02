using System.Collections.Generic;
using System.Collections.ObjectModel;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name.add)]
    public class TestAddOperatorControllerFour : OperatorController
    {
        public static readonly KeyController AKey = new KeyController("5B15A261-18BF-479C-8F11-BF167A11B5DC", "A");
        public static readonly KeyController BKey = new KeyController("460427C6-B81C-44F8-AF96-60058BAB4F01", "B");
        public static readonly KeyController CKey = new KeyController("1DA70045-7B5C-4F85-8B0F-B90C24209DEE", "C");
        public static readonly KeyController DKey = new KeyController("62BD5112-3BC4-429A-9D68-E40CD96FAA0C", "D");

        public static readonly KeyController OutputKey = new KeyController("A7D02B59-7D8C-4518-BFEB-06BA17CA28FC", "Output");

        public TestAddOperatorControllerFour() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

        public TestAddOperatorControllerFour(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("0DB9818C-75C4-4EE6-A237-8DBCDCA7C28F", "Test Add four param");

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

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var a = ((NumberController)inputs[AKey]).Data;
            var b = ((NumberController)inputs[BKey]).Data;
            var c = ((NumberController)inputs[CKey]).Data;
            var d = ((NumberController)inputs[DKey]).Data;
            outputs[OutputKey] = new NumberController(a + b + c + d);
        }
    }

    [OperatorType(Op.Name.add)]
    public class TestAddOperatorControllerOne : OperatorController
    {
        public static readonly KeyController AKey = new KeyController("CCA5E3CC-FDB7-4583-A70C-EF0723ED6F8F", "A");

        public static readonly KeyController OutputKey = new KeyController("B85FC7BE-9D88-4B48-90C5-4924385A42DB", "Output");

        public TestAddOperatorControllerOne() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

        public TestAddOperatorControllerOne(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("DABC33AD-193C-4434-AF77-33CA87F26BB5", "Test Add one param");

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

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var a = ((NumberController)inputs[AKey]).Data;
            outputs[OutputKey] = new NumberController(a);
        }
    }

    [OperatorType(Op.Name.ambiguous_add_test)]
    public class TestAddOperatorControllerInheritanceA : OperatorController
    {
        public static readonly KeyController AKey = new KeyController("A4D2EB11-F510-4673-ADBB-3CE7E2B03443", "A");
        public static readonly KeyController BKey = new KeyController("22A6B883-F8DC-4BEA-BB50-EC07B3CA5389", "B");

        public static readonly KeyController OutputKey = new KeyController("FA30068B-70EA-4AA8-9FEB-3079ED52D526", "Output");

        public TestAddOperatorControllerInheritanceA() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

        public TestAddOperatorControllerInheritanceA(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("21509C51-0134-421B-A1A7-4833DCAFCDC4", "Test Add inheritance-based ambiguity");

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

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var a = ((NumberController)inputs[AKey]).Data;
            var b = ((NumberController)inputs[BKey]).Data;
            outputs[OutputKey] = new NumberController(a + b);
        }
    }

    [OperatorType(Op.Name.ambiguous_add_test)]
    public class TestAddOperatorControllerInheritanceB : OperatorController
    {
        public static readonly KeyController AKey = new KeyController("EB1A56DB-2CE3-4523-80F0-8CC9148ADAAE", "A");
        public static readonly KeyController BKey = new KeyController("6B307973-DEF8-444F-B88E-8E5E046AE581", "B");

        public static readonly KeyController OutputKey = new KeyController("313AAE53-B58B-433A-869A-C6C96A4EE345", "Output");

        public TestAddOperatorControllerInheritanceB() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

        public TestAddOperatorControllerInheritanceB(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("BC3963BC-9AFC-4122-823F-2427FE54B336", "Test Add inheritance-based ambiguity");

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

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var a = ((NumberController)inputs[AKey]).Data;
            var b = ((NumberController)inputs[BKey]).Data;
            outputs[OutputKey] = new NumberController(a + b);
        }
    }
}
