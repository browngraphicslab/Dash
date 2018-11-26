using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

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

        public ManageBehaviorsViewModel()
        {
            Behaviors = new ObservableCollection<DocumentController>();
            _behaviors.CollectionChanged += BehaviorsChanged;
        }

        public ManageBehaviorsViewModel(ObservableCollection<DocumentController> behaviors)
        {
            Behaviors = behaviors;
            _behaviors.CollectionChanged += BehaviorsChanged;
        }

        public void RemoveBehavior(DocumentController behavior) => Behaviors.Remove(behavior);

        private void BehaviorsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
            case NotifyCollectionChangedAction.Add:
                break;
            case NotifyCollectionChangedAction.Move:
                break;
            case NotifyCollectionChangedAction.Remove:
                break;
            case NotifyCollectionChangedAction.Replace:
                break;
            case NotifyCollectionChangedAction.Reset:
                break;
            default:
                throw new ArgumentOutOfRangeException();
            }
        }
    }
}
