using System.Collections.Generic;
using System.Collections.ObjectModel;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name.intersect_by_value)]
    public class IntersectByValueOperatorController : OperatorController
    {
        //Input keys
        public static readonly KeyController SetAKey = new KeyController("C6DF3CD1-729B-4E3F-AB51-8CF1C46E6AE8", "A");
        public static readonly KeyController SetBKey = new KeyController("752AC76D-BB48-458E-BA32-FC00F0955D7C", "B");

        //Output keys
        public static readonly KeyController IntersectionKey = new KeyController("5B7CA95F-9DE7-4884-9827-F1E8A22DB48F", "Intersection");

        public IntersectByValueOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }

        public IntersectByValueOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new IntersectByValueOperatorController();
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(SetAKey, new IOInfo(TypeInfo.List, true)),
            new KeyValuePair<KeyController, IOInfo>(SetBKey, new IOInfo(TypeInfo.List, true)),
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>()
        {
            [IntersectionKey] = TypeInfo.List
        };

        public override KeyController OperatorType { get; } = TypeKey;

        private static readonly KeyController TypeKey =
            new KeyController("9B073C2B-8B11-4939-818F-903A067D412E", "Intersect by value");

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var set1 = ((inputs[SetAKey] as BaseListController) ?? new ListController<FieldControllerBase>()).Data;
            var set2 = ((inputs[SetBKey] as BaseListController) ?? new ListController<FieldControllerBase>()).Data;

            var returnSet = new ListController<FieldControllerBase>();

            //TODO actually optimize this, right now it's just a prood of concept.  
            //optimizing this will be VERY important as we can definitly go from O(n*n) to O(n) 
            foreach(var obj in set1)
            {
                foreach (var obj2 in set2)
                {
                    if (obj.Model.ValueEquals(obj2.Model))
                    {
                        returnSet.Add(obj);
                    }
                }
            }

            outputs[IntersectionKey] = returnSet;
        }
    }
}
