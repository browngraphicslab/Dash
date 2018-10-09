using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Dash.Annotations;
using iText.Kernel.Events;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public class ActionGroupViewModel : ObservableCollection<ActionViewModel>, IGrouping<string, ActionViewModel>
    {
        public string GroupTitle { get; }

        public ObservableCollection<ActionViewModel> Actions { get; }

        public ActionGroupViewModel(string groupTitle, IEnumerable<ActionViewModel> actions)
        {
            GroupTitle = groupTitle;
            Actions = new ObservableCollection<ActionViewModel>(actions);
            Actions.CollectionChanged += Actions_CollectionChanged;
            foreach (var actionViewModel in actions)
            {
                Add(actionViewModel);
            }
        }

        private void Actions_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateFilter();
        }

        private Predicate<ActionViewModel> _predicate;
        public void Filter(Predicate<ActionViewModel> pred)
        {
            _predicate = pred;
            UpdateFilter();
        }

        private bool Matches(ActionViewModel vm)
        {
            return _predicate?.Invoke(vm) ?? true;
        }

        private void UpdateFilter()
        {
            HashSet<ActionViewModel> set = new HashSet<ActionViewModel>();
            List<ActionViewModel> toRemove = new List<ActionViewModel>();
            foreach (var vm in this)
            {
                if (Matches(vm))
                {
                    set.Add(vm);
                }
                else
                {
                    toRemove.Add(vm);
                }
            }

            foreach (var vm in toRemove)
            {
                Remove(vm);
            }

            int index = 0;
            foreach (var actionViewModel in Actions)
            {
                if (set.Contains(actionViewModel))
                {
                    index++;
                    continue;
                }

                if (Matches(actionViewModel))
                {
                    Insert(index, actionViewModel);
                    index++;
                }
            }
        }

        public void SetActions(IEnumerable<ActionViewModel> actions)
        {
            Actions.Clear();
            foreach (var actionViewModel in actions)
            {
                Actions.Add(actionViewModel);
            }
            UpdateFilter();
        }

        public string Key => GroupTitle;
    }

    public sealed partial class ActionMenu : UserControl, INotifyPropertyChanged
    {
        private bool _useFilterBox = true;
        public ObservableCollection<ActionGroupViewModel> Groups { get; } = new ObservableCollection<ActionGroupViewModel>();

        public bool UseFilterBox
        {
            get => _useFilterBox;
            set
            {
                if (value == _useFilterBox)
                {
                    return;
                }

                _useFilterBox = value;
                OnPropertyChanged();
            }
        }

        private Point _targetPoint;

        private string _filterString;
        public string FilterString
        {
            get => _filterString;
            set
            {
                _filterString = value;
                var predicate = GetFilterPredicate(value);
                foreach (var vm in Groups)
                {
                    vm.Filter(predicate);
                }
            }
        }

        public ActionMenu(Point targetPoint)
        {
            InitializeComponent();
            _targetPoint = targetPoint;


            GroupCVS.Source = Groups;
        }

        private Predicate<object> GetFilterPredicate(string filterText)
        {
            Predicate<object> predicate;
            if (string.IsNullOrWhiteSpace(filterText))
            {
                predicate = null;
            }
            else
            {
                predicate = obj => obj is ActionViewModel avm && avm.Title.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0;
            }

            return predicate;
        }

        private void XFilterBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            FilterString = XFilterBox.Text;
        }

        public void AddGroup(string groupName, List<ActionViewModel> actions)
        {
            var existingGroup = Groups.FirstOrDefault(group => group.GroupTitle == groupName);
            if (existingGroup == null)
            {
                var vm = new ActionGroupViewModel(groupName, new ObservableCollection<ActionViewModel>(actions));
                vm.Filter(GetFilterPredicate(_filterString));
                Groups.Add(vm);
            }
            else
            {
                existingGroup.SetActions(actions);
            }
        }

        public void AddAction(string groupName, ActionViewModel action)
        {
            var existingGroup = Groups.FirstOrDefault(group => group.GroupTitle == groupName);

            if (existingGroup == null)
            {
                Groups.Add(existingGroup = new ActionGroupViewModel(groupName, new List<ActionViewModel>()));
            }

            existingGroup.Actions.Add(action);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool InvokeAction(string actionName, Point point)
        {
            foreach (var group in Groups)
            {
                foreach (var action in group)
                {
                    if (action.Title.Equals(actionName, StringComparison.OrdinalIgnoreCase))
                    {
                        action.Action.Invoke(point);
                    }
                }
            }
            return false;
        }

        public event Action<bool> ActionCommitted;

        private async void ListViewBase_OnItemClick(object sender, ItemClickEventArgs e)
        {
            var action = ((ActionViewModel) e.ClickedItem).Action;
            var removeTextBox = action != null && await action.Invoke(_targetPoint);
            MainPage.Instance.xCanvas.Children.Remove(this);
            OnActionCommitted(removeTextBox);
        }

        private void OnActionCommitted(bool obj)
        {
            ActionCommitted?.Invoke(obj);
        }
    }
}
