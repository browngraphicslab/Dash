using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
        private TaskCompletionSource<List<TextController>> _tcs;
        private readonly Dictionary<int, ComboBox> _modifierMapping = new Dictionary<int, ComboBox>();
        private readonly ObservableCollection<string> _behaviors = new ObservableCollection<string>();
        private const int BehaviorCount = 3;
        private bool _editMode;
        private DocumentBehavior _editing;
        private string _scriptState;
        private string _titleState;
        private ManageBehaviorsViewModel _oldViewModel;

        public ManageBehaviorsViewModel ViewModel => DataContext as ManageBehaviorsViewModel;

        public ManageBehaviorsPopup()
        {
            InitializeComponent();
            _behaviors.Add("Add new behavior");
            SetupComboBoxes();
        }

        private void SetupComboBoxes()
        {
            _modifierMapping[0] = xTappedModifiers;
            _modifierMapping[1] = xDeletedModifiers;
            _modifierMapping[2] = xFieldModifiers;
        }

        public Task<List<TextController>> OpenAsync()
        {
            _tcs = new TaskCompletionSource<List<TextController>>();
            _oldViewModel = ViewModel;
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
                    var title = XTitleBox.Text;

                    if (_editMode) ViewModel.Behaviors.Remove(_editing);

                    if (behavior != null && behavior.Equals("Custom")) behavior += ": " + title;

                    ViewModel.Behaviors.Add(new DocumentBehavior(trigger, behavior, triggerModifier, title, XScript.Text, new[]
                    {
                        xTriggeringEvent.SelectedIndex,
                        triggerModifier.SelectedIndex,
                        xBehavior.SelectedIndex,
                        xBehaviorModifiers.SelectedIndex
                    }));

                    XTitleBox.Text = "";
                    XScript.Text = "";
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

            var outOps = new List<TextController>();
            foreach (var b in ViewModel.Behaviors)
            {
                var script = b.Script;
                if (!b.Trigger.Equals("Tapped") || script == null || script.Equals("")) continue;

                outOps.Add(new TextController($"function(doc) {{\n\t{script}\n}}"));
            }

            //if (_oldViewModel == ViewModel)
            //{
            //    _tcs.SetResult(null);
            //    return;
            //}

            _tcs.SetResult(outOps);
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
            XTitleBox.Text = b.Title;
            XScript.Text = b.Script;

            DisplayAddNewPane();
            xAddButton.Visibility = Visibility.Visible;
        }

        private void DeleteBehavior(object sender, RoutedEventArgs e)
        {
            ViewModel.RemoveBehavior((sender as Button)?.DataContext as DocumentBehavior);
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            if (XScriptEntry.Visibility == Visibility.Collapsed)
            {
                xAddNewBehaviorPanel.Visibility = Visibility.Collapsed;
                xCancelButton.Visibility = Visibility.Collapsed;
                xConfirmButton.Visibility = Visibility.Visible;
                xAddButton.Visibility = Visibility.Visible;
                xAddTextbox.Text = "Add New";
                XScript.Text = "";
            } else
            {
                XScriptEntry.Visibility = Visibility.Collapsed;
                xAddButton.Visibility = XTitleBox.Text.Equals("") || XScript.Text.Equals("") ? Visibility.Collapsed : Visibility.Visible;
                xScriptAddButton.Visibility = Visibility.Collapsed;
                XScript.Text = _scriptState;
                XTitleBox.Text = _titleState;
            }
        }

        private void ProcessScript(object sender, RoutedEventArgs e)
        {
            xAddNewBehaviorPanel.Visibility = Visibility.Visible;
            xCancelButton.Visibility = Visibility.Visible;
            xConfirmButton.Visibility = Visibility.Collapsed;
            xAddButton.Visibility = xTriggeringEvent.SelectedIndex != -1 ? Visibility.Visible : Visibility.Collapsed;
            xScriptAddButton.Visibility = Visibility.Collapsed;
            XScriptEntry.Visibility = Visibility.Collapsed;
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
            if (XScriptEntry.Visibility == Visibility.Visible) return;

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
            XScript.Focus(FocusState.Programmatic);
            XScript.Focus(FocusState.Keyboard);
            XScript.Focus(FocusState.Pointer);
            XScriptEntry.Visibility = Visibility.Visible;
            xAddButton.Visibility = Visibility.Collapsed;
            xScriptAddButton.Visibility = Visibility.Collapsed;
            xBehaviorModifiers.Visibility = Visibility.Collapsed;
            xEditScriptPanel.Visibility = Visibility.Visible;
            xModifiersText.Visibility = Visibility.Collapsed;
            _scriptState = XScript.Text;
            _titleState = XTitleBox.Text;
        }

        private void Script_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (XScriptEntry.Visibility == Visibility.Collapsed) return;
            xScriptAddButton.Visibility = XScript.Text.Equals("") || XTitleBox.Text.Equals("") ? Visibility.Collapsed : Visibility.Visible;
        }

        private void EditScript(object sender, TappedRoutedEventArgs e) => ShowScript();

        private void TitleChanged(object sender, TextChangedEventArgs e)
        {
            if (XScriptEntry.Visibility == Visibility.Collapsed) return;
            xScriptAddButton.Visibility = XScript.Text.Equals("") || XTitleBox.Text.Equals("") ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
