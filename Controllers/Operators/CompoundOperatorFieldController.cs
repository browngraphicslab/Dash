using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using DashShared.Models;

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

        public override void Init()
        {
            var fm = Model as CompoundOperatorFieldModel;
            InputFieldReferences = fm.InputFieldReferences.ToDictionary(
                k => ContentController<KeyModel>.GetController<KeyController>(k.Key),
                k => k.Value.Select(ContentController<FieldModel>
                    .GetController<ReferenceFieldModelController>).ToList());


        }

        private CompoundOperatorFieldController(CompoundOperatorFieldController copy) : this()
        {
            Inputs = new ObservableDictionary<KeyController, IOInfo>(copy.Inputs);
            Outputs = new ObservableDictionary<KeyController, TypeInfo>(copy.Outputs);
            InputFieldReferences = new Dictionary<KeyController, List<ReferenceFieldModelController>>(copy.InputFieldReferences);
            OutputFieldReferences = new Dictionary<KeyController, ReferenceFieldModelController>(copy.OutputFieldReferences);
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

        public Dictionary<KeyController, List<ReferenceFieldModelController>> InputFieldReferences = new Dictionary<KeyController, List<ReferenceFieldModelController>>();
        public Dictionary<KeyController, ReferenceFieldModelController> OutputFieldReferences = new Dictionary<KeyController, ReferenceFieldModelController>();

        public override void ExecuteAsync(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs)
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

        public void AddInputreference(KeyController key, IOReference reference)
        {
            if (!InputFieldReferences.ContainsKey(key))
            {
                InputFieldReferences[key] = new List<ReferenceFieldModelController>();
                (OperatorFieldModel as CompoundOperatorFieldModel).InputFieldReferences[key.Id] = new List<string>();
            }
            var r = reference.FieldReference.GetReferenceController();
            InputFieldReferences[key].Add(r);
            (OperatorFieldModel as CompoundOperatorFieldModel).InputFieldReferences[key.Id].Add(r.Id);
            var ioInfo = new IOInfo(reference.Type, true);
            Inputs.Add(key, ioInfo);
            (OperatorFieldModel as CompoundOperatorFieldModel).Inputs[key.Id] = ioInfo;
            UpdateOnServer();
        }

        public void RemoveInputReference(KeyController key, IOReference reference)
        {
            if (!InputFieldReferences.ContainsKey(key))
            {
                return;
            }
            var r = reference.FieldReference.GetReferenceController();
            InputFieldReferences[key].Remove(r);
            (OperatorFieldModel as CompoundOperatorFieldModel).InputFieldReferences[key.Id].Remove(r.Id);
            //TODO Update keys
            UpdateOnServer();
        }

        public void AddOutputreference(KeyController key, IOReference reference)
        {
            var r = reference.FieldReference.GetReferenceController();
            OutputFieldReferences.Add(key, r);
            (OperatorFieldModel as CompoundOperatorFieldModel).OutputFieldReferences.Add(key.Id, r.Id);
            Outputs.Add(key, reference.Type);
            (OperatorFieldModel as CompoundOperatorFieldModel).Outputs[key.Id] = reference.Type;
            UpdateOnServer();
        }

        public void RemoveOutputReference(KeyController key)
        {
            OutputFieldReferences.Remove(key);
            (OperatorFieldModel as CompoundOperatorFieldModel).OutputFieldReferences.Remove(key.Id);
            //TODO Update keys
            UpdateOnServer();
        }
    }
}
