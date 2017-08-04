using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class CompoundOperatorFieldController : OperatorFieldModelController
    {
        public CompoundOperatorFieldController(CompoundOperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public CompoundOperatorFieldController() : base(new CompoundOperatorFieldModel("Compound"))
        {
        }

        public override FieldModelController Copy()
        {
            Debug.Assert(OperatorFieldModel is CompoundOperatorFieldModel);
            return new CompoundOperatorFieldController(OperatorFieldModel as CompoundOperatorFieldModel);
        }

        public override ObservableDictionary<KeyController, TypeInfo> Inputs { get; } = new ObservableDictionary<KeyController, TypeInfo>();

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>();

        public Dictionary<KeyController, ReferenceFieldModelController> InputFieldReferences = new Dictionary<KeyController, ReferenceFieldModelController>();
        public Dictionary<KeyController, ReferenceFieldModelController> OutputFieldReferences = new Dictionary<KeyController, ReferenceFieldModelController>();

        public override void Execute(Dictionary<KeyController, FieldModelController> inputs, Dictionary<KeyController, FieldModelController> outputs)
        {
            Context c = new Context();
            foreach (var reference in InputFieldReferences)
            {
                var doc = reference.Value.GetDocumentController(c);
                doc.SetField(reference.Value.FieldKey, inputs[reference.Key].Copy(), true);
            }
            foreach (var output in OutputFieldReferences)
            {
                outputs[output.Key] = output.Value.DereferenceToRoot(c);
            }
        }

        public void AddInputreference(KeyController key, ReferenceFieldModelController reference)
        {
            InputFieldReferences.Add(key, reference);
            (OperatorFieldModel as CompoundOperatorFieldModel).InputFieldReferences.Add(key, reference.ReferenceFieldModel);
        }

        public void RemoveInputReference(KeyController key)
        {
            InputFieldReferences.Remove(key);
            (OperatorFieldModel as CompoundOperatorFieldModel).InputFieldReferences.Remove(key);

        }

        public void AddOutputreference(KeyController key, ReferenceFieldModelController reference)
        {
            OutputFieldReferences.Add(key, reference);
            (OperatorFieldModel as CompoundOperatorFieldModel).OutputFieldReferences.Add(key, reference.ReferenceFieldModel);
        }

        public void RemoveOutputReference(KeyController key)
        {
            OutputFieldReferences.Remove(key);
            (OperatorFieldModel as CompoundOperatorFieldModel).OutputFieldReferences.Remove(key);
        }
    }
}
