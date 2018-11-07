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

        public override async Task<(FieldControllerBase, ControlFlowFlag)> Execute(Scope scope)
        {
            scope = new Scope(scope);
            scope.DeclareVariable(_subVarName, new NumberController(0));
            var (field, _) = await _listToExecute.Execute(scope);
            if (!(field is IListController list))
            {
                return (null, ControlFlowFlag.None);
            }

            for (var i = 0; i < list.Count; i++)
            {
                scope.SetVariable(_subVarName, list.GetValue(i));
                var (field2, flags) = await _bodyToExecute.Execute(scope);
                switch (flags)
                {
                case ControlFlowFlag.Return:
                    return (field2, flags);
                case ControlFlowFlag.Break:
                    break;
                }
                list.SetValue(i, scope.GetVariable(_subVarName));
            }

            return (null, ControlFlowFlag.None);
        }

        public Op.Name GetOperatorName() => _opName;

        public override FieldControllerBase CreateReference(Scope scope) => throw new NotImplementedException();

        public override DashShared.TypeInfo Type => OperatorScript.GetOutputType(_opName);
    }
}
