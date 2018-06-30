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
}
