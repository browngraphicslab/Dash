using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class ExecutionEnvironment
    {
        private ScriptExpression Expression { get; set; }

        public ExecutionEnvironment(ScriptExpression expression)
        {
            Expression = expression;
        }

        public async Task<(FieldControllerBase, ScriptErrorModel)> Execute(Scope scope)
        {
            try
            {
                var (field, _) = await Expression.Execute(scope);
                return (field, null);
            }
            catch (ScriptException se)
            {
                return (null, se.Error);
            }
            catch (ScriptExecutionException see)
            {
                return (null, see.Error);
            }
            catch (Exception e)
            {
                return (null, new GeneralScriptExecutionFailureModel(e));
            }
        }

        public static Task<(FieldControllerBase, ScriptErrorModel)> Run(ScriptExpression expression, Scope scope = null)
        {
            return new ExecutionEnvironment(expression).Execute(scope);
        }

        public static async Task<(FieldControllerBase, ScriptErrorModel)> Run(OperatorController op,
            List<FieldControllerBase> inputs, Scope scope = null)
        {
            try
            {
                var field = await OperatorScript.Run(op, inputs, scope);
                return (field, null);
            }
            catch (ScriptException se)
            {
                return (null, se.Error);
            }
            catch (ScriptExecutionException see)
            {
                return (null, see.Error);
            }
            catch (Exception e)
            {
                return (null, new GeneralScriptExecutionFailureModel(e));
            }
        }
    }
}
