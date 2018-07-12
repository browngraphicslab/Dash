namespace Dash
{

    public class VariableNotFoundExecutionErrorModel : ScriptExecutionErrorModel
    {
        public VariableNotFoundExecutionErrorModel(string variableName) => VariableName = variableName;

        public string VariableName { get; }

        public override string GetHelpfulString() => $" Exception:\n            UndefinedVariable\n      Feedback:\n            <{VariableName}> is not currently defined.\n" +
                                                     $"            Declare definition with <var {VariableName} = __> syntax or convert to string.";
    }
}
