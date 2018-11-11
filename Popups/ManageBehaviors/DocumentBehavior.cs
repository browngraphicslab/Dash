using Windows.UI;
using Windows.UI.Xaml.Controls;

namespace Dash.Popups
{
    public class DocumentBehavior
    {
        public string Trigger { get; set; }

        public string Behavior { get; set; }

        public ComboBox TriggerModifier { get; set; }

        public string Script { get; set; }

        /*
         * 0 = Triggering event
         * 1 = Trigger modifiers
         * 2 = Behavior
         * 3 = Behavior modifiers
         */
        public int[] Indices;

        public DocumentBehavior(string trigger, string behavior, ComboBox triggerModifier, string script, int[] indices)
        {
            Trigger = trigger;
            Behavior = behavior;
            TriggerModifier = triggerModifier;
            Script = script;
            Indices = indices;
        }
    }
}
