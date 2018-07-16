using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    /// <summary>
    /// operator to get all documents after a given time
    /// </summary>
    [OperatorType(Op.Name.after)]
    public class GetAllDocsAfterTime : OperatorController
    {
        //Input keys
        public static readonly KeyController TimeKey = new KeyController("Time");

        //Output keys
        public static readonly KeyController ResultsKey = new KeyController("Results");

        public GetAllDocsAfterTime() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }
        public GetAllDocsAfterTime(OperatorModel operatorFieldModel) : base(operatorFieldModel)
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

        private static readonly KeyController TypeKey = new KeyController("After", "C902F10A-454E-40C0-A2C8-9B2FC9711A9B");

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var toReturn = new ListController<DocumentController>();

            var time = (inputs[TimeKey] as TextController)?.Data?.ToLower();


            if (!DateTime.TryParse(time, out DateTime givenTime))
            {
                return;
            }

            if (!string.IsNullOrEmpty(time))
            {
                var allResults = DocumentTree.MainPageTree;
                var data = allResults.Select(node => node.ViewDocument).ToList();
                foreach (var t in data)
                {
                    var docTimeS = t.GetDataDocument().GetField<Controllers.DateTimeController>(KeyStore.ModifiedTimestampKey)?.Data;

                    //return all docs after givenTime
                    if (docTimeS > givenTime)
                    {
                        toReturn.Add(t);
                    }
                }

            }

            outputs[ResultsKey] = toReturn;
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new GetAllDocsAfterTime();
        }
    }
}
