using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Dash
{
[OperatorType(Op.Name.max)]
public sealed class MaxOperator : OperatorController
{
    //Input Keys
    public static readonly KeyController AKey = KeyController.Get("A");
    public static readonly KeyController BKey = KeyController.Get("B");

    //Output Keys
    public static readonly KeyController Output0Key = KeyController.Get("Output0");

    public MaxOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public MaxOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("MaxOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new MaxOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
        new KeyValuePair<KeyController, IOInfo>(AKey, new IOInfo(DashShared.TypeInfo.Number, true)),
        new KeyValuePair<KeyController, IOInfo>(BKey, new IOInfo(DashShared.TypeInfo.Number, true)),
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
        [Output0Key] = DashShared.TypeInfo.Number,
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        var a = (NumberController)inputs[AKey];
        var b = (NumberController)inputs[BKey];
        var output0 = Dash.MathFunctions.Max(a, b);
        outputs[Output0Key] = output0;
        return Task.CompletedTask;
    }

}

[OperatorType(Op.Name.min)]
public sealed class MinOperator : OperatorController
{
    //Input Keys
    public static readonly KeyController AKey = KeyController.Get("A");
    public static readonly KeyController BKey = KeyController.Get("B");

    //Output Keys
    public static readonly KeyController Output0Key = KeyController.Get("Output0");

    public MinOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public MinOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("MinOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new MinOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
        new KeyValuePair<KeyController, IOInfo>(AKey, new IOInfo(DashShared.TypeInfo.Number, true)),
        new KeyValuePair<KeyController, IOInfo>(BKey, new IOInfo(DashShared.TypeInfo.Number, true)),
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
        [Output0Key] = DashShared.TypeInfo.Number,
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        var a = (NumberController)inputs[AKey];
        var b = (NumberController)inputs[BKey];
        var output0 = Dash.MathFunctions.Min(a, b);
        outputs[Output0Key] = output0;
        return Task.CompletedTask;
    }

}

[OperatorType(Op.Name.sin)]
public sealed class SinOperator : OperatorController
{
    //Input Keys
    public static readonly KeyController ThetaKey = KeyController.Get("Theta");

    //Output Keys
    public static readonly KeyController Output0Key = KeyController.Get("Output0");

    public SinOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public SinOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("SinOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new SinOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
        new KeyValuePair<KeyController, IOInfo>(ThetaKey, new IOInfo(DashShared.TypeInfo.Number, true)),
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
        [Output0Key] = DashShared.TypeInfo.Number,
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        var theta = (NumberController)inputs[ThetaKey];
        var output0 = Dash.MathFunctions.Sin(theta);
        outputs[Output0Key] = output0;
        return Task.CompletedTask;
    }

}

[OperatorType(Op.Name.cos)]
public sealed class CosOperator : OperatorController
{
    //Input Keys
    public static readonly KeyController ThetaKey = KeyController.Get("Theta");

    //Output Keys
    public static readonly KeyController Output0Key = KeyController.Get("Output0");

    public CosOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public CosOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("CosOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new CosOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
        new KeyValuePair<KeyController, IOInfo>(ThetaKey, new IOInfo(DashShared.TypeInfo.Number, true)),
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
        [Output0Key] = DashShared.TypeInfo.Number,
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        var theta = (NumberController)inputs[ThetaKey];
        var output0 = Dash.MathFunctions.Cos(theta);
        outputs[Output0Key] = output0;
        return Task.CompletedTask;
    }

}

[OperatorType(Op.Name.tan)]
public sealed class TanOperator : OperatorController
{
    //Input Keys
    public static readonly KeyController ThetaKey = KeyController.Get("Theta");

    //Output Keys
    public static readonly KeyController Output0Key = KeyController.Get("Output0");

    public TanOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public TanOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("TanOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new TanOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
        new KeyValuePair<KeyController, IOInfo>(ThetaKey, new IOInfo(DashShared.TypeInfo.Number, true)),
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
        [Output0Key] = DashShared.TypeInfo.Number,
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        var theta = (NumberController)inputs[ThetaKey];
        var output0 = Dash.MathFunctions.Tan(theta);
        outputs[Output0Key] = output0;
        return Task.CompletedTask;
    }

}

[OperatorType(Op.Name.rand)]
public sealed class RandOperator : OperatorController
{
    //Output Keys
    public static readonly KeyController Output0Key = KeyController.Get("Output0");

    public RandOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public RandOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("RandOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new RandOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
        [Output0Key] = DashShared.TypeInfo.Number,
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        var output0 = Dash.MathFunctions.Rand();
        outputs[Output0Key] = output0;
        return Task.CompletedTask;
    }

}

[OperatorType(Op.Name.rand_i)]
public sealed class RandIOperator : OperatorController
{
    //Output Keys
    public static readonly KeyController Output0Key = KeyController.Get("Output0");

    public RandIOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public RandIOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("RandIOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new RandIOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
        [Output0Key] = DashShared.TypeInfo.Number,
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        var output0 = Dash.MathFunctions.RandI();
        outputs[Output0Key] = output0;
        return Task.CompletedTask;
    }

}

[OperatorType(Op.Name.rand_i)]
public sealed class RandIMaxOperator : OperatorController
{
    //Input Keys
    public static readonly KeyController MaxKey = KeyController.Get("Max");

    //Output Keys
    public static readonly KeyController Output0Key = KeyController.Get("Output0");

    public RandIMaxOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public RandIMaxOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("RandIMaxOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new RandIMaxOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
        new KeyValuePair<KeyController, IOInfo>(MaxKey, new IOInfo(DashShared.TypeInfo.Number, true)),
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
        [Output0Key] = DashShared.TypeInfo.Number,
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        var max = (NumberController)inputs[MaxKey];
        var output0 = Dash.MathFunctions.RandIMax(max);
        outputs[Output0Key] = output0;
        return Task.CompletedTask;
    }

}

[OperatorType(Op.Name.rand_i)]
public sealed class RandIRangeOperator : OperatorController
{
    //Input Keys
    public static readonly KeyController MinKey = KeyController.Get("Min");
    public static readonly KeyController MaxKey = KeyController.Get("Max");

    //Output Keys
    public static readonly KeyController Output0Key = KeyController.Get("Output0");

    public RandIRangeOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public RandIRangeOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("RandIRangeOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new RandIRangeOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
        new KeyValuePair<KeyController, IOInfo>(MinKey, new IOInfo(DashShared.TypeInfo.Number, true)),
        new KeyValuePair<KeyController, IOInfo>(MaxKey, new IOInfo(DashShared.TypeInfo.Number, true)),
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
        [Output0Key] = DashShared.TypeInfo.Number,
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        var min = (NumberController)inputs[MinKey];
        var max = (NumberController)inputs[MaxKey];
        var output0 = Dash.MathFunctions.RandIRange(min, max);
        outputs[Output0Key] = output0;
        return Task.CompletedTask;
    }

}

}
