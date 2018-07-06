using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name.find, Op.Name.f)]
    public class SimplifiedSearchOperatorController : OperatorController
    {

        //Input keys
        public static readonly KeyController QueryKey = new KeyController("Query");

        //Output keys
        public static readonly KeyController ResultsKey = new KeyController("Results");

        public SimplifiedSearchOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();

        }

        public SimplifiedSearchOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }


        public override FieldControllerBase GetDefaultController()
        {
            return new SimplifiedSearchOperatorController();
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(QueryKey, new IOInfo(TypeInfo.Text, true)),
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>()
        {
            [ResultsKey] = TypeInfo.List
        };

        public override KeyController OperatorType { get; } = TypeKey;

        private static readonly KeyController TypeKey =
            new KeyController("Simple Search", "F0D6FCB0-4635-4ECF-880F-81D2738A1350");

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            //TODO not have the function calls hardcoded here as strings.  We should find a dynamic way to reference Dish script function string names
            var searchQuery = (inputs[QueryKey] as TextController)?.Data ?? "";

            var exec = OperatorScript.GetDishOperatorName<ExecDishOperatorController>();

            var stringScriptToExecute = $"{exec}({DSL.GetFuncName<ParseSearchStringToDishOperatorController>()}(\"{searchQuery}\"))";

            var interpreted = TypescriptToOperatorParser.Interpret(stringScriptToExecute);
            var resultDict = interpreted as DocumentController;

            if (resultDict != null)
            {
                var docs = MainSearchBox.GetDocumentControllersFromSearchDictionary(resultDict, searchQuery);

                outputs[ResultsKey] = new ListController<FieldControllerBase>(docs.Select(i => ContentController<FieldModel>.GetController<DocumentController>(MainSearchBox.SearchHelper.DocumentSearchResultToViewModel(i).Id)));
            }
            else
            {
                outputs[ResultsKey] = new ListController<TextController>();
            }
        }
    }
}
