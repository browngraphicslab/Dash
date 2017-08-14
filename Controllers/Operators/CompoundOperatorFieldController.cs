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
        public static readonly DocumentType MapType = new DocumentType("CFB46F9B-03FB-48E1-9AF9-DBBD266F0D31", "Compound");

        public CompoundOperatorFieldController(CompoundOperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public CompoundOperatorFieldController() : base(new CompoundOperatorFieldModel("Compound"))
        {
        }

        private CompoundOperatorFieldController(CompoundOperatorFieldController copy) : this()
        {
            Inputs = new ObservableDictionary<KeyController, TypeInfo>(copy.Inputs);
            Outputs = new ObservableDictionary<KeyController, TypeInfo>(copy.Outputs);
            InputFieldReferences = new Dictionary<KeyController, FieldReference>(copy.InputFieldReferences);
            OutputFieldReferences = new Dictionary<KeyController, FieldReference>(copy.OutputFieldReferences);
        }

        public override FieldModelController Copy()
        {
            Debug.Assert(OperatorFieldModel is CompoundOperatorFieldModel);
            return new CompoundOperatorFieldController(this);
        }

        public override ObservableDictionary<KeyController, TypeInfo> Inputs { get; } = new ObservableDictionary<KeyController, TypeInfo>();

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>();

        public Dictionary<KeyController, FieldReference> InputFieldReferences = new Dictionary<KeyController, FieldReference>();
        public Dictionary<KeyController, FieldReference> OutputFieldReferences = new Dictionary<KeyController, FieldReference>();

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

        public void AddInputreference(KeyController key, FieldReference reference)
        {
            InputFieldReferences.Add(key, reference);
            (OperatorFieldModel as CompoundOperatorFieldModel).InputFieldReferences.Add(key, reference);
        }

        public void RemoveInputReference(KeyController key)
        {
            InputFieldReferences.Remove(key);
            (OperatorFieldModel as CompoundOperatorFieldModel).InputFieldReferences.Remove(key);

        }

        public void AddOutputreference(KeyController key, FieldReference reference)
        {
            OutputFieldReferences.Add(key, reference);
            (OperatorFieldModel as CompoundOperatorFieldModel).OutputFieldReferences.Add(key, reference);
        }

        public void RemoveOutputReference(KeyController key)
        {
            OutputFieldReferences.Remove(key);
            (OperatorFieldModel as CompoundOperatorFieldModel).OutputFieldReferences.Remove(key);
        }
    }
}
