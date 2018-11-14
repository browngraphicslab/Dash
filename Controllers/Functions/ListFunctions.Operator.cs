using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Dash
{
[OperatorType(Op.Name.remove)]
public sealed class RemoveOperator : OperatorController
{
    //Input Keys
    public static readonly KeyController ListKey = KeyController.Get("List");
    public static readonly KeyController ItemKey = KeyController.Get("Item");

    //Output Keys
    public static readonly KeyController Output0Key = KeyController.Get("Output0");

    public RemoveOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public RemoveOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("RemoveOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new RemoveOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
        new KeyValuePair<KeyController, IOInfo>(ListKey, new IOInfo(DashShared.TypeInfo.List, true)),
        new KeyValuePair<KeyController, IOInfo>(ItemKey, new IOInfo(DashShared.TypeInfo.Any, true)),
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
        [Output0Key] = DashShared.TypeInfo.Bool,
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        var list = (IListController)inputs[ListKey];
        var item = (FieldControllerBase)inputs[ItemKey];
        var output0 = Dash.ListFunctions.Remove(list, item);
        outputs[Output0Key] = output0;
        return Task.CompletedTask;
    }

}

[OperatorType(Op.Name.remove)]
public sealed class DocumentRemoveOperator : OperatorController
{
    //Input Keys
    public static readonly KeyController CollectionKey = KeyController.Get("Collection");
    public static readonly KeyController ItemKey = KeyController.Get("Item");

    //Output Keys
    public static readonly KeyController Output0Key = KeyController.Get("Output0");

    public DocumentRemoveOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public DocumentRemoveOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("DocumentRemoveOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new DocumentRemoveOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
        new KeyValuePair<KeyController, IOInfo>(CollectionKey, new IOInfo(DashShared.TypeInfo.Document, true)),
        new KeyValuePair<KeyController, IOInfo>(ItemKey, new IOInfo(DashShared.TypeInfo.Any, true)),
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
        [Output0Key] = DashShared.TypeInfo.Bool,
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        var collection = (DocumentController)inputs[CollectionKey];
        var item = (FieldControllerBase)inputs[ItemKey];
        var output0 = Dash.ListFunctions.DocumentRemove(collection, item);
        outputs[Output0Key] = output0;
        return Task.CompletedTask;
    }

}

}
