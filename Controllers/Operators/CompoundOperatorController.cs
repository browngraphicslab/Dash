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

    }
}
