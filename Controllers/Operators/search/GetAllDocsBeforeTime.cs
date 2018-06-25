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
    /// operator to get all documents before a given time
    /// </summary>
    [OperatorType("before")]
    public class GetAllDocsBeforeTime : OperatorController
    {
        //Input keys
        public static readonly KeyController TimeKey = new KeyController("A7D1E920-8149-4844-B119-D72A831D81AF", "Time");

        //Output keys
        public static readonly KeyController ResultsKey = new KeyController("9A1C4EFD-8A9F-4117-B9AB-F8C77AD96F41", "Results");

        public GetAllDocsBeforeTime() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
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
        private static readonly KeyController TypeKey = new KeyController("27B6978D-F053-480B-8B64-439D334E5C9E", "Before");

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, ScriptState state = null)
        {
            var toReturn = new ListController<DocumentController>();

            var time = (inputs[TimeKey] as TextController)?.Data?.ToLower();

            if (!DateTime.TryParse(time, out DateTime givenTime))
            {
                Debug.WriteLine("Invalid time input");
            }
            else
            {

                if (!string.IsNullOrEmpty(time))
                {
                    var allResults =
                        DSL.Interpret(OperatorScript.GetDishOperatorName<SearchOperatorController>() + "(\" \")") as
                            ListController<DocumentController>;

                    Debug.Assert(allResults != null);

                    var data = allResults.TypedData;
                    for (int i = 0; i < data.Count; i++)
                    {
                        //get time paratmeter in doc and make it into DateTime
                        var docTimeS = data[i]
                            .GetField<TextController>(KeyStore.SearchResultDocumentOutline.SearchResultHelpTextKey) .Data;
                        if(!DateTime.TryParse(docTimeS, out DateTime docTime))
                        {
                            continue;
                        }

                        //return all docs before givenTime
                        if (docTime < givenTime)
                        {
                            toReturn.Add(data[i]);
                        }

                    }

                }
            }

            outputs[ResultsKey] = toReturn;
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new GetAllDocsBeforeTime();
        }
    }
}
