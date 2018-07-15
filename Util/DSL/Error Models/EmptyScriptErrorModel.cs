namespace Dash
{

    public class EmptyScriptErrorModel : ScriptErrorModel
    {
        public EmptyScriptErrorModel()
        {
            ExtraInfo = ExtraInfo ?? "";
            ExtraInfo += "The script was a blank space";
        }

        public override string GetHelpfulString()
        {
            return $"The script or an inner part of the script was empty.";
        }
    }
}
