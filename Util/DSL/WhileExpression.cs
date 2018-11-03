using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dash
{
    public class WhileExpression : ScriptExpression
    {
        private readonly Op.Name _opName;
        private readonly Dictionary<KeyController, ScriptExpression> _parameters;

        private readonly FieldControllerBase _recursiveError = new TextController("ERROR - an infinite loop was created.");
        private FieldControllerBase _output;

        public WhileExpression(Op.Name opName, Dictionary<KeyController, ScriptExpression> parameters)
        {
            _opName = opName;
            _parameters = parameters;
        }

        public override async Task<(FieldControllerBase, ControlFlowFlag)> Execute(Scope scope)
        {
            var blockKey = WhileOperatorController.BlockKey;
            
            //create a timer to catch infinite loops, that fires after 5 sec and then never fires again
            var timer = new Timer(WhileTimeout, null, 5000, Timeout.Infinite);

            //if there hasn't been an infinite loop timeout, keep looping
            while (_output != _recursiveError)
            {
                //see if boolean is true or false
                var boolRes = ((BoolController)(await _parameters[WhileOperatorController.BoolKey].Execute(scope)).Item1).Data;
                 if (boolRes)
                {
                    //boolean is true, so execute block again
                    var (field, flags) = await _parameters[blockKey].Execute(scope);
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
                    break;
                }
            }

            return (null, ControlFlowFlag.None);
        }

        //set the output to an infinite recursion error
        private void WhileTimeout(object status) => _output = _recursiveError;

        public Op.Name GetOperatorName() => _opName;

        public Dictionary<KeyController, ScriptExpression> GetFuncParams() => _parameters;


        public override FieldControllerBase CreateReference(Scope scope)
        {
            throw new NotImplementedException();
        }

        public override DashShared.TypeInfo Type => OperatorScript.GetOutputType(_opName);
    }
}
