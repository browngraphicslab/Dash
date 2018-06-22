using System.Collections.Generic;
using System.Collections.ObjectModel;
using DashShared;

namespace Dash
{

    public class UnionOperatorController : OperatorController
    {
        //Input keys
        public static readonly KeyController AKey = new KeyController("FBEBB4BE-5077-4ADC-8DAD-61142C301F61", "Input A");
        public static readonly KeyController BKey = new KeyController("740CE0AA-C7FD-4E78-9FC7-C7ED5E828260", "Input B");

        //Output keys
        public static readonly KeyController UnionKey = new KeyController("914B682E-E30C-46C5-80E2-7EC6B0B5C0F6", "Union");

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
            SaveOnServer();

        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("CAEA115E-BE19-4796-B225-51AC4122C168", "Union");

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args, ScriptState state = null)
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

        }

        public override FieldControllerBase GetDefaultController()
        {
            return new UnionOperatorController();
        }
    }

}
