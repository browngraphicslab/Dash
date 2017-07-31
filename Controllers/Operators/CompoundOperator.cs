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

        public override void Execute(Dictionary<Key, FieldModelController> inputs, Dictionary<Key, FieldModelController> outputs)
        {
            foreach (var output in Outputs.Keys)
            {
                //outputs[output] = ;
            }
        }
    }
}
