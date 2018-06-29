using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Dash
{
    public class ForExpression : ScriptExpression
    {
        private readonly Op.Name _opName;
        private readonly Dictionary<KeyController, ScriptExpression> _parameters;

        private readonly FieldControllerBase _recursiveError = new TextController("ERROR - an infinite loop was created.");
        private  FieldControllerBase _output;

        public ForExpression(Op.Name opName, Dictionary<KeyController, ScriptExpression> parameters)
        {
            _opName = opName;
            _parameters = parameters;
        }

        public override FieldControllerBase Execute(Scope scope)
        {
            var boolKey = ForOperatorController.BoolKey;
            var blockKey = ForOperatorController.ForBlockKey;
            var incrementKey = ForOperatorController.IncrementKey;
            var countDecKey = ForOperatorController.CounterDeclarationKey;

            var inputs = new Dictionary<KeyController, FieldControllerBase>
            {
                { countDecKey, _parameters[countDecKey].Execute(scope) },
                { boolKey, _parameters[boolKey].Execute(scope) }
            };

            //create a timer to catch infinite loops, that fires after 5 sec and then never fires again
            Timer whileTimer = new Timer(whileTimeout, null, 5000, Timeout.Infinite);

            //if there hasn't been an infinite loop timeout, keep looping
            while (_output != _recursiveError)
            {
                //see if boolean is true or false
                var boolRes = ((BoolController)_parameters[ForOperatorController.BoolKey].Execute(scope)).Data;
                if (boolRes)
                {
                    //boolean is true, so execute block again
                    if (inputs.ContainsKey(blockKey))
                    {
                        inputs[blockKey] = _parameters[blockKey].Execute(scope);
                        inputs[incrementKey] = _parameters[incrementKey].Execute(scope);
                    }
                    else
                    {
                        inputs.Add(blockKey, _parameters[blockKey].Execute(scope));
                        inputs.Add(incrementKey, _parameters[incrementKey].Execute(scope));
                    }


                    try
                    {
                        if (_output != _recursiveError)
                        {
                            //output = OperatorScript.Run(_opName, inputs, scope);
                        }
                    }
                    catch (Exception)
                    {
                        throw new ScriptExecutionException(new GeneralScriptExecutionFailureModel(_opName));
                    }
                }
                else
                {
                    //now that boolean is false, give it a null input and stop looping
                    if (!inputs.ContainsKey(blockKey))
                    {
                        inputs.Add(blockKey, null);
                    }

                    break;
                }
            }

            return _output;
        }

        private void whileTimeout(object status)
        {
            //set the output to an infinite recursion error
            _output = _recursiveError;
        }

        public Op.Name GetOperatorName() => _opName;


        public Dictionary<KeyController, ScriptExpression> GetFuncParams() => _parameters;


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
