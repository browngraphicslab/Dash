using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Dash.Annotations;
using Microsoft.Toolkit.Uwp.UI;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public class ActionGroupViewModel : ViewModelBase
    {
        public string GroupTitle { get; }

        public AdvancedCollectionView BindableActions { get; }

        public ObservableCollection<ActionViewModel> Actions { get; }

        public ActionGroupViewModel(string groupTitle, IEnumerable<ActionViewModel> actions)
        {
            GroupTitle = groupTitle;
            Actions = new ObservableCollection<ActionViewModel>(actions);
            BindableActions = new AdvancedCollectionView(Actions);
        }

        public void SetActions(IEnumerable<ActionViewModel> actions)
        {
            Actions.Clear();
            foreach (var actionViewModel in actions)
            {
                Actions.Add(actionViewModel);
            }
        }
    }

    public sealed partial class ActionMenu : UserControl, INotifyPropertyChanged
    {
        public ObservableCollection<ActionGroupViewModel> Groups { get; } = new ObservableCollection<ActionGroupViewModel>();

        public AdvancedCollectionView BindableGroups { get; }

        public ActionMenu()
        {
            this.InitializeComponent();
            BindableGroups = new AdvancedCollectionView(Groups);
        }

        private Predicate<object> GetFilterPredicate()
        {
            var filterText = XFilterBox.Text;
            Predicate<object> predicate;
            if (string.IsNullOrWhiteSpace(filterText))
            {
                predicate = null;
            }
            else
            {
                predicate = obj => obj is ActionViewModel avm && avm.Title.Contains(filterText);
            }

            return predicate;
        }

        private void XFilterBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var predicate = GetFilterPredicate();
            foreach (var vm in Groups)
            {
                vm.BindableActions.Filter = predicate;
            }
            BindableGroups.Filter = obj => obj is ActionGroupViewModel agvm && agvm.BindableActions.Any();
        }

        public void AddGroup(string groupName, List<ActionViewModel> actions)
        {
            var existingGroup = Groups.FirstOrDefault(group => group.GroupTitle == groupName);
            if (existingGroup == null)
            {
                var vm = new ActionGroupViewModel(groupName, new ObservableCollection<ActionViewModel>(actions));
                vm.BindableActions.Filter = GetFilterPredicate();
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
                throw new ArgumentException("No group with given name exists", nameof(groupName));
            }

            existingGroup.Actions.Add(action);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ListViewBase_OnItemClick(object sender, ItemClickEventArgs e)
        {
            (e.ClickedItem as ActionViewModel)?.Action?.Invoke();
        }
    }
}
