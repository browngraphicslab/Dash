using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Dash
{
[OperatorType(Op.Name.copy)]
public sealed class CopyOperator : OperatorController
{
    //Input Keys
    public static readonly KeyController FieldKey = KeyController.Get("Field");

    //Output Keys
    public static readonly KeyController CopyKey = KeyController.Get("Copy");

    public CopyOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public CopyOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("CopyOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new CopyOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
        new KeyValuePair<KeyController, IOInfo>(FieldKey, new IOInfo(DashShared.TypeInfo.Any, true)),
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
        [CopyKey] = DashShared.TypeInfo.Any,
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        var field = (FieldControllerBase)inputs[FieldKey];
        var copy = Dash.UtilFunctions.Copy(field);
        outputs[CopyKey] = copy;
        return Task.CompletedTask;
    }

}

}
