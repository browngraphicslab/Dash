﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name.pref)]
    public class PointerReferenceOperator : OperatorController
    {
        public static readonly KeyController DocumentReferenceKey = KeyController.Get("DocumentReference");
        public static readonly KeyController KeyKey = KeyController.Get("Key");


        public static readonly KeyController ReferenceKey = KeyController.Get("Reference");


        public PointerReferenceOperator() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }

        public PointerReferenceOperator(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Pointer Operator");

        public override FieldControllerBase GetDefaultController()
        {
            return new PointerReferenceOperator();
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(DocumentReferenceKey, new IOInfo(TypeInfo.Reference, true)),
            new KeyValuePair<KeyController, IOInfo>(KeyKey, new IOInfo(TypeInfo.Key, true)),

        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ReferenceKey] = TypeInfo.PointerReference,

        };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var documentReference = (ReferenceController)inputs[DocumentReferenceKey];
            var key = (KeyController)inputs[KeyKey];
            PointerReferenceController reference = Execute(documentReference, key);
            outputs[ReferenceKey] = reference;

            return Task.CompletedTask;
        }

        public PointerReferenceController Execute(ReferenceController documentReference, KeyController key)
        {
            return new PointerReferenceController(documentReference, key);
        }

    }
}
