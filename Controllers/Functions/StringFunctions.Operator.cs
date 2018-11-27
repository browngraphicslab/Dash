using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Dash
{
[OperatorType(Op.Name.split)]
public sealed class SplitOperator : OperatorController
{
    //Input Keys
    public static readonly KeyController FieldKey = KeyController.Get("Field");
    public static readonly KeyController DelimiterKey = KeyController.Get("Delimiter");

    //Output Keys
    public static readonly KeyController SplitKey = KeyController.Get("Split");

    public SplitOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public SplitOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("SplitOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new SplitOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
        new KeyValuePair<KeyController, IOInfo>(FieldKey, new IOInfo(DashShared.TypeInfo.Text, true)),
        new KeyValuePair<KeyController, IOInfo>(DelimiterKey, new IOInfo(DashShared.TypeInfo.Text, true)),
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
        [SplitKey] = DashShared.TypeInfo.List,
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        var field = (TextController)inputs[FieldKey];
        var delimiter = (TextController)inputs[DelimiterKey];
        var split = Dash.StringFunctions.Split(field, delimiter);
        outputs[SplitKey] = split;
        return Task.CompletedTask;
    }

}

}
