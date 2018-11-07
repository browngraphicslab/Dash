using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Popups
{
    public sealed partial class ManageBehaviorsPopup
    {
        private TaskCompletionSource<List<OperatorController>> _tcs;
        private readonly Dictionary<int, ComboBox> _modifierMapping = new Dictionary<int, ComboBox>();
        private readonly ObservableCollection<string> Behaviors = new ObservableCollection<string>();
        private readonly int BehaviorCount = 3;

        public ManageBehaviorsPopup()
        {
            InitializeComponent();
            Behaviors.Add("Add new behavior");
            SetupComboBoxes();
        }

        private void SetupComboBoxes()
        {
            _modifierMapping[0] = xTappedModifiers;
            _modifierMapping[1] = xDeletedModifiers;
            _modifierMapping[2] = xFieldModifiers;
        }

        public FrameworkElement Self() => this;

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
            return _tcs.Task;
        }

        private void AddOnClick(object sender, RoutedEventArgs e)
        {
            bool shouldExpand = xAddNewBehaviorPanel.Visibility == Visibility.Collapsed;
            xAddNewBehaviorPanel.Visibility = shouldExpand ? Visibility.Visible : Visibility.Collapsed;
            xAddTextbox.Text = shouldExpand ? "Apply" : "Add New";
        }

        private void TriggeringEventChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedIndex = xTriggeringEvent.SelectedIndex;
            _modifierMapping[selectedIndex].Visibility = Visibility.Visible;
            HideRemaining(selectedIndex);
        }

        private void HideRemaining(int selectedIndex)
        {
            for (int i = 0; i < BehaviorCount; i++)
            {
                if (i == selectedIndex) continue;
                _modifierMapping[i].Visibility = Visibility.Collapsed;
            }
        }
    }
}
