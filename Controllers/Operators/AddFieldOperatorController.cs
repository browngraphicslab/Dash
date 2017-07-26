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
            [AddedFieldKey] = TypeInfo.Text // IT SHOULD BE ABLE TO HANDLE ALL TYPES FUCK YOU DAFDAKFDAYFDAFOUDAF DAKFA
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
            var fieldToAdd = inputs[AddedFieldKey] as TextFieldModelController; //fieldmodelcontroller 

            //DocumentController of the doc you need to add fields to?? 
            //outputs[OutputDocKey] = // the document of the input... ugh
            //var newDoc = (outputs[OutputDocKey] as DocumentFieldModelController).Data;
            //newDoc.SetField(new Key("CREATE NEW GUID HEREERERERERERERE", "?idkhowtogethtekey"), fieldToAdd, true); 
        }
    }
}
