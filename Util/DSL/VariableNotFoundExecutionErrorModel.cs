namespace Dash
{

    public class VariableNotFoundExecutionErrorModel : ScriptExecutionErrorModel
    {
        public VariableNotFoundExecutionErrorModel(string variableName)
        {
            VariableName = variableName;
        }

        public string VariableName { get; private set; }

        public override string GetHelpfulString()
        {
            return "A variable used in the script wasn't present at the time of execution.  Variable requested: " +
                   VariableName;
        }
    }
}
