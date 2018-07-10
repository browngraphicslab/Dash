using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType("findSingle", "fs", "findS")]
    public class FindSingleDocumentOperatorController : OperatorController
    {
        //Input keys
        public static readonly KeyController QueryKey = new KeyController("Query");

        //Output keys
        public static readonly KeyController ResultsKey = new KeyController("Results");

        public FindSingleDocumentOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }

        public FindSingleDocumentOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
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
            [ResultsKey] = TypeInfo.Document
        };

        public override KeyController OperatorType { get; } = TypeKey;

        private static readonly KeyController TypeKey =
            new KeyController("Simple Single Search", "C35B553E-F12A-483A-AED9-30927606B897");

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, ScriptState state = null)
        {
            //TODO not have the function calls hardcoded here as strings.  We should find a dynamic way to reference Dish script function string names
            var searchQuery = (inputs[QueryKey] as TextController)?.Data ?? "";

            var exec = OperatorScript.GetDishOperatorName<ExecDishOperatorController>();

            var stringScriptToExecute = $"{exec}(parseSearchString(\"{searchQuery}\"))";

            var interpreted = TypescriptToOperatorParser.Interpret(stringScriptToExecute);
            var resultDict = interpreted as DocumentController;

            if (resultDict != null)
            {
                var docs = MainSearchBox.GetDocumentControllersFromSearchDictionary(resultDict, searchQuery);

                outputs[ResultsKey] = docs.Select(i => ContentController<FieldModel>.GetController<DocumentController>(MainSearchBox.SearchHelper.DocumentSearchResultToViewModel(i).Id)).FirstOrDefault();
            }
            else
            {
                outputs[ResultsKey] = new TextController();
            }
        }
    }
}

