using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    class CompoundOperator : OperatorFieldModelController
    {
        public CompoundOperator(CompoundOperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public CompoundOperator() : base(new CompoundOperatorFieldModel("Compound"))
        {
        }

        public override FieldModelController Copy()
        {
            Debug.Assert(OperatorFieldModel is CompoundOperatorFieldModel);
            return new CompoundOperator(OperatorFieldModel as CompoundOperatorFieldModel);
        }

        public override ObservableDictionary<Key, TypeInfo> Inputs { get; } = new ObservableDictionary<Key, TypeInfo>();

        public override ObservableDictionary<Key, TypeInfo> Outputs { get; } = new ObservableDictionary<Key, TypeInfo>();

        public Dictionary<Key, ReferenceFieldModelController> InputFieldReferences = new Dictionary<Key, ReferenceFieldModelController>();
        public Dictionary<Key, ReferenceFieldModelController> OutputFieldReferences = new Dictionary<Key, ReferenceFieldModelController>();

        public override void Execute(Dictionary<Key, FieldModelController> inputs, Dictionary<Key, FieldModelController> outputs)
        {
            Context c = new Context();
            foreach (var reference in InputFieldReferences)
            {
                var doc = reference.Value.GetDocumentController(c);
                doc.SetField(reference.Value.FieldKey, inputs[reference.Key], true);
            }
            foreach (var output in OutputFieldReferences)
            {
                outputs[output.Key] = output.Value.DereferenceToRoot(c);
            }
        }

        public void AddInputreference(Key key, ReferenceFieldModelController reference)
        {
            InputFieldReferences.Add(key, reference);
            (OperatorFieldModel as CompoundOperatorFieldModel).InputFieldReferences.Add(key, reference.ReferenceFieldModel);
        }

        public void RemoveInputReference(Key key)
        {
            InputFieldReferences.Remove(key);
            (OperatorFieldModel as CompoundOperatorFieldModel).InputFieldReferences.Remove(key);

        }

        public void AddOutputreference(Key key, ReferenceFieldModelController reference)
        {
            OutputFieldReferences.Add(key, reference);
            (OperatorFieldModel as CompoundOperatorFieldModel).OutputFieldReferences.Add(key, reference.ReferenceFieldModel);
        }

        public void RemoveOutputReference(Key key)
        {
            OutputFieldReferences.Remove(key);
            (OperatorFieldModel as CompoundOperatorFieldModel).OutputFieldReferences.Remove(key);
        }
    }
}
