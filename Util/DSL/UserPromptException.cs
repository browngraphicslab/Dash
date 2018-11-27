namespace Dash
{
    public class UserPromptException : DSLException
    {
        private readonly string _prompt;

        public UserPromptException(string prompt)
        {
            _prompt = prompt;
        }

        public override string GetHelpfulString() => "Prompt user after exception with a smart response.";

        public TextController GetPrompt() => new TextController(_prompt);
    }
}
