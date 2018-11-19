using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dash
{
    public class WhileExpression : ScriptExpression
    {
        private readonly ScriptExpression _whileCondition;
        private readonly ScriptExpression _whileBody;

        public WhileExpression(ScriptExpression whileCondition, ScriptExpression whileBody)
        {
            _whileCondition = whileCondition;
            _whileBody = whileBody;
        }

        public override async Task<(FieldControllerBase, ControlFlowFlag)> Execute(Scope scope)
        {
            //create a timer to catch infinite loops, that fires after 5 sec and then never fires again
            bool timeout = false;
            var timer = new Timer(state => timeout = true, null, 5000, Timeout.Infinite);

            //if there hasn't been an infinite loop timeout, keep looping
            while (!timeout)
            {
                //see if boolean is true or false
                var boolRes = ((BoolController)(await _whileCondition.Execute(scope)).Item1).Data;
                 if (boolRes)
                {
                    //boolean is true, so execute block again
                    var (field, flags) = await _whileBody.Execute(scope);
                    switch (flags)
                    {
                    case ControlFlowFlag.Return:
                        return (field, flags);
                    case ControlFlowFlag.Break:
                        break;
                    }
                }
                else
                {
                    //now that boolean is false, give it a null input and stop looping
                    return (null, ControlFlowFlag.None);
                }
            }

            throw new ScriptExecutionException(new TextErrorModel("Error: while loop timed out. Check for infinite loops or increase the timeout"));
        }

        public Op.Name GetOperatorName() => Op.Name.invalid;

        public override FieldControllerBase CreateReference(Scope scope)
        {
            throw new NotImplementedException();
        }

        public override DashShared.TypeInfo Type => OperatorScript.GetOutputType(Op.Name.invalid);
    }
}
