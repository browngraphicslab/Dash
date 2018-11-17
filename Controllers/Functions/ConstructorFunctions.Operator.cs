using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Dash
{
[OperatorType(Op.Name.point)]
public sealed class ZeroPointOperator : OperatorController
{
    //Output Keys
    public static readonly KeyController PointKey = KeyController.Get("Point");

    public ZeroPointOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public ZeroPointOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("ZeroPointOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new ZeroPointOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
        [PointKey] = DashShared.TypeInfo.Point,
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        var point = Dash.ConstructorFunctions.ZeroPoint();
        outputs[PointKey] = point;
        return Task.CompletedTask;
    }

}

[OperatorType(Op.Name.point)]
public sealed class PointOperator : OperatorController
{
    //Input Keys
    public static readonly KeyController XKey = KeyController.Get("X");
    public static readonly KeyController YKey = KeyController.Get("Y");

    //Output Keys
    public static readonly KeyController PointKey = KeyController.Get("Point");

    public PointOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public PointOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("PointOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new PointOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
        new KeyValuePair<KeyController, IOInfo>(XKey, new IOInfo(DashShared.TypeInfo.Number, true)),
        new KeyValuePair<KeyController, IOInfo>(YKey, new IOInfo(DashShared.TypeInfo.Number, true)),
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
        [PointKey] = DashShared.TypeInfo.Point,
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        var x = (NumberController)inputs[XKey];
        var y = (NumberController)inputs[YKey];
        var point = Dash.ConstructorFunctions.Point(x, y);
        outputs[PointKey] = point;
        return Task.CompletedTask;
    }

}

[OperatorType(Op.Name.image)]
public sealed class ImageOperator : OperatorController
{
    //Input Keys
    public static readonly KeyController PathKey = KeyController.Get("Path");

    //Output Keys
    public static readonly KeyController ImageKey = KeyController.Get("Image");

    public ImageOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public ImageOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("ImageOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new ImageOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
        new KeyValuePair<KeyController, IOInfo>(PathKey, new IOInfo(DashShared.TypeInfo.Text, true)),
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
        [ImageKey] = DashShared.TypeInfo.Image,
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        var path = (TextController)inputs[PathKey];
        var image = Dash.ConstructorFunctions.Image(path);
        outputs[ImageKey] = image;
        return Task.CompletedTask;
    }

}

[OperatorType(Op.Name.video)]
public sealed class VideoOperator : OperatorController
{
    //Input Keys
    public static readonly KeyController PathKey = KeyController.Get("Path");

    //Output Keys
    public static readonly KeyController VideoKey = KeyController.Get("Video");

    public VideoOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public VideoOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("VideoOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new VideoOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
        new KeyValuePair<KeyController, IOInfo>(PathKey, new IOInfo(DashShared.TypeInfo.Text, true)),
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
        [VideoKey] = DashShared.TypeInfo.Video,
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        var path = (TextController)inputs[PathKey];
        var video = Dash.ConstructorFunctions.Video(path);
        outputs[VideoKey] = video;
        return Task.CompletedTask;
    }

}

[OperatorType(Op.Name.audio)]
public sealed class AudioOperator : OperatorController
{
    //Input Keys
    public static readonly KeyController PathKey = KeyController.Get("Path");

    //Output Keys
    public static readonly KeyController AudioKey = KeyController.Get("Audio");

    public AudioOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public AudioOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("AudioOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new AudioOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
        new KeyValuePair<KeyController, IOInfo>(PathKey, new IOInfo(DashShared.TypeInfo.Text, true)),
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
        [AudioKey] = DashShared.TypeInfo.Audio,
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        var path = (TextController)inputs[PathKey];
        var audio = Dash.ConstructorFunctions.Audio(path);
        outputs[AudioKey] = audio;
        return Task.CompletedTask;
    }

}

[OperatorType(Op.Name.pdf)]
public sealed class PdfOperator : OperatorController
{
    //Input Keys
    public static readonly KeyController PathKey = KeyController.Get("Path");

    //Output Keys
    public static readonly KeyController PdfKey = KeyController.Get("Pdf");

    public PdfOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public PdfOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("PdfOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new PdfOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
        new KeyValuePair<KeyController, IOInfo>(PathKey, new IOInfo(DashShared.TypeInfo.Text, true)),
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
        [PdfKey] = DashShared.TypeInfo.Pdf,
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        var path = (TextController)inputs[PathKey];
        var pdf = Dash.ConstructorFunctions.Pdf(path);
        outputs[PdfKey] = pdf;
        return Task.CompletedTask;
    }

}

[OperatorType(Op.Name.color)]
public sealed class ColorOperator : OperatorController
{
    //Input Keys
    public static readonly KeyController SKey = KeyController.Get("S");

    //Output Keys
    public static readonly KeyController ColorKey = KeyController.Get("Color");

    public ColorOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public ColorOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("ColorOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new ColorOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
        new KeyValuePair<KeyController, IOInfo>(SKey, new IOInfo(DashShared.TypeInfo.Text, true)),
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
        [ColorKey] = DashShared.TypeInfo.Color,
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        var s = (TextController)inputs[SKey];
        var color = Dash.ConstructorFunctions.Color(s);
        outputs[ColorKey] = color;
        return Task.CompletedTask;
    }

}

[OperatorType(Op.Name.date)]
public sealed class DateOperator : OperatorController
{
    //Input Keys
    public static readonly KeyController SKey = KeyController.Get("S");

    //Output Keys
    public static readonly KeyController DateKey = KeyController.Get("Date");

    public DateOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public DateOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("DateOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new DateOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
        new KeyValuePair<KeyController, IOInfo>(SKey, new IOInfo(DashShared.TypeInfo.Text, true)),
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
        [DateKey] = DashShared.TypeInfo.DateTime,
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        var s = (TextController)inputs[SKey];
        var date = Dash.ConstructorFunctions.Date(s);
        outputs[DateKey] = date;
        return Task.CompletedTask;
    }

}

}
