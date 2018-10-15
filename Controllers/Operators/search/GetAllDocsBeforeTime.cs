using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    /// <summary>
    /// operator to get all documents before a given time
    /// </summary>
    [OperatorType(Op.Name.before)]
    public class GetAllDocsBeforeTime : OperatorController
    {
        //Input keys
        public static readonly KeyController TimeKey = KeyController.Get("Time");

        //Output keys
        public static readonly KeyController ResultsKey = KeyController.Get("Results");

        public GetAllDocsBeforeTime() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }
        public GetAllDocsBeforeTime(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(TimeKey, new IOInfo(TypeInfo.Text, true)),
        };

        public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } =
            new ObservableDictionary<KeyController, DashShared.TypeInfo>()
            {
                [ResultsKey] = TypeInfo.List
            };

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Before", new Guid("27B6978D-F053-480B-8B64-439D334E5C9E"));

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var toReturn = new ListController<DocumentController>();

            var time = (inputs[TimeKey] as TextController)?.Data?.ToLower();

            if (!DateTime.TryParse(time, out DateTime givenTime))
            {
                return Task.CompletedTask;
            }

            if (!string.IsNullOrEmpty(time))
            {
                var allResults = DocumentTree.MainPageTree.GetAllNodes();
                var data = allResults.Select(node => node.ViewDocument).ToList();
                foreach (var t in data)
                {
                    var docTimeS = t.GetDataDocument().GetField<Controllers.DateTimeController>(KeyStore.DateModifiedKey)?.Data;

                    //return all docs before givenTime
                    if (docTimeS < givenTime)
                    {
                        toReturn.Add(t);
                    }
                }

            }

            outputs[ResultsKey] = toReturn;
            return Task.CompletedTask;
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new GetAllDocsBeforeTime();
        }
    }
}
