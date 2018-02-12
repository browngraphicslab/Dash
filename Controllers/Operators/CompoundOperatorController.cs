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
    public class CompoundOperatorController : OperatorController
    {
        public static readonly DocumentType MapType = new DocumentType("CFB46F9B-03FB-48E1-9AF9-DBBD266F0D31", "Compound");
        public static readonly string OperationBarDragKey = "4D9172C1-266F-4119-BB76-961D7D6C37B0";

        public CompoundOperatorController(CompoundOperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public CompoundOperatorController() : base(new CompoundOperatorModel())
        {
        }

        public override void Init()
        {
            var fm = Model as CompoundOperatorModel;
            InputFieldReferences = fm.InputFieldReferences.ToDictionary(
                k => ContentController<FieldModel>.GetController<KeyController>(k.Key),
                k => k.Value.Select(ContentController<FieldModel>
                    .GetController<ReferenceController>).ToList());


        }

        private CompoundOperatorController(CompoundOperatorController copy) : this()
        {
            Inputs = new ObservableDictionary<KeyController, IOInfo>(copy.Inputs);
            Outputs = new ObservableDictionary<KeyController, TypeInfo>(copy.Outputs);
            InputFieldReferences = new Dictionary<KeyController, List<ReferenceController>>(copy.InputFieldReferences);
            OutputFieldReferences = new Dictionary<KeyController, ReferenceController>(copy.OutputFieldReferences);
        }

        public override FieldModelController<OperatorModel> Copy()
        {
            Debug.Assert(OperatorFieldModel is CompoundOperatorModel);
            return new CompoundOperatorController(this);
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

        public Dictionary<KeyController, List<ReferenceController>> InputFieldReferences = new Dictionary<KeyController, List<ReferenceController>>();
        public Dictionary<KeyController, ReferenceController> OutputFieldReferences = new Dictionary<KeyController, ReferenceController>();

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args)
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
                InputFieldReferences[key] = new List<ReferenceController>();
                (OperatorFieldModel as CompoundOperatorModel).InputFieldReferences[key.Id] = new List<string>();
            }
            var r = reference.FieldReference.GetReferenceController();
            InputFieldReferences[key].Add(r);
            (OperatorFieldModel as CompoundOperatorModel).InputFieldReferences[key.Id].Add(r.Id);
            var ioInfo = new IOInfo(reference.Type, true);
            Inputs.Add(key, ioInfo);
            (OperatorFieldModel as CompoundOperatorModel).Inputs[key.Id] = ioInfo;
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
            (OperatorFieldModel as CompoundOperatorModel).InputFieldReferences[key.Id].Remove(r.Id);
            //TODO Update keys
            UpdateOnServer();
        }

        public void AddOutputreference(KeyController key, IOReference reference)
        {
            var r = reference.FieldReference.GetReferenceController();
            OutputFieldReferences.Add(key, r);
            (OperatorFieldModel as CompoundOperatorModel).OutputFieldReferences.Add(key.Id, r.Id);
            Outputs.Add(key, reference.Type);
            (OperatorFieldModel as CompoundOperatorModel).Outputs[key.Id] = reference.Type;
            UpdateOnServer();
        }

        public void RemoveOutputReference(KeyController key)
        {
            OutputFieldReferences.Remove(key);
            (OperatorFieldModel as CompoundOperatorModel).OutputFieldReferences.Remove(key.Id);
            //TODO Update keys
            UpdateOnServer();
        }
    }
}
