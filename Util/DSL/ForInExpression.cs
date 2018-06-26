using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DashShared;

namespace Dash
{
    public class ForInExpression : ScriptExpression
    {
        private readonly string _opName;
        private readonly Dictionary<KeyController, ScriptExpression> _parameters;

        private readonly FieldControllerBase _recursiveError = new TextController("ERROR - an infinite loop was created.");
        private FieldControllerBase _output;

        public ForInExpression(string opName, Dictionary<KeyController, ScriptExpression> parameters)
        {
            _opName = opName;
            _parameters = parameters;
        }

        public override FieldControllerBase Execute(Scope scope)
        {
            var countDecKey = ForInOperatorController.CounterDeclarationKey;
            var incAssignKey = ForInOperatorController.IncrementAndAssignmentKey;
            var subVarKey = ForInOperatorController.SubVarNameKey;
            var subVarDecKey = ForInOperatorController.SubVarDeclarationKey;
            var listKey = ForInOperatorController.ListNameKey;
            var blockKey = ForInOperatorController.ForInBlockKey;
            var writeKey = ForInOperatorController.WriteToListKey;

            var inputs = new Dictionary<KeyController, FieldControllerBase>
            {
                { subVarDecKey, _parameters[subVarDecKey].Execute(scope) }, //Declares "<given dummy variable> = 0"
                { countDecKey, _parameters[countDecKey].Execute(scope) }, //Declares "var c = 0"
                { listKey, _parameters[listKey].Execute(scope) }, //Extracts the list currently associated with "cookies"
                { subVarKey, _parameters[subVarKey].Execute(scope) } //Doesn't necessarily have to happen here, but stores the current value of the subvariable: at initialization should be zero
            };

            var list = (inputs[listKey] as BaseListController)?.Data;
            var length = list?.Count;

            //create a timer to catch infinite loops, that fires after 5 sec and then never fires again
            var whileTimer = new Timer(WhileTimeout, null, DashConstants.ScriptingInfiniteLoopTimeout, Timeout.Infinite);

            //if there hasn't been an infinite loop timeout, keep looping
            while (_output != _recursiveError)
            {
                inputs[subVarKey] = _parameters[subVarKey].Execute(scope);
                //see if boolean is true or false
                if ((int)(double)inputs[subVarKey].GetValue(null) <= length)
                {
                    //boolean is true, so execute block again
                    if (inputs.ContainsKey(blockKey))
                    {
                        inputs[incAssignKey] = _parameters[incAssignKey].Execute(scope);
                        inputs[blockKey] = _parameters[blockKey].Execute(scope);
                        inputs[writeKey] = _parameters[writeKey].Execute(scope);
                    }
                    else
                    {
                        inputs.Add(incAssignKey, _parameters[incAssignKey].Execute(scope));
                        inputs.Add(blockKey, _parameters[blockKey].Execute(scope));
                        inputs.Add(writeKey, _parameters[writeKey].Execute(scope));
                    }

                    try
                    {
                        if (_output != _recursiveError)
                        {
                            _output = OperatorScript.Run(_opName, inputs, scope);
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

        //set the output to an infinite recursion error
        private void WhileTimeout(object status) => _output = _recursiveError;

        public string GetOperatorName() => _opName;


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
