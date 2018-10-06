using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class IntersectionOperatorController : OperatorController

    {
        //Input keys
        public static readonly KeyController AKey = new KeyController("Input A");
        public static readonly KeyController BKey = new KeyController("Input B");

        //Output keys
        public static readonly KeyController IntersectionKey = new KeyController("Intersection");

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
            SaveOnServer();
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("Intersection operator", "5B93D353-AE02-4E20-9E2D-D38C01BC5F20");

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
