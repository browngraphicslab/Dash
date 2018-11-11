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
        [Output0Key] = DashShared.TypeInfo.Operator,
    };

    public override async Task Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs,
                                 DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
    {
        var output0 = await Dash.UIFunctions.TextInput();
        outputs[Output0Key] = new ListController<TextController>(output0);
    }

}

}
