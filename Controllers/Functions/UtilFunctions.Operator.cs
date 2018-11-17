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

[OperatorType(Op.Name.now)]
public sealed class NowOperator : OperatorController
{
    //Output Keys
    public static readonly KeyController CurrentTimeKey = KeyController.Get("CurrentTime");

    public NowOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public NowOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("NowOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new NowOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
        [CurrentTimeKey] = DashShared.TypeInfo.DateTime,
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        var currentTime = Dash.UtilFunctions.Now();
        outputs[CurrentTimeKey] = currentTime;
        return Task.CompletedTask;
    }

}

[OperatorType(Op.Name.to_string)]
public sealed class ToStringOperator : OperatorController
{
    //Input Keys
    public static readonly KeyController InputKey = KeyController.Get("Input");

    //Output Keys
    public static readonly KeyController ResultKey = KeyController.Get("Result");

    public ToStringOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public ToStringOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("ToStringOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new ToStringOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
        new KeyValuePair<KeyController, IOInfo>(InputKey, new IOInfo(DashShared.TypeInfo.Any, false)),
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
        [ResultKey] = DashShared.TypeInfo.Text,
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        var input = inputs[InputKey] as FieldControllerBase;
        var result = Dash.UtilFunctions.ToString(input);
        outputs[ResultKey] = result;
        return Task.CompletedTask;
    }

}

[OperatorType(Op.Name.undo)]
public sealed class UndoOperator : OperatorController
{

    public UndoOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public UndoOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("UndoOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new UndoOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        Dash.UtilFunctions.Undo();
        return Task.CompletedTask;
    }

}

[OperatorType(Op.Name.redo)]
public sealed class RedoOperator : OperatorController
{

    public RedoOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public RedoOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("RedoOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new RedoOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        Dash.UtilFunctions.Redo();
        return Task.CompletedTask;
    }

}

}
