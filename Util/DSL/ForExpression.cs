using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Dash
{
    public class ForExpression : ScriptExpression
    {
        private readonly ScriptExpression _countDeclaration;
        private readonly ScriptExpression _forBinary;
        private readonly ScriptExpression _incrementExp;
        private readonly ExpressionChain _forBody;

        public ForExpression(ScriptExpression countDeclaration, ScriptExpression forBinary, ScriptExpression incrementExp, ExpressionChain forBody)
        {
            _countDeclaration = countDeclaration;
            _forBinary = forBinary;
            _incrementExp = incrementExp;
            _forBody = forBody;
        }
        public override async Task<(FieldControllerBase, ControlFlowFlag)> Execute(Scope scope)
        {
            bool timeout = false;
            var timer = new Timer(state => timeout = true, null, 5000, Timeout.Infinite);

            // Declare counter variable
            await _countDeclaration.Execute(scope);

            while (!timeout)
            {
                var boolRes = ((BoolController)(await _forBinary.Execute(scope)).Item1).Data;
                if (boolRes)
                {
                    var (field, flags) = await _forBody.Execute(scope);
                    switch (flags)
                    {
                    case ControlFlowFlag.Return:
                        return (field, flags);
                    case ControlFlowFlag.Break:
                        break;
                    //Continue gets handled lower down in the call stack
                    }

                    await _incrementExp.Execute(scope);
                }
                else
                {
                    return (null, ControlFlowFlag.None);
                }
            }

            throw new ScriptExecutionException(new TextErrorModel("Error: for loop timed out. Check for infinite loops or increase the timeout"));
        }

        public Op.Name GetOperatorName() => Op.Name.invalid;

        public override FieldControllerBase CreateReference(Scope scope) => throw new NotImplementedException();

        public override DashShared.TypeInfo Type => OperatorScript.GetOutputType(Op.Name.invalid);
    }
}
