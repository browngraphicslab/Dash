using System.Collections.ObjectModel;

namespace Dash.Popups
{
    public class ManageBehaviorsViewModel : ViewModelBase
    {
        private ObservableCollection<DocumentController> _behaviors;

        public ObservableCollection<DocumentController> Behaviors
        {
            get => _behaviors;
            set => SetProperty(ref _behaviors, value);
        }

        public void RemoveBehavior(DocumentController behavior) => Behaviors.Remove(behavior);

        public ManageBehaviorsViewModel()
        {
            Behaviors = new ObservableCollection<DocumentController>();
        }
    }
}
