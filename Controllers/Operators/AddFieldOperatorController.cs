using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class AddFieldOperatorController : OperatorFieldModelController
    {
        public AddFieldOperatorController(OperatorFieldModel opFieldModel) : base(opFieldModel)
        {
            OperatorFieldModel = opFieldModel; 
        }

        //Input Keys 
        //public static readonly Key InputDocKey = new Key("", "InputDoc");

        //Output Keys 
        public static readonly Key AddedFieldKey = new Key("TODO:CREATEGUID IF THIS IS GONNA STAY", "Addedfield");
        public static readonly Key OutputDocKey = new Key("ADDSOMETHING", "OutputDoc"); 

        public override ObservableDictionary<Key, TypeInfo> Inputs { get; } = new ObservableDictionary<Key, TypeInfo>
        {
            [AddedFieldKey] = TypeInfo.Document
        };

        public override ObservableDictionary<Key, TypeInfo> Outputs { get; } = new ObservableDictionary<Key, TypeInfo>
        {
            [OutputDocKey] = TypeInfo.Document
        }; 

        public override FieldModelController Copy()
        {
            return new AddFieldOperatorController(OperatorFieldModel); 
        }

        public override void Execute(Dictionary<Key, FieldModelController> inputs, Dictionary<Key, FieldModelController> outputs)
        {
            var fieldToAdd = inputs[AddedFieldKey]; //fieldmodelcontroller 

            //DocumentController of the doc you need to add fields to?? 
            var newDoc = (outputs[OutputDocKey] as DocumentFieldModelController).Data;
            newDoc.SetField(new Key("CREATE NEW GUID HEREERERERERERERE", "?idkhowtogethtekey"), fieldToAdd, true); 
        }
    }
}
