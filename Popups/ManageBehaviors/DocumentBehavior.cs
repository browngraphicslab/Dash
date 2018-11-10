namespace Dash.Popups
{
    public class DocumentBehavior
    {
        public string Trigger { get; set; }

        public string Reaction { get; set; }

        public DocumentBehavior(string trigger, string reaction)
        {
            Trigger = trigger;
            Reaction = reaction;
        }
    }
}
