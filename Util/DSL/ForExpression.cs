using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

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

        public static Stopwatch sw = new Stopwatch();
        public override async Task<FieldControllerBase> Execute(Scope scope)
        {
            var timer = new Timer(WhileTimeout, null, 5000, Timeout.Infinite);
            _loopRef = "";

            // Declare counter variable
            await _countDeclaration.Execute(scope);

            // Body of for loop
            while (!InfiniteLoopDetected())
            {
                sw.Start();
                if (!((BoolController)await _forBinary.Execute(scope)).Data)
                {
                    sw.Stop();
                    break;
                }
                sw.Stop();
                // Have we timed out?
                if (false && _loopRef == RecursiveError) return new TextController(RecursiveError);

                // Execute body
                await _forBody.Execute(scope); 

                // Increment counter
                await _incrementExp.Execute(scope);
            }

            return new TextController("");
        }

        private void WhileTimeout(object status) => _loopRef = RecursiveError;

        private bool InfiniteLoopDetected() => false &&_loopRef == RecursiveError;

        public Op.Name GetOperatorName() => _opName;

        public override FieldControllerBase CreateReference(Scope scope) => throw new NotImplementedException();

        public override DashShared.TypeInfo Type => OperatorScript.GetOutputType(_opName);
    }
}
