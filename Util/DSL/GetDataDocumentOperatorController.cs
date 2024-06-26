﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name.data_doc, Op.Name.data_document)]
    public sealed class GetDataDocumentOperatorController : OperatorController
    {
        //Input keys
        public static readonly KeyController InputDocumentKey = KeyController.Get("InputDocument");

        //Output keys
        public static readonly KeyController ResultDataDocumentKey = KeyController.Get("ResultDataDocument");

        public GetDataDocumentOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public GetDataDocumentOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) {}
        
        public override FieldControllerBase GetDefaultController() => new GetDataDocumentOperatorController();

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(InputDocumentKey, new IOInfo(TypeInfo.Document, true)),
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>()
        {
            [ResultDataDocumentKey] = TypeInfo.Document
        };

        public override KeyController OperatorType { get; } = TypeKey;

        private static readonly KeyController TypeKey = KeyController.Get("Get Data Document");

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var inputDocument = inputs[InputDocumentKey] as DocumentController;
            if (inputDocument != null)
            {
                outputs[ResultDataDocumentKey] = inputDocument.GetDataDocument();
            }

            else
            {
                outputs[ResultDataDocumentKey] = new DocumentController();
            }
            return Task.CompletedTask;
        }
    }
}

