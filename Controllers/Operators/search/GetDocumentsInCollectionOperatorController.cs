﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.coll, Op.Name.inside)]
    public class GetDocumentsInCollectionOperatorController : OperatorController
    {
        //Input keys
        public static readonly KeyController TextKey = KeyController.Get("Term");

        //Output keys
        public static readonly KeyController ResultsKey = KeyController.Get("Results");


        public GetDocumentsInCollectionOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public GetDocumentsInCollectionOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Get Documents In Collection");


        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(TextKey, new IOInfo(TypeInfo.Text, true)),
        };


        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ResultsKey] = TypeInfo.List,
        };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var searchTerm = inputs[TextKey] as TextController;
            if (searchTerm == null || searchTerm.Data == null) return Task.CompletedTask;

            var term = searchTerm.Data;
            var tree = DocumentTree.MainPageTree;

            var reg = new System.Text.RegularExpressions.Regex("^" + term + "$");
            var final = tree.Where(doc => reg.IsMatch(doc.DataDocument.Title)).ToList();
            var docs = final.SelectMany(node => node.Children, (colNode, documentNode) => documentNode.ViewDocument);
            outputs[ResultsKey] = new ListController<DocumentController>(docs.Distinct());
            return Task.CompletedTask;
        }

        public override FieldControllerBase GetDefaultController() => new GetDocumentsInCollectionOperatorController();
    }
}
