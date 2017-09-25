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
        public static readonly string OperationBarDragKey = "4D9172C1-266F-4119-BB76-961D7D6C37B0";

        public CompoundOperatorFieldController(CompoundOperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public CompoundOperatorFieldController() : base(new CompoundOperatorFieldModel("Compound"))
        {
        }

        private CompoundOperatorFieldController(CompoundOperatorFieldController copy) : this()
        {
            Inputs = new ObservableDictionary<KeyControllerBase, IOInfo>(copy.Inputs);
            Outputs = new ObservableDictionary<KeyControllerBase, TypeInfo>(copy.Outputs);
            InputFieldReferences = new Dictionary<KeyControllerBase, List<FieldReference>>(copy.InputFieldReferences);
            OutputFieldReferences = new Dictionary<KeyControllerBase, FieldReference>(copy.OutputFieldReferences);
        }

        public override FieldModelController<OperatorFieldModel> Copy()
        {
            Debug.Assert(OperatorFieldModel is CompoundOperatorFieldModel);
            return new CompoundOperatorFieldController(this);
        }
        public override object GetValue(Context context)
        {
            throw new System.NotImplementedException();
        }
        public override bool SetValue(object value)
        {
            return false;
        }

        public override ObservableDictionary<KeyControllerBase, IOInfo> Inputs { get; } = new ObservableDictionary<KeyControllerBase, IOInfo>();

        public override ObservableDictionary<KeyControllerBase, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyControllerBase, TypeInfo>();

        public Dictionary<KeyControllerBase, List<FieldReference>> InputFieldReferences = new Dictionary<KeyControllerBase, List<FieldReference>>();
        public Dictionary<KeyControllerBase, FieldReference> OutputFieldReferences = new Dictionary<KeyControllerBase, FieldReference>();

        public override void Execute(Dictionary<KeyControllerBase, FieldControllerBase> inputs, Dictionary<KeyControllerBase, FieldControllerBase> outputs)
        {
            Context c = new Context();
            foreach (var reference in InputFieldReferences)
            {
                var refList = reference.Value;
                foreach (var fieldReference in refList)
                {
                    var doc = fieldReference.GetDocumentController(c);
                    doc.SetField(fieldReference.FieldKey, inputs[reference.Key].GetCopy(), true);
                }
            }
            foreach (var output in OutputFieldReferences)
            {
                outputs[output.Key] = output.Value.DereferenceToRoot(c);
            }
        }

        public void AddInputreference(KeyControllerBase key, FieldReference reference)
        {
            if (!InputFieldReferences.ContainsKey(key))
            {
                InputFieldReferences[key] = new List<FieldReference>();
                (OperatorFieldModel as CompoundOperatorFieldModel).InputFieldReferences[key] = new List<FieldReference>();
            }
            InputFieldReferences[key].Add(reference);
            (OperatorFieldModel as CompoundOperatorFieldModel).InputFieldReferences[key].Add(reference);
        }

        public void RemoveInputReference(KeyControllerBase key, FieldReference reference)
        {
            if (!InputFieldReferences.ContainsKey(key))
            {
                return;
            }
            InputFieldReferences[key].Remove(reference);
            (OperatorFieldModel as CompoundOperatorFieldModel).InputFieldReferences[key].Remove(reference);
        }

        public void AddOutputreference(KeyControllerBase key, FieldReference reference)
        {
            OutputFieldReferences.Add(key, reference);
            (OperatorFieldModel as CompoundOperatorFieldModel).OutputFieldReferences.Add(key, reference);
        }

        public void RemoveOutputReference(KeyControllerBase key)
        {
            OutputFieldReferences.Remove(key);
            (OperatorFieldModel as CompoundOperatorFieldModel).OutputFieldReferences.Remove(key);
        }
    }
}
