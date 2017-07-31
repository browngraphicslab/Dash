using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    class CompoundOperator : OperatorFieldModelController
    {
        public CompoundOperator(OperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override FieldModelController Copy()
        {
            return new CompoundOperator(OperatorFieldModel);
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
    }
}
