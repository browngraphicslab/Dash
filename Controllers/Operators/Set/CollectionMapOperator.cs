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

        public static readonly KeyControllerBase InputOperatorKey = new KeyControllerBase("520F5DC4-005E-4F0D-91A3-099358990E40", "Input Operator");

        public static readonly KeyControllerBase OutputCollectionKey = new KeyControllerBase("5AB32970-0950-45BE-87CB-1FD82B38892E", "Output Collection");

        public CollectionMapOperator() : base(new OperatorFieldModel("CollectionMap"))
        {
        }

        public override FieldModelController<OperatorFieldModel> Copy()
        {
            return new CollectionMapOperator();
        }
        public override object GetValue(Context context)
        {
            throw new System.NotImplementedException();
        }
        public override bool SetValue(object value)
        {
            return false;
        }

        public override ObservableDictionary<KeyControllerBase, IOInfo> Inputs { get; } = new ObservableDictionary<KeyControllerBase, IOInfo>()
        {
            [InputOperatorKey] = new IOInfo(TypeInfo.Operator, true)
        };

        public override ObservableDictionary<KeyControllerBase, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyControllerBase, TypeInfo>()
        {
            [OutputCollectionKey] = TypeInfo.Collection
        };

        public void ResetInputKeys()
        {
            Inputs.Clear();
            Inputs[InputOperatorKey] = new IOInfo(TypeInfo.Operator, true);
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
                Inputs[controllerInput.Key] = new IOInfo(TypeInfo.Collection, true);
            }
        }

        public Dictionary<KeyControllerBase, KeyControllerBase> InputKeyMap { get; set; } = new Dictionary<KeyControllerBase, KeyControllerBase>();

        public override void Execute(Dictionary<KeyControllerBase, FieldControllerBase> inputs, Dictionary<KeyControllerBase, FieldControllerBase> outputs)
        {
            OperatorFieldModelController operatorController = inputs[InputOperatorKey] as OperatorFieldModelController;
            Dictionary<KeyControllerBase, FieldControllerBase> operatorInputs = new Dictionary<KeyControllerBase, FieldControllerBase>();
            Dictionary<KeyControllerBase, FieldControllerBase> operatorOutputs = new Dictionary<KeyControllerBase, FieldControllerBase>();

            List<KeyControllerBase> keys = new List<KeyControllerBase>();
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
                if (!InputKeyMap.ContainsKey(key))
                {
                    return;//We don't have a key for one of the inputs
                }
                keys.Add(key);
                collections.Add(documentControllers);
            }

            List<DocumentController> documents = new List<DocumentController>();

            DocumentController prototype = new DocumentController(new Dictionary<KeyControllerBase, FieldControllerBase>(), DocumentType.DefaultType);

            for (int i = 0; i < numDocuments; i++)
            {
                operatorInputs.Clear();
                for(int j = 0; j < collections.Count; ++j)
                {
                    operatorInputs[keys[j]] = collections[j][i].GetField(InputKeyMap[keys[j]]);
                }
                operatorOutputs.Clear();
                operatorController.Execute(operatorInputs, operatorOutputs);
                DocumentController doc = prototype.MakeDelegate();//new DocumentController(operatorOutputs, DocumentType.DefaultType);
                doc.SetFields(operatorOutputs, true);
                doc.SetActiveLayout(new DefaultLayout().Document, true, false);
                documents.Add(doc);
            }

            outputs[OutputCollectionKey] = new DocumentCollectionFieldModelController(documents);
        }
    }
}
