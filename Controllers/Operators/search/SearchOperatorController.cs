using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name.search)]
    public sealed class SearchOperatorController : OperatorController
    {
        public SearchOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public SearchOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Search", new Guid("EA5FD353-F99A-4F99-B0BC-5D2C88A51019"));

        public override Func<ReferenceController, CourtesyDocument> LayoutFunc { get; } = rfmc => new SearchOperatorBox(rfmc);

        //Input keys
        public static readonly KeyController TextKey = KeyController.Get("Term");
        public static readonly KeyController SortKey = KeyController.Get("Sorted");
        public static readonly KeyController InputCollection = KeyController.Get("InputCollection");

        //Output keys
        public static readonly KeyController ResultsKey = KeyController.Get("Results");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(TextKey, new IOInfo(TypeInfo.Text, true)),
            new KeyValuePair<KeyController, IOInfo>(SortKey, new IOInfo(TypeInfo.Bool, false)),
            new KeyValuePair<KeyController, IOInfo>(InputCollection, new IOInfo(TypeInfo.List, false)),
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ResultsKey] = TypeInfo.List,
        };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            //search all docs for searchText and get results (list of doc controller)
            string searchText = ((TextController)inputs[TextKey]).Data;
            var docs = inputs[InputCollection] as ListController<DocumentController>;
            List<SearchResult> searchRes;
            try
            {
                searchRes = Search.Parse(searchText, docs: docs).ToList();
                if ((inputs[SortKey] as BoolController)?.Data ?? false)
                {
                    searchRes.Sort((result1, result2) => result2.Rank - result1.Rank);
                }
            }
            catch (Exception e)
            {
                searchRes = new List<SearchResult>();
            }
            var searchResultDocs = searchRes.Select(res => res.ViewDocument).ToArray();
            outputs[ResultsKey] = new ListController<DocumentController>(searchResultDocs);
            return Task.CompletedTask;
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new SearchOperatorController();
        }
    }
}
