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

        private readonly string _subVarName;
        private readonly ScriptExpression _listToExecute;
        private readonly ExpressionChain _bodyToExecute;

        public ForInExpression(string opName, string subVarName, ScriptExpression listToExecute, ExpressionChain bodyToExecute)
        {
            _opName = opName;

            _subVarName = subVarName;
            _listToExecute = listToExecute;
            _bodyToExecute = bodyToExecute;
        }

        public override FieldControllerBase Execute(Scope scope)
        {
            scope = new Scope(scope);
            scope.DeclareVariable(_subVarName, new NumberController(0));
            var list = (_listToExecute.Execute(scope) as BaseListController);
            var listData = list?.Data;

            for (var i = 0; i < list?.Count; i++)
            {
                scope.SetVariable(_subVarName, listData[i]);
                _bodyToExecute.Execute(scope);
                listData[i] = scope.GetVariable(_subVarName);
                list.Data = listData;
            }

            return new ListController<FieldControllerBase>(list);
        }

        public string GetOperatorName() => _opName;


        public override FieldControllerBase CreateReference(Scope scope)
        {
            throw new NotImplementedException();
        }

        public override DashShared.TypeInfo Type => OperatorScript.GetOutputType(_opName);
    }
}
