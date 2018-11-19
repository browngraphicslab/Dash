using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dash
{
    public class BreakLoopExpression : ScriptExpression
    {
        private readonly Dictionary<KeyController, ScriptExpression> _parameters;
        private Op.Name _opName = Op.Name.invalid;

        public BreakLoopExpression(Dictionary<KeyController, ScriptExpression> parameters = null)
        {
            _parameters = parameters;
        }

        public override Task<(FieldControllerBase, ControlFlowFlag)> Execute(Scope scope)
        {
            return Task.FromResult<(FieldControllerBase, ControlFlowFlag)>((null, ControlFlowFlag.Break));
        }

        public Op.Name GetOperatorName() => _opName;


        public Dictionary<KeyController, ScriptExpression> GetFuncParams() => _parameters;


        public override FieldControllerBase CreateReference(Scope scope)
        {
            throw new NotImplementedException();
        }

        public override DashShared.TypeInfo Type => OperatorScript.GetOutputType(_opName);
    }
}

