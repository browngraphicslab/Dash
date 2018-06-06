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
        public static readonly KeyController TimeKey = new KeyController("9C2EBB16-FC45-4946-84C0-FF943F529093", "Time");

        //Output keys
        public static readonly KeyController ResultsKey = new KeyController("1FCC9F1B-DCA2-4DB5-8A04-AD6F5BEADF0B", "Results");

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
        private static readonly KeyController TypeKey = new KeyController("C92AC345-0987-4A3C-8B95-7387440481D6", "Before");

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args, ScriptState state = null)
        {
            var toReturn = new ListController<DocumentController>();

            var time = (inputs[TimeKey] as TextController)?.Data?.ToLower();

            //remove any extra quotes
            time = time.Trim('"');

            try
            {
                DateTime givenTime = DateTime.Parse(time);

                if (!string.IsNullOrEmpty(time))
                {
                    var allResults = DSL.Interpret(OperatorScript.GetDishOperatorName<SearchOperatorController>() + "(\" \")") as ListController<DocumentController>;

                    Debug.Assert(allResults != null);

                    var data = allResults.Data;
                    for (int i = 0; i < data.Count; i++)
                    {
                        //get time paratmeter in doc and make it into DateTime
                        var docTimeS = data[i].DereferenceToRoot<DocumentController>(null)
                            .GetField(KeyStore.SearchResultDocumentOutline.SearchResultHelpTextKey).ToString();
                        DateTime docTime = DateTime.Parse(docTimeS);

                        //return all docs before givenTime
                        if (docTime < givenTime)
                        {
                            toReturn.Add(data[i]);
                        }

                    }

                }
            }
            catch (Exception e)
            {
                //invalid time input
                Debug.WriteLine("Invalid time input");
            }

            outputs[ResultsKey] = toReturn;
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new GetAllDocsBeforeTime();
        }
    }
}
