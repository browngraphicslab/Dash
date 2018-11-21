using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Dash
{
[OperatorType(Op.Name.alias_of)]
public sealed class AliasOfOperator : OperatorController
{
    //Input Keys
    public static readonly KeyController DocKey = KeyController.Get("Doc");

    //Output Keys
    public static readonly KeyController Output0Key = KeyController.Get("Output0");

    public AliasOfOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public AliasOfOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("AliasOfOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new AliasOfOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
        new KeyValuePair<KeyController, IOInfo>(DocKey, new IOInfo(DashShared.TypeInfo.Document, true)),
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
        [Output0Key] = DashShared.TypeInfo.List,
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        var doc = (DocumentController)inputs[DocKey];
        var output0 = Dash.SearchFunctions.AliasOf(doc);
        outputs[Output0Key] = output0;
        return Task.CompletedTask;
    }

}

}
