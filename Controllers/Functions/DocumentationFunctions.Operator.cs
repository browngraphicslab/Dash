using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Dash
{
[OperatorType(Op.Name.help)]
public sealed class HelpOperator : OperatorController
{
    //Input Keys
    public static readonly KeyController FuncNameKey = KeyController.Get("FuncName");

    //Output Keys
    public static readonly KeyController HelpStringKey = KeyController.Get("HelpString");

    public HelpOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public HelpOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("HelpOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new HelpOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
        new KeyValuePair<KeyController, IOInfo>(FuncNameKey, new IOInfo(DashShared.TypeInfo.Text, false)),
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
        [HelpStringKey] = DashShared.TypeInfo.Any,
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        var funcName = inputs[FuncNameKey] as TextController;
        var helpString = Dash.DocumentationFunctions.Help(funcName);
        outputs[HelpStringKey] = helpString;
        return Task.CompletedTask;
    }

}

}
