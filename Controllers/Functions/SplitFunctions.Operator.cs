using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Dash
{
[OperatorType(Op.Name.split_horizontal)]
public sealed class SplitHorizontalOperator : OperatorController
{

    public SplitHorizontalOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public SplitHorizontalOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("SplitHorizontalOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new SplitHorizontalOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        Dash.Controllers.Functions.Operators.SplitFunctions.SplitHorizontal();
        return Task.CompletedTask;
    }

}

[OperatorType(Op.Name.split_vertical)]
public sealed class SplitVerticalOperator : OperatorController
{

    public SplitVerticalOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public SplitVerticalOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("SplitVerticalOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new SplitVerticalOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        Dash.Controllers.Functions.Operators.SplitFunctions.SplitVertical();
        return Task.CompletedTask;
    }

}

[OperatorType(Op.Name.close_split)]
public sealed class CloseSplitOperator : OperatorController
{

    public CloseSplitOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public CloseSplitOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("CloseSplitOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new CloseSplitOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        Dash.Controllers.Functions.Operators.SplitFunctions.CloseSplit();
        return Task.CompletedTask;
    }

}

[OperatorType(Op.Name.frame_history_back)]
public sealed class FrameHistoryBackOperator : OperatorController
{

    public FrameHistoryBackOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public FrameHistoryBackOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("FrameHistoryBackOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new FrameHistoryBackOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        Dash.Controllers.Functions.Operators.SplitFunctions.FrameHistoryBack();
        return Task.CompletedTask;
    }

}

[OperatorType(Op.Name.frame_history_forward)]
public sealed class FrameHistoryForwardOperator : OperatorController
{

    public FrameHistoryForwardOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public FrameHistoryForwardOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("FrameHistoryForwardOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new FrameHistoryForwardOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        Dash.Controllers.Functions.Operators.SplitFunctions.FrameHistoryForward();
        return Task.CompletedTask;
    }

}

}
