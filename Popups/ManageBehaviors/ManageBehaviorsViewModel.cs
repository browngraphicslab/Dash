using System.Collections.ObjectModel;

namespace Dash.Popups
{
    public class ManageBehaviorsViewModel : ViewModelBase
    {
        private ObservableCollection<DocumentBehavior> _behaviors;

        public ObservableCollection<DocumentBehavior> Behaviors
        {
            get => _behaviors;
            set => SetProperty(ref _behaviors, value);
        }

        public void RemoveBehavior(DocumentBehavior behavior) => Behaviors.Remove(behavior);

        public ManageBehaviorsViewModel()
        {
            Behaviors = new ObservableCollection<DocumentBehavior>();
        }
    }
}
