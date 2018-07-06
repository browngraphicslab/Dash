using System;
using System.Threading;

// ReSharper disable once CheckNamespace
namespace Dash
{
    public class ForExpression : ScriptExpression
    {
        private readonly Op.Name _opName;
        private readonly ScriptExpression _countDeclaration;
        private readonly ScriptExpression _forBinary;
        private readonly ScriptExpression _incrementExp;
        private readonly ExpressionChain _forBody;

        private const string RecursiveError = "Exception: - infinite for loop detected. Feedback: Correct direction and bounds of loop, ensure body does not counteract counter increment/decrement";
        private string _loopRef;

        public ForExpression(Op.Name opName, ScriptExpression countDeclaration, ScriptExpression forBinary, ScriptExpression incrementExp, ExpressionChain forBody)
        {
            _opName = opName;
            _countDeclaration = countDeclaration;
            _forBinary = forBinary;
            _incrementExp = incrementExp;
            _forBody = forBody;
        }

        public override FieldControllerBase Execute(Scope scope)
        {
            var timer = new Timer(WhileTimeout, null, 5000, Timeout.Infinite);
            _loopRef = "";

            _countDeclaration.Execute(scope);

            while (((BoolController) _forBinary.Execute(scope)).Data && !InfiniteLoopDetected())
            {
                if (InfiniteLoopDetected()) return new TextController(RecursiveError);
                _forBody.Execute(scope); 
                _incrementExp.Execute(scope);
            }

            return new TextController("");
        }

        private void WhileTimeout(object status) => _loopRef = RecursiveError;

        private bool InfiniteLoopDetected() => _loopRef == RecursiveError;

        public Op.Name GetOperatorName() => _opName;

        public override FieldControllerBase CreateReference(Scope scope) => throw new NotImplementedException();

        public override DashShared.TypeInfo Type => OperatorScript.GetOutputType(_opName);
    }
}
