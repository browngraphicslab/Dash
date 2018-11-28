using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Dash
{
[OperatorType(Op.Name.content_template)]
public sealed class ContentTemplateOperator : OperatorController
{
    //Input Keys
    public static readonly KeyController KeyNameKey = KeyController.Get("KeyName");

    //Output Keys
    public static readonly KeyController Output0Key = KeyController.Get("Output0");

    public ContentTemplateOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public ContentTemplateOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("ContentTemplateOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new ContentTemplateOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
        new KeyValuePair<KeyController, IOInfo>(KeyNameKey, new IOInfo(DashShared.TypeInfo.Text, true)),
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
        [Output0Key] = DashShared.TypeInfo.Text,
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        var keyName = (TextController)inputs[KeyNameKey];
        var output0 = Dash.TemplateFunctions.ContentTemplate(keyName);
        outputs[Output0Key] = output0;
        return Task.CompletedTask;
    }

}

[OperatorType(Op.Name.text_template)]
public sealed class TextTemplateOperator : OperatorController
{
    //Input Keys
    public static readonly KeyController KeyNameKey = KeyController.Get("KeyName");

    //Output Keys
    public static readonly KeyController Output0Key = KeyController.Get("Output0");

    public TextTemplateOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public TextTemplateOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("TextTemplateOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new TextTemplateOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
        new KeyValuePair<KeyController, IOInfo>(KeyNameKey, new IOInfo(DashShared.TypeInfo.Text, true)),
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
        [Output0Key] = DashShared.TypeInfo.Text,
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        var keyName = (TextController)inputs[KeyNameKey];
        var output0 = Dash.TemplateFunctions.TextTemplate(keyName);
        outputs[Output0Key] = output0;
        return Task.CompletedTask;
    }

}

[OperatorType(Op.Name.image_template)]
public sealed class ImageTemplateOperator : OperatorController
{
    //Input Keys
    public static readonly KeyController KeyNameKey = KeyController.Get("KeyName");

    //Output Keys
    public static readonly KeyController Output0Key = KeyController.Get("Output0");

    public ImageTemplateOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

    public ImageTemplateOperator(OperatorModel operatorModel) : base(operatorModel) { }

    public override KeyController OperatorType { get; } = TypeKey;
    private static readonly KeyController TypeKey = KeyController.Get("ImageTemplateOperator");

    public override FieldControllerBase GetDefaultController()
    {
        return new ImageTemplateOperator();
    }

    public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
    {
        new KeyValuePair<KeyController, IOInfo>(KeyNameKey, new IOInfo(DashShared.TypeInfo.Text, true)),
    };


    public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>
    {
        [Output0Key] = DashShared.TypeInfo.Text,
    };

    public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null) {
        var keyName = (TextController)inputs[KeyNameKey];
        var output0 = Dash.TemplateFunctions.ImageTemplate(keyName);
        outputs[Output0Key] = output0;
        return Task.CompletedTask;
    }

}

}
