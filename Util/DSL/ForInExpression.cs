using System;
using System.Threading.Tasks;

namespace Dash
{
    public class ForInExpression : ScriptExpression
    {
        private readonly Op.Name _opName;

        private readonly string _subVarName;
        private readonly ScriptExpression _listToExecute;
        private readonly ExpressionChain _bodyToExecute;

        public ForInExpression(Op.Name opName, string subVarName, ScriptExpression listToExecute, ExpressionChain bodyToExecute)
        {
            _opName = opName;

            _subVarName = subVarName;
            _listToExecute = listToExecute;
            _bodyToExecute = bodyToExecute;
        }

        public override async Task<FieldControllerBase> Execute(Scope scope)
        {
            scope = new Scope(scope);
            scope.DeclareVariable(_subVarName, new NumberController(0));
            var list = await _listToExecute.Execute(scope) as BaseListController;

            for (var i = 0; i < list?.Count; i++)
            {
                scope.SetVariable(_subVarName, list.GetValue(i));
                await _bodyToExecute.Execute(scope);
                list.SetValue(i, scope.GetVariable(_subVarName));
            }

            return list;
        }

        public Op.Name GetOperatorName() => _opName;

        public override FieldControllerBase CreateReference(Scope scope) => throw new NotImplementedException();

        public override DashShared.TypeInfo Type => OperatorScript.GetOutputType(_opName);
    }
}
