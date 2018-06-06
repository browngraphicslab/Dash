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
    [OperatorType("after")]
    public class GetAllDocsAfterTime : OperatorController
    {
        //Input keys
        public static readonly KeyController TimeKey = new KeyController("47684CA7-687B-489E-8DAA-82051620363C", "Time");

        //Output keys
        public static readonly KeyController ResultsKey = new KeyController("C856C754-B60F-44AF-B262-960F03156691", "Results");

        public GetAllDocsAfterTime() : base(new OperatorModel(TypeKey.KeyModel))
        {
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
        private static readonly KeyController TypeKey = new KeyController("C902F10A-454E-40C0-A2C8-9B2FC9711A9B", "After");

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
                    for(int i = 0; i < data.Count; i++)
                    {
                        //get time paratmeter in doc and make it into DateTime
                        var docTimeS = data[i].DereferenceToRoot<DocumentController>(null)
                            .GetField(KeyStore.SearchResultDocumentOutline.SearchResultHelpTextKey).ToString();
                        DateTime docTime = DateTime.Parse(docTimeS);

                        //return all docs after givenTime
                        if (docTime > givenTime)
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
            return new GetAllDocsAfterTime();
        }
    }
}
