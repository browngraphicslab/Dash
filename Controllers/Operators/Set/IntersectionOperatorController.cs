﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using DashShared;

namespace Dash
{
    public class IntersectionOperatorController : OperatorController

    {
        //Input keys
        public static readonly KeyController AKey = new KeyController("178123E8-4E64-44D9-8F05-509B2F097B7D", "Input A");
        public static readonly KeyController BKey = new KeyController("0B9C67F7-3FB7-400A-B016-F12C048325BA", "Input B");

        //Output keys
        public static readonly KeyController IntersectionKey = new KeyController("95E14D4F-362A-4B4F-B0CD-78A4F5B47A92", "Intersection");

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
        private static readonly KeyController TypeKey = new KeyController("5B93D353-AE02-4E20-9E2D-D38C01BC5F20", "Intersection");

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args, ScriptState state = null)
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
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new IntersectionOperatorController();
        }
    }
}
