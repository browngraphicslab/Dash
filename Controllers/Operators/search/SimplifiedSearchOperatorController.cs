using System.Collections.Generic;
using System.Collections.ObjectModel;
using DashShared;

// ReSharper disable CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.find, Op.Name.f/*, Op.Name.search*/)]
    public sealed class SimplifiedSearchOperatorController : OperatorController
    {

        //Input keys
        public static readonly KeyController QueryKey = new KeyController("Query");

        //Output keys
        public static readonly KeyController ResultsKey = new KeyController("Results");

        public SimplifiedSearchOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

        public SimplifiedSearchOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public override FieldControllerBase GetDefaultController() => new SimplifiedSearchOperatorController();

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(QueryKey, new IOInfo(TypeInfo.Text, true)),
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>()
        {
            [ResultsKey] = TypeInfo.List
        };

        public override KeyController OperatorType { get; } = TypeKey;

        private static readonly KeyController TypeKey = new KeyController("Simple Search", "F0D6FCB0-4635-4ECF-880F-81D2738A1350");

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            //TODO not have the function calls hardcoded here as strings.  We should find a dynamic way to reference Dish script function string names
            var searchQuery = (inputs[QueryKey] as TextController)?.Data ?? "";
            var results = new ListController<FieldControllerBase>();
            var exec = OperatorScript.GetDishOperatorName<ExecDishOperatorController>();

            var stringScriptToExecute = $"{exec}({DSL.GetFuncName<ParseSearchStringToDishOperatorController>()}(\"{searchQuery}\"))";
            outputs[ResultsKey] = TypescriptToOperatorParser.Interpret(stringScriptToExecute); 
        }
    }
}
