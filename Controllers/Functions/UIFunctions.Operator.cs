using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Dash
{
[OperatorType(Op.Name.text_input)]
public sealed class TextInputOperator : OperatorController
{
    //Output Keys
    public static readonly KeyController Output0Key = KeyController.Get("Output0");

    public TextInputOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public TextInputOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("TextInputOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new TextInputOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
        [Output0Key] = DashShared.TypeInfo.Text,
    };

    public override async Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        var output0 = await Dash.UIFunctions.TextInput();
        outputs[Output0Key] = output0;
    }

}

[OperatorType(Op.Name.toggle_presentation)]
public sealed class TogglePresentationOperator : OperatorController
{

    public TogglePresentationOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public TogglePresentationOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("TogglePresentationOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new TogglePresentationOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        Dash.UIFunctions.TogglePresentation();
        return Task.CompletedTask;
    }

}

[OperatorType(Op.Name.export_workspace)]
public sealed class ExportWorkspaceOperator : OperatorController
{

    public ExportWorkspaceOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public ExportWorkspaceOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("ExportWorkspaceOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new ExportWorkspaceOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        Dash.UIFunctions.ExportWorkspace();
        return Task.CompletedTask;
    }

}

}
