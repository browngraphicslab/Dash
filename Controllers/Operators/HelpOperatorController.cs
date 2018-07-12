using System.Collections.Generic;
using System.Collections.ObjectModel;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.help)]
    public class HelpOperatorController : OperatorController
    {
        private readonly Dictionary<Op.Name, ScriptHelpExcerpt> _constructedExcerpts = new Dictionary<Op.Name, ScriptHelpExcerpt>();

        public HelpOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public HelpOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("Get information on functions", "2D95083C-03E1-4FFA-80FA-881C0ECBD3D7");

        //Input keys
        public static readonly KeyController FuncNameKey = new KeyController("Name of function to explore");

        //Output keys
        public static readonly KeyController ComputedResultKey = new KeyController("Computed Result");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(FuncNameKey, new IOInfo(TypeInfo.Text, false)),
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ComputedResultKey] = TypeInfo.Text
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            if (!inputs.ContainsKey(FuncNameKey))
            {
                outputs[ComputedResultKey] = OperatorScript.GetFunctionList();
                return;
            }
            if (!(inputs[FuncNameKey] is TextController enumAsString)) return;
            if (enumAsString.Data == "")
            {
                outputs[ComputedResultKey] = OperatorScript.GetFunctionList();
                return;
            }
            var enumOut = Op.Parse(enumAsString.Data);
            if (enumOut == Op.Name.invalid) throw new ScriptExecutionException(new FunctionCallMissingScriptErrorModel(enumAsString.Data));
            if (!_constructedExcerpts.ContainsKey(enumOut)) _constructedExcerpts.Add(enumOut, new ScriptHelpExcerpt(enumOut));
            outputs[ComputedResultKey] = _constructedExcerpts[enumOut].GetExcerpt();
        }

        public override FieldControllerBase GetDefaultController() => new HelpOperatorController();
    }
}