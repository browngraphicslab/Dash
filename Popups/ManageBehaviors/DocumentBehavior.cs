using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Dash.Popups
{
    public class DocumentBehavior
    {
        public string Trigger { get; set; }

        public string Behavior { get; set; }

        public ComboBox TriggerModifier { get; set; }

        public string Title { get; set; }

        public string Script { get; set; }

        public DocumentController BehaviorDoc { get; set; }

        public DocumentBehavior(DocumentController behaviorDoc)
        {
            var binding = new FieldBinding<TextController>()
            {
                Document = behaviorDoc,
                Mode = BindingMode.OneWay,
                Key = KeyStore.ScriptTextKey,
            };
            
        }
    }
}
