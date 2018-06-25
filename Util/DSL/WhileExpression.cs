using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dash
{
    class WhileExpression : ScriptExpression
    {
        private string _opName;
        private Dictionary<KeyController, ScriptExpression> _parameters;

        private FieldControllerBase recursiveError = new TextController("ERROR - an infinite loop was created.");
        FieldControllerBase output = null;

        public WhileExpression(string opName, Dictionary<KeyController, ScriptExpression> parameters)
        {
            this._opName = opName;
            this._parameters = parameters;
        }

        public override FieldControllerBase Execute(Scope scope)
        {
            var inputs = new Dictionary<KeyController, FieldControllerBase>();
            inputs.Add(WhileOperatorController.BoolKey, _parameters[WhileOperatorController.BoolKey].Execute(scope));

            var BlockKey = WhileOperatorController.BlockKey;
            
            //create a timer to catch infinite loops, that fires after 5 sec and then never fires again
            Timer whileTimer = new Timer(whileTimeut, null, 5000, Timeout.Infinite);            

            //if there hasn't been an infinite loop timeout, keep looping
            while (output != recursiveError)
            {
                //see if boolean is true or false
                bool boolRes = ((BoolController)_parameters[WhileOperatorController.BoolKey].Execute(scope)).Data;
                 if (boolRes)
                {
                    //boolean is true, so execute block again
                    if (inputs.ContainsKey(BlockKey))
                    {
                        inputs[BlockKey] = _parameters[BlockKey].Execute(scope);
                    }
                    else
                    {
                        inputs.Add(BlockKey, _parameters[BlockKey].Execute(scope));
                    }
                    

                    try
                    {
                        if (output != recursiveError)
                        {
                            output = OperatorScript.Run(_opName, inputs, scope);
                        }
                    }
                    catch (Exception e)
                    {
                        throw new ScriptExecutionException(new GeneralScriptExecutionFailureModel(_opName));
                    }
                }
                else
                {
                    //now that boolean is false, give it a null input and stop looping
                    if (!inputs.ContainsKey(BlockKey))
                    {
                        inputs.Add(BlockKey, null);
                    }
                   
                    break;
                }
            }

            return output;
        }

        private void whileTimeut(object status) {
            //set the output to an infinite recursion error
            output = recursiveError;
        }

        public string GetOperatorName()
        {
            return _opName;
        }


        public Dictionary<KeyController, ScriptExpression> GetFuncParams()
        {
            return _parameters;
        }


        public override FieldControllerBase CreateReference(Scope scope)
        {
            return OperatorScript.CreateDocumentForOperator(
                _parameters.Select(
                    kvp => new KeyValuePair<KeyController, FieldControllerBase>(kvp.Key,
                        kvp.Value.CreateReference(scope))), _opName); //recursive linq
        }

        public override DashShared.TypeInfo Type => OperatorScript.GetOutputType(_opName);
    }
}
