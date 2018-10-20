using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class IntersectionOperatorController : OperatorController

    {
        //Input keys
        public static readonly KeyController AKey = KeyController.Get("Input A");
        public static readonly KeyController BKey = KeyController.Get("Input B");

        //Output keys
        public static readonly KeyController IntersectionKey = KeyController.Get("Intersection");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(AKey, new IOInfo(TypeInfo.List, true)),
            new KeyValuePair<KeyController, IOInfo>(BKey, new IOInfo(TypeInfo.List, true)),
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [IntersectionKey] = TypeInfo.List
        };

        public IntersectionOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public IntersectionOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Intersection operator");

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            ListController<DocumentController> setA = (ListController<DocumentController>) inputs[AKey];
            ListController<DocumentController> setB = (ListController<DocumentController>) inputs[BKey];

            // Intersect by comparing all fields 
            HashSet<DocumentController> result = Util.GetIntersection(setA, setB); 
            //(doc.GetDereferencedField(IntersectionKey, docContextList) as ListController<DocumentController>).SetDocuments(result.ToList());
            outputs[IntersectionKey] = new ListController<DocumentController>(result);
            //Debug.WriteLine("intersection count :" + result.Count);

            // Intersect by Document ID 
            //(doc.GetField(IntersectionKey) as ListController<DocumentController>).SetDocuments(setA.GetDocuments().Intersect(setB.GetDocuments()).ToList());
            return Task.CompletedTask;
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new IntersectionOperatorController();
        }
    }
}
