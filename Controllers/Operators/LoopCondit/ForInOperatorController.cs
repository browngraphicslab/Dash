using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private static readonly KeyController TypeKey = new KeyController("D3C64EC0-B4D4-43BC-B055-4EF8B11A2B97", "For In");

        //Input keys
        public static readonly KeyController SubVarNameKey = new KeyController("FB97A9E7-8D2B-444A-8817-DD6ABE12D134", "Invokable name of dummy variable");
        public static readonly KeyController SubVarDeclarationKey = new KeyController("E8564E98-7707-48BE-BD48-33ED2B2E8F40", "Declaration of dummy variable");
        public static readonly KeyController ListNameKey = new KeyController("25C0265B-4D73-4EA8-BDA4-05B403CEAEBB", "List over which to iterate");
        public static readonly KeyController ForInBlockKey = new KeyController("24ABB93D-A1A2-4B7C-BCE1-502DFF75291C", "The body contained in the for in block");
        public static readonly KeyController CounterKey = new KeyController("11905E2B-63D4-434E-9903-9A0BF1BD28CD", "The phantom variable counter");
        public static readonly KeyController CounterDeclarationKey = new KeyController("67F5C326-CCA7-40AB-9AF2-3E1ED72EB0A3", "The phantom variable counter declaration");
        public static readonly KeyController IncrementAndAssignmentKey = new KeyController("48C63629-B77B-47F7-9346-759714AE31EB", "The incrementation of the phantom variable counter");
        public static readonly KeyController WriteToListKey = new KeyController("C85785DE-A25E-4638-B3A3-CD6EEBBF63CB", "Takes the output of manipulation and stores it in list");

        //Output keys
        public static readonly KeyController ResultKey = new KeyController("A6C8661F-E91A-49DE-820A-3F93B65030CA", "Result");

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
        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            outputs[ResultKey] = new NumberController(0);
        }

        public override FieldControllerBase GetDefaultController() => new ForOperatorController();
    }
}
