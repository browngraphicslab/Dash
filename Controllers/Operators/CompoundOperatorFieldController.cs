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

        public CompoundOperatorFieldController() : base(new CompoundOperatorFieldModel())
        {
        }

        private CompoundOperatorFieldController(CompoundOperatorFieldController copy) : this()
        {
            Inputs = new ObservableDictionary<KeyController, IOInfo>(copy.Inputs);
            Outputs = new ObservableDictionary<KeyController, TypeInfo>(copy.Outputs);
            InputFieldReferences = new Dictionary<KeyController, List<FieldReference>>(copy.InputFieldReferences);
            OutputFieldReferences = new Dictionary<KeyController, FieldReference>(copy.OutputFieldReferences);
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

        public override ObservableDictionary<KeyController, IOInfo> Inputs { get; } = new ObservableDictionary<KeyController, IOInfo>();

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>();

        public Dictionary<KeyController, List<FieldReference>> InputFieldReferences = new Dictionary<KeyController, List<FieldReference>>();
        public Dictionary<KeyController, FieldReference> OutputFieldReferences = new Dictionary<KeyController, FieldReference>();

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs)
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

        public void AddInputreference(KeyController key, FieldReference reference)
        {
            if (!InputFieldReferences.ContainsKey(key))
            {
                InputFieldReferences[key] = new List<FieldReference>();
                (OperatorFieldModel as CompoundOperatorFieldModel).InputFieldReferences[key] = new List<FieldReference>();
            }
            InputFieldReferences[key].Add(reference);
            (OperatorFieldModel as CompoundOperatorFieldModel).InputFieldReferences[key].Add(reference);
        }

        public void RemoveInputReference(KeyController key, FieldReference reference)
        {
            if (!InputFieldReferences.ContainsKey(key))
            {
                return;
            }
            InputFieldReferences[key].Remove(reference);
            (OperatorFieldModel as CompoundOperatorFieldModel).InputFieldReferences[key].Remove(reference);
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
