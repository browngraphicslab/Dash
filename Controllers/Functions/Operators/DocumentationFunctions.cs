using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public static class DocumentationFunctions
    {

        [OperatorReturnName("HelpString")]
        public static FieldControllerBase Help(TextController funcName = null)
        {
            if (string.IsNullOrWhiteSpace(funcName?.Data))
            {
                return OperatorScript.GetFunctionList();
            }
            var enumOut = Op.Parse(funcName.Data);
            if (enumOut == Op.Name.invalid) throw new ScriptExecutionException(new FunctionCallMissingScriptErrorModel(funcName.Data));
            return new ScriptHelpExcerpt(enumOut).GetExcerpt();
        }
    }
}
