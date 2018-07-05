using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using Zu.TypeScript.TsTypes;

namespace Dash
{
    class FunctionCallExpression : ScriptExpression
    {
        private readonly string _name;

        public FunctionCallExpression(string name)
        {
            _name = name;
        }

        public override FieldControllerBase Execute(Scope scope)
        {

            //get variable from scope
            ///cope?.DeclareVariable(functionName, functionString);
            //var function = scope?.GetVariable(_name);

            //return (function as FunctionOperatorController).Execute(scope);
            return null;
        }



        public override FieldControllerBase CreateReference(Scope scope)
        {
            //return OperatorScript.CreateDocumentForOperator(
            //    _parameters.Select(
            //        kvp => new KeyValuePair<KeyController, FieldControllerBase>(kvp.Key,
            //            kvp.Value.CreateReference(scope))), _opName); //recursive linq

            return null;
        }

        public override DashShared.TypeInfo Type => TypeInfo.Any;
    }
}
