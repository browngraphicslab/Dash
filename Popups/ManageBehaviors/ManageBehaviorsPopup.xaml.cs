using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Popups
{
    public sealed partial class ManageBehaviorsPopup
    {
        private TaskCompletionSource<List<OperatorController>> _tcs;
        private readonly Dictionary<int, ComboBox> _modifierMapping = new Dictionary<int, ComboBox>();
        private readonly ObservableCollection<string> _behaviors = new ObservableCollection<string>();
        private const int BehaviorCount = 3;
        private bool _editMode;
        private DocumentBehavior _editing;
        private string _scriptState;

        public ManageBehaviorsViewModel ViewModel => DataContext as ManageBehaviorsViewModel;

        public ManageBehaviorsPopup()
        {
            InitializeComponent();
            _behaviors.Add("Add new behavior");
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
                    var trigger = ((ComboBoxItem)xTriggeringEvent.SelectedItem).Content?.ToString();
                    var behavior = ((ComboBoxItem)xBehavior.SelectedItem).Content?.ToString();
                    var triggerModifier = _modifierMapping[xTriggeringEvent.SelectedIndex];

                    if (_editMode) ViewModel.Behaviors.Remove(_editing);

                    ViewModel.Behaviors.Add(new DocumentBehavior(trigger, behavior, triggerModifier, xScript.Text, new[]
                    {
                        xTriggeringEvent.SelectedIndex,
                        triggerModifier.SelectedIndex,
                        xBehavior.SelectedIndex,
                        xBehaviorModifiers.SelectedIndex
                    }));

                    xScript.Text = "";
                }
            }
            else
            {
                xTriggeringEvent.SelectedItem = null;
                xBehavior.SelectedItem = null;
                xTappedModifiers.SelectedIndex = 0;
                xDeletedModifiers.SelectedIndex = 0;
                xFieldModifiers.SelectedIndex = 0;

                DisplayAddNewPane();
            }
        }

        private void DisplayAddNewPane()
        {
            xAddNewBehaviorPanel.BorderBrush = new SolidColorBrush(Colors.DarkRed);
            xAddNewBehaviorPanel.Visibility = Visibility.Visible;
            xCancelButton.Visibility = Visibility.Visible;
            xConfirmButton.Visibility = Visibility.Collapsed;
            xAddTextbox.Text = "Apply";
            xAddButton.Visibility = Visibility.Collapsed;
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
            var b = (DocumentBehavior)e.ClickedItem;

            _editMode = true;
            _editing = b;

            xTriggeringEvent.SelectedIndex = b.Indices[0];
            b.TriggerModifier.Visibility = Visibility.Visible;
            b.TriggerModifier.SelectedIndex = b.Indices[1];
            xBehavior.SelectedIndex = b.Indices[2];
            xBehaviorModifiers.SelectedIndex = b.Indices[3];
            xScript.Text = b.Script;

            DisplayAddNewPane();
            xAddButton.Visibility = Visibility.Visible;
        }

        private void DeleteBehavior(object sender, RoutedEventArgs e)
        {
            ViewModel.RemoveBehavior((sender as Button)?.DataContext as DocumentBehavior);
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            if (xScriptAddButton.Visibility == Visibility.Collapsed)
            {
                xAddNewBehaviorPanel.Visibility = Visibility.Collapsed;
                xCancelButton.Visibility = Visibility.Collapsed;
                xConfirmButton.Visibility = Visibility.Visible;
                xAddButton.Visibility = Visibility.Visible;
                xAddTextbox.Text = "Add New";
                xScript.Text = "";
            } else
            {
                xScriptEntry.Visibility = Visibility.Collapsed;
                xAddButton.Visibility = Visibility.Visible;
                xScriptAddButton.Visibility = Visibility.Collapsed;
                xScript.Text = _scriptState;
            }
        }

        private void ProcessScript(object sender, RoutedEventArgs e)
        {
            xAddNewBehaviorPanel.Visibility = Visibility.Visible;
            xCancelButton.Visibility = Visibility.Visible;
            xConfirmButton.Visibility = Visibility.Collapsed;
            xAddButton.Visibility = Visibility.Visible;
            xScriptAddButton.Visibility = Visibility.Collapsed;
            xScriptEntry.Visibility = Visibility.Collapsed;
            xAddTextbox.Text = "Apply";
        }

        private void TriggeringEventChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedIndex = xTriggeringEvent.SelectedIndex;
            if (selectedIndex < 0) return;

            _modifierMapping[selectedIndex].Visibility = Visibility.Visible;
            HideRemaining(selectedIndex);

            if (xBehavior.SelectedItem != null)
            {
                xAddButton.Visibility = Visibility.Visible;
                xAddNewBehaviorPanel.BorderBrush = ColorConverter.HexToBrush("#407BB1");
            }
        }

        private void BehaviorChanged(object sender, SelectionChangedEventArgs e)
        {
            if (xScriptEntry.Visibility == Visibility.Visible) return;

            var selectedIndex = xBehavior.SelectedIndex;
            if (selectedIndex < 0) return;

            if (xTriggeringEvent.SelectedItem != null)
            {
                xAddButton.Visibility = Visibility.Visible;
                xAddNewBehaviorPanel.BorderBrush = ColorConverter.HexToBrush("#407BB1");
            }

            if (selectedIndex == 4)
            {
                ShowScript();
            } 
            else
            {
                xBehaviorModifiers.Visibility = Visibility.Visible;
                xEditScriptPanel.Visibility = Visibility.Collapsed;
                xModifiersText.Visibility = Visibility.Visible;
            }
        }

        private void ShowScript()
        {
            xScript.Focus(FocusState.Programmatic);
            xScript.Focus(FocusState.Keyboard);
            xScript.Focus(FocusState.Pointer);
            xScriptEntry.Visibility = Visibility.Visible;
            xAddButton.Visibility = Visibility.Collapsed;
            xScriptAddButton.Visibility = Visibility.Visible;
            xBehaviorModifiers.Visibility = Visibility.Collapsed;
            xEditScriptPanel.Visibility = Visibility.Visible;
            xModifiersText.Visibility = Visibility.Collapsed;
            _scriptState = xScript.Text;
        }

        private void XTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void EditScript(object sender, TappedRoutedEventArgs e) => ShowScript();
    }
}
