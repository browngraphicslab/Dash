using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{

    public class UnionOperatorController : OperatorController
    {
        //Input keys
        public static readonly KeyController AKey = KeyController.Get("Input A");
        public static readonly KeyController BKey = KeyController.Get("Input B");

        //Output keys
        public static readonly KeyController UnionKey = KeyController.Get("Union");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(AKey, new IOInfo(TypeInfo.List, true)),
            new KeyValuePair<KeyController, IOInfo>(BKey, new IOInfo(TypeInfo.List, true)),
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [UnionKey] = TypeInfo.List
        };

        public UnionOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
            OperatorFieldModel = operatorFieldModel;
        }

        public UnionOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {

        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Union operator");

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            ListController<DocumentController> setA = (ListController<DocumentController>) inputs[AKey];
            ListController<DocumentController> setB = (ListController<DocumentController>)inputs[BKey];

            // Union by comparing all fields 
            List<DocumentController> bigSet = setA.GetElements();
            bigSet.AddRange(setB.GetElements());
            HashSet<DocumentController> result = new HashSet<DocumentController>(bigSet);
            HashSet<DocumentController> same = Util.GetIntersection(setA, setB);
            result.ExceptWith(same);
            //(doc.GetDereferencedField(UnionKey, DocContextList) as ListController<DocumentController>).SetDocuments(result.ToList());
            outputs[UnionKey] = new ListController<DocumentController>(result);
            //Debug.WriteLine("union count :" + result.Count);

            // Union by Document ID 
            //(doc.GetField(UnionKey) as ListController<DocumentController>).SetDocuments(setA.GetDocuments().Union(setB.GetDocuments()).ToList());

            return Task.CompletedTask;
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new UnionOperatorController();
        }
    }

}
