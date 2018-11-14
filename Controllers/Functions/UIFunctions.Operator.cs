using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Dash.Popups;

namespace Dash
{
[OperatorType(Op.Name.manage_behaviors)]
public sealed class TextInputOperator : OperatorController
{
    public static readonly KeyController DocumentKey = KeyController.Get("Document");

    //Output Keys
    public static readonly KeyController Output0Key = KeyController.Get("Output0");

    public TextInputOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public TextInputOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("TextInputOperator");

    public override FieldControllerBase GetDefaultController() => new TextInputOperator();

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
        new KeyValuePair<KeyController, IOInfo>(DocumentKey, new IOInfo(DashShared.TypeInfo.Document, true))
    };

    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
        [Output0Key] = DashShared.TypeInfo.Any
    };

    public override async Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
    {
        var docRef = inputs[DocumentKey] as DocumentController;
        var scripts = await UIFunctions.ManageBehaviors(docRef);
        var scriptsOut = scripts == null ? null : new ListController<FieldControllerBase>(scripts);
        outputs[Output0Key] = scriptsOut;
    }

}

}
