using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class RemoveCommonWordsOperator : OperatorFieldModelController
    {
        public override ObservableDictionary<KeyController, IOInfo> Inputs => throw new NotImplementedException();

        public override ObservableDictionary<KeyController, TypeInfo> Outputs => throw new NotImplementedException();

        public RemoveCommonWordsOperator(OperatorFieldModel model) : base(model)
        {
            
        }

        public override FieldModelController<OperatorFieldModel> Copy()
        {
            throw new NotImplementedException();
        }

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs)
        {
            throw new NotImplementedException();
        }

        public override object GetValue(Context context)
        {
            throw new NotImplementedException();
        }

        public override bool SetValue(object value)
        {
            throw new NotImplementedException();
        }
    }
}
