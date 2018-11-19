using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Dash
{
[OperatorType(Op.Name.get_selected_docs)]
public sealed class GetSelectedDocsOperator : OperatorController
{
    //Output Keys
    public static readonly KeyController SelectedDocsKey = KeyController.Get("SelectedDocs");

    public GetSelectedDocsOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public GetSelectedDocsOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("GetSelectedDocsOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new GetSelectedDocsOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
        [SelectedDocsKey] = DashShared.TypeInfo.List,
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        var selectedDocs = Dash.SelectionFunctions.GetSelectedDocs();
        outputs[SelectedDocsKey] = selectedDocs;
        return Task.CompletedTask;
    }

}

[OperatorType(Op.Name.active_document)]
public sealed class ActiveDocumentOperator : OperatorController
{
    //Output Keys
    public static readonly KeyController Output0Key = KeyController.Get("Output0");

    public ActiveDocumentOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public ActiveDocumentOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("ActiveDocumentOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new ActiveDocumentOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
        [Output0Key] = DashShared.TypeInfo.Document,
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        var output0 = Dash.SelectionFunctions.ActiveDocument();
        outputs[Output0Key] = output0;
        return Task.CompletedTask;
    }

}

}
