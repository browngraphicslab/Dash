﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Audio;
using Windows.Storage.Streams;
using DashShared;

namespace Dash
{
    class CollectionMapOperator : OperatorFieldModelController
    {
        public static readonly DocumentType MapType = new DocumentType("60B60218-966F-47F1-8291-B2FD5EEE444F", "Map");

        public static readonly Key InputOperatorKey = new Key("520F5DC4-005E-4F0D-91A3-099358990E40", "Input Operator");

        public static readonly Key OutputCollectionKey = new Key("5AB32970-0950-45BE-87CB-1FD82B38892E", "Output Collection");

        public CollectionMapOperator() : base(new OperatorFieldModel("CollectionMap"))
        {
        }

        public override FieldModelController Copy()
        {
            return new CollectionMapOperator();
        }

        public override ObservableDictionary<Key, TypeInfo> Inputs { get; } = new ObservableDictionary<Key, TypeInfo>()
        {
            [InputOperatorKey] = TypeInfo.Operator
        };

        public override ObservableDictionary<Key, TypeInfo> Outputs { get; } = new ObservableDictionary<Key, TypeInfo>()
        {
            [OutputCollectionKey] = TypeInfo.Collection
        };

        public void ResetInputKeys()
        {
            Inputs.Clear();
            Inputs[InputOperatorKey] = TypeInfo.Operator;
        }

        public void UpdateInputs(OperatorFieldModelController controller)
        {
            var toRemove = Inputs.Where(pair => !pair.Key.Equals(InputOperatorKey)).Select(pair => pair.Key).ToList();
            foreach (var key in toRemove)
            {
                Inputs.Remove(key);
            }

            //Inputs.Clear();
            //Inputs[InputOperatorKey] = TypeInfo.Operator;
            foreach (var controllerInput in controller.Inputs)
            {
                Inputs[controllerInput.Key] = TypeInfo.Collection;
            }
        }

        public Dictionary<Key, Key> InputKeyMap { get; set; } = new Dictionary<Key, Key>();

        public override void Execute(Dictionary<Key, FieldModelController> inputs, Dictionary<Key, FieldModelController> outputs)
        {
            OperatorFieldModelController operatorController = inputs[InputOperatorKey] as OperatorFieldModelController;
            Dictionary<Key, FieldModelController> operatorInputs = new Dictionary<Key, FieldModelController>();
            Dictionary<Key, FieldModelController> operatorOutputs = new Dictionary<Key, FieldModelController>();

            List<Key> keys = new List<Key>();
            List<List<DocumentController>> collections = new List<List<DocumentController>>();
            int numDocuments = -1;
            foreach (var key in inputs.Keys)
            {
                if (key.Equals(InputOperatorKey))
                {
                    continue;
                }
                var documentControllers = (inputs[key] as DocumentCollectionFieldModelController)?.GetDocuments();
                if (numDocuments == -1)
                {
                    numDocuments = documentControllers.Count;
                }
                else if(numDocuments != documentControllers.Count)
                {
                    return;//Collections with different lengths
                }
                keys.Add(key);
                collections.Add(documentControllers);
            }

            List<DocumentController> documents = new List<DocumentController>();

            for (int i = 0; i < numDocuments; i++)
            {
                operatorInputs.Clear();
                for(int j = 0; j < collections.Count; ++i)
                {
                    operatorInputs[keys[j]] = collections[j][i].GetField(InputKeyMap[keys[j]]);
                }
                operatorOutputs.Clear();
                operatorController.Execute(operatorInputs, operatorOutputs);
                DocumentController doc = new DocumentController(operatorOutputs, DocumentType.DefaultType);
                documents.Add(doc);
            }

            outputs[OutputCollectionKey] = new DocumentCollectionFieldModelController(documents);
        }
    }
}
