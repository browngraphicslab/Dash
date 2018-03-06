using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType("find")]
    public class SimplifiedSearchOperatorController : OperatorController
    {

        //Input keys
        public static readonly KeyController QueryKey = new KeyController("2515150F-3AF7-4840-AE45-B6951EF628C6", "Query");

        //Output keys
        public static readonly KeyController ResultsKey = new KeyController("C0EBD4D8-C922-4CAC-81FE-0FB8D8A8AE36", "Results");

        public SimplifiedSearchOperatorController() : base(new OperatorModel(OperatorType.SimplifiedSearch))
        {
        }

        public SimplifiedSearchOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override bool SetValue(object value)
        {
            throw new NotImplementedException();
        }

        public override object GetValue(Context context)
        {
            throw new NotImplementedException();
        }

        public override FieldModelController<OperatorModel> Copy()
        {
            throw new NotImplementedException();
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(QueryKey, new IOInfo(TypeInfo.Text, true)),
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>()
        {
            [ResultsKey] = TypeInfo.List
        };
    
        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args)
        {
            //TODO not have the function calls hardcoded here as strings.  We should find a dynamic way to reference Dish script function string names
            var searchQuery = (inputs[QueryKey] as TextController)?.Data ?? "";

            var stringScriptToExecute = $"exec(Script:parseSearchString(Query:{{{searchQuery}}}))";

            var searchOutputList = OperatorScriptParser.Interpret(stringScriptToExecute) as BaseListController;
            if (searchOutputList != null)
            {
                outputs[ResultsKey] = searchOutputList;
            }
            else
            {
                outputs[ResultsKey] = new ListController<TextController>();
            }
        }
    }
}
