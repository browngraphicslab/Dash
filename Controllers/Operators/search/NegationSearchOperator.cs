using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType("negationSearch")]
    public class NegationSearchOperator : OperatorController
    {
        //Input keys
        public static readonly KeyController DictKey = new KeyController("7A24D7D3-9E1B-412A-89E8-C6DE395B5C52", "Dict");

        //Output keys
        public static readonly KeyController ResultsKey = new KeyController("5A5C3D40-CEEE-4787-80FB-3922D2A6C663", "DictionaryResults");

        public NegationSearchOperator() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }
        public NegationSearchOperator(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("60B8F4DA-A150-4600-A3EE-D92503413811", "Negation Search");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(DictKey, new IOInfo(TypeInfo.Document, true))
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>()
        {
            [ResultsKey] = TypeInfo.Document
        };

        /// <summary>
        /// Gets a dictionary of all existing DocumentControllers, and creates a new dictionary that is the complement of the input dict 
        /// </summary>
        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args, ScriptState state = null)
        {
            var allResults = DSL.Interpret(OperatorScript.GetDishOperatorName<SearchOperatorController>() + "(\" \")") as ListController<DocumentController>;
            var dictOp = new PutSearchResultsIntoDictionaryOperatorController();
            var dictInputs = new Dictionary<KeyController, FieldControllerBase>
            {
                [PutSearchResultsIntoDictionaryOperatorController.ListKey] = allResults
            };
            var dictOutputs = new Dictionary<KeyController, FieldControllerBase>();
            dictOp.Execute(dictInputs, dictOutputs, null);
            var allResultsDict = dictOutputs[PutSearchResultsIntoDictionaryOperatorController.DictionaryResultsKey] as DocumentController;
            var dict = inputs[DictKey] as DocumentController;

            var toReturn = new DocumentController();

            foreach (var kvp in allResultsDict.EnumFields())
            {
                var l1 = kvp.Value as ListController<DocumentController>;
                var l2 = dict.GetField<ListController<DocumentController>>(kvp.Key);
                if (l1 != null && l2 == null)
                {
                    toReturn.SetField(kvp.Key, new ListController<DocumentController>(l1.TypedData), true);
                }
            }

            outputs[ResultsKey] = toReturn;
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new NegationSearchOperator();
        }
    }
}
