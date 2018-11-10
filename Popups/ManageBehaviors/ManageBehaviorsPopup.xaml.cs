using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Popups
{
    public sealed partial class ManageBehaviorsPopup
    {
        private TaskCompletionSource<List<OperatorController>> _tcs;
        private readonly Dictionary<int, ComboBox> _modifierMapping = new Dictionary<int, ComboBox>();
        private readonly ObservableCollection<string> Behaviors = new ObservableCollection<string>();
        private readonly int BehaviorCount = 3;

        public ManageBehaviorsViewModel ViewModel => DataContext as ManageBehaviorsViewModel;

        public ManageBehaviorsPopup()
        {
            InitializeComponent();
            Behaviors.Add("Add new behavior");
            SetupComboBoxes();
        }

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {

        }

        private void SetupComboBoxes()
        {
            _modifierMapping[0] = xTappedModifiers;
            _modifierMapping[1] = xDeletedModifiers;
            _modifierMapping[2] = xFieldModifiers;
        }

        private void OnOpened(object sender, object e)
        {
            //xComboBox.SelectedItem = null;
        }

        public Task<List<OperatorController>> OpenAsync()
        {
            _tcs = new TaskCompletionSource<List<OperatorController>>();
            xBehaviorsPopup.IsOpen = true;
            MainPage.Instance.XGrid.Children.Add(this);
            MainPage.Instance.xOverlay.Visibility = Visibility.Visible;
            HorizontalAlignment = HorizontalAlignment.Center;
            VerticalAlignment = VerticalAlignment.Center;
            Width = 700;
            Height = 500;
            return _tcs.Task;
        }

        private void AddOnClick(object sender, RoutedEventArgs e)
        {
            var applyingNew = xAddNewBehaviorPanel.Visibility == Visibility.Visible;
            if (applyingNew)
            {
                if (xTriggeringEvent.SelectedItem != null && xBehavior.SelectedItem != null)
                {
                    xAddNewBehaviorPanel.Visibility = Visibility.Collapsed;
                    xCancelButton.Visibility = Visibility.Collapsed;
                    xConfirmButton.Visibility = Visibility.Visible;
                    xAddTextbox.Text = "Add New";
                    var trigger = ((ComboBoxItem)xTriggeringEvent.SelectedItem).Content.ToString();
                    var behavior = ((ComboBoxItem)xBehavior.SelectedItem).Content.ToString();
                    ViewModel.Behaviors.Add(new DocumentBehavior(trigger, behavior));
                    xAddButton.Opacity = 1;
                }
            } else
            {
                xAddNewBehaviorPanel.Visibility = Visibility.Visible;
                xCancelButton.Visibility = Visibility.Visible;
                xConfirmButton.Visibility = Visibility.Collapsed;
                xAddTextbox.Text = "Apply";
                xAddButton.Opacity = 0.5;
            }
        }

        private void HideRemaining(int selectedIndex)
        {
            for (int i = 0; i < BehaviorCount; i++)
            {
                if (i == selectedIndex) continue;
                _modifierMapping[i].Visibility = Visibility.Collapsed;
            }
        }

        private void ConfirmTapped(object sender, TappedRoutedEventArgs e)
        {
            xBehaviorsPopup.IsOpen = false;
            MainPage.Instance.XGrid.Children.Remove(this);
            MainPage.Instance.xOverlay.Visibility = Visibility.Collapsed;

            var outOps = new List<OperatorController>();
            foreach (var b in ViewModel.Behaviors)
            {
                outOps.Add(Process(b));
            }
        }

        private OperatorController Process(DocumentBehavior behavior)
        {
            return null;
        }

        private void ExistingBehaviorClicked(object sender, ItemClickEventArgs e)
        {

        }

        private void DeleteBehavior(object sender, RoutedEventArgs e)
        {
            ViewModel.RemoveBehavior((sender as Button)?.DataContext as DocumentBehavior);
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            xAddNewBehaviorPanel.Visibility = Visibility.Collapsed;
            xCancelButton.Visibility = Visibility.Collapsed;
            xConfirmButton.Visibility = Visibility.Visible;
        }

        private void TriggeringEventChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedIndex = xTriggeringEvent.SelectedIndex;
            xAddTextbox.Opacity = xBehavior.SelectedItem != null ? 1 : 0.5;
            _modifierMapping[selectedIndex].Visibility = Visibility.Visible;
            HideRemaining(selectedIndex);
        }

        private void BehaviorChanged(object sender, SelectionChangedEventArgs e)
        {
            xAddTextbox.Opacity = xTriggeringEvent.SelectedItem != null ? 1 : 0.5;
        }
    }
}
