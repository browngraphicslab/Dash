using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI;
using Windows.UI.Xaml.Data;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Popups
{
    public sealed partial class ManageBehaviorsPopup
    {
        private TaskCompletionSource<ListController<DocumentController>> _tcs;
        private readonly Dictionary<int, ComboBox> _modifierMapping = new Dictionary<int, ComboBox>();
        private readonly ObservableCollection<string> _behaviors = new ObservableCollection<string>();
        private const int BehaviorCount = 3;
        private bool _editMode;
        private DocumentController _editing;
        private string _scriptState;
        private string _titleState;

        public ManageBehaviorsViewModel ViewModel => DataContext as ManageBehaviorsViewModel;

        public ManageBehaviorsPopup()
        {
            InitializeComponent();
            HorizontalAlignment = HorizontalAlignment.Center;
            VerticalAlignment = VerticalAlignment.Center;
            Width = 700;
            Height = 500;
            _behaviors.Add("Add new behavior");
            SetupComboBoxes();
        }

        private void SetupComboBoxes()
        {
            _modifierMapping[0] = xTappedModifiers;
            _modifierMapping[1] = xScheduledModifiers;
            _modifierMapping[2] = xFieldModifiers;
        }

        public Task<ListController<DocumentController>> OpenAsync(DocumentController layoutDoc)
        {
            var dataDoc = layoutDoc.GetDataDocument();
            _tcs = new TaskCompletionSource<ListController<DocumentController>>();

            xTriggeringEvent.SelectedIndex = -1;
            xBehavior.SelectedIndex = -1;
            xBehaviorModifiers.SelectedIndex = -1;
            ClearModifiers();

            var behaviors = dataDoc.GetField<ListController<DocumentController>>(KeyStore.DocumentBehaviorsKey);
            if (behaviors != null)
            {
                ViewModel.Behaviors = new ObservableCollection<DocumentController>(behaviors);
            }

            var fields = dataDoc.EnumDisplayableFields().ToList().Select(f => $"d.{f.Key.Name}").ToList();
            fields.AddRange(layoutDoc.EnumDisplayableFields().ToList().Select(f => $"v.{f.Key.Name}"));
            xFieldModifiers.ItemsSource = fields;
            xBehaviorsPopup.IsOpen = true;
            MainPage.Instance.XGrid.Children.Add(this);
            MainPage.Instance.OverlayVisibility(Visibility.Visible);
            return _tcs.Task;
        }

        private void ClearModifiers() => _modifierMapping.ToList().ForEach(kv => kv.Value.SelectedIndex = -1);

        public static bool BehaviorDocsEqual(DocumentController bDocOne, DocumentController bDocTwo)
        {
            if (!bDocOne.GetField<TextController>(KeyStore.ScriptTextKey).Data.Equals(bDocTwo.GetField<TextController>(KeyStore.ScriptTextKey).Data))                                  return false;
            if (!bDocOne.GetField<TextController>(KeyStore.ScriptTitleKey).Data.Equals(bDocTwo.GetField<TextController>(KeyStore.ScriptTitleKey).Data))                                return false;
            if (!bDocOne.GetField<TextController>(KeyStore.TriggerKey).Data.Equals(bDocTwo.GetField<TextController>(KeyStore.TriggerKey).Data))                                        return false;
            if (!bDocOne.GetField<TextController>(KeyStore.DocBehaviorNameKey).Data.Equals(bDocTwo.GetField<TextController>(KeyStore.DocBehaviorNameKey).Data))                        return false;
            if (!(bDocOne.GetField<KeyController>(KeyStore.WatchFieldKey) == bDocTwo.GetField<KeyController>(KeyStore.DocBehaviorNameKey)))                                            return false;
            if (!(bDocOne.GetField<ListController<NumberController>>(KeyStore.BehaviorIndicesKey) == bDocTwo.GetField<ListController<NumberController>>(KeyStore.BehaviorIndicesKey))) return false;

            return true;
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
                    var triggerModifier = _modifierMapping[xTriggeringEvent.SelectedIndex];
                    var keyName = (string)xFieldModifiers.SelectedItem;
                    var ind = triggerModifier.SelectedIndex;

                    switch (trigger)
                    {
                        case "Tapped":
                            Debug.Assert(xTriggeringEvent.SelectedIndex == 0);
                            Debug.Assert(ind < 3 && ind > -1);
                            trigger = (ind == 0 ? "Left " : (ind == 1 ? "Right " : "Double ")) + trigger;
                            break;
                        case "Scheduled":
                            Debug.Assert(xTriggeringEvent.SelectedIndex == 1);
                            trigger = (ind == 0 ? "Low " : (ind == 1 ? "Moderate " : "High ")) + "Priority";
                        break;
                        case "Field Updated":
                            trigger = $"On '{keyName}' Updated";
                            break;
                        default:
                            throw new Exception();
                    }

                    var behavior = ((ComboBoxItem)xBehavior.SelectedItem).Content?.ToString();
                    var title = XTitleBox.Text;

                    if (_editMode) ViewModel.Behaviors.Remove(_editing);
                    _editMode = false;

                    if (behavior != null && behavior.Equals("Custom")) behavior += ": " + title;

                    var behaviorDoc = new DocumentController();

                    behaviorDoc.SetField<TextController>(KeyStore.ScriptTextKey, XScript.Text, true);
                    behaviorDoc.SetField<TextController>(KeyStore.TriggerKey, trigger, true);
                    behaviorDoc.SetField<TextController>(KeyStore.DocBehaviorNameKey, behavior, true);
                    behaviorDoc.SetField<TextController>(KeyStore.ScriptTitleKey, title, true);
                    if (keyName != null) behaviorDoc.SetField(KeyStore.WatchFieldKey, KeyController.Get(keyName), true);
                    behaviorDoc.SetField<ListController<NumberController>>(KeyStore.BehaviorIndicesKey, new[]
                    {
                        xTriggeringEvent.SelectedIndex,
                        triggerModifier.SelectedIndex,
                        xBehavior.SelectedIndex,
                        xBehaviorModifiers.SelectedIndex
                    }.Select(i => new NumberController(i)), true);

                    ViewModel.Behaviors.Add(behaviorDoc);

                    XTitleBox.Text = "";
                    XScript.Text = "";
                }
            }
            else
            {
                xTriggeringEvent.SelectedItem = null;
                xBehavior.SelectedItem = null;
                ClearModifiers();

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
            MainPage.Instance.OverlayVisibility(Visibility.Collapsed);
            _tcs.SetResult(new ListController<DocumentController>(ViewModel.Behaviors.ToList()));
        }

        private void ExistingBehaviorClicked(object sender, ItemClickEventArgs e)
        {
            var bDoc = (DocumentController)e.ClickedItem;
            var indices = bDoc.GetField<ListController<NumberController>>(KeyStore.BehaviorIndicesKey);

            if (indices == null) return;

            _editMode = true;
            _editing = bDoc;

            //Upper left combo box (trigger) selected index   [0]
            //Lower left combo box (modifier) selected Index  [1]
            //Upper right combo box (behavior) selected index [2]
            //Lower left combo box (behavior) displayed       [3]

            var triggerIndex = (int)indices[0].Data;
            xTriggeringEvent.SelectedIndex = triggerIndex;
            var selectedModifierBox = _modifierMapping[triggerIndex];

            selectedModifierBox.Visibility = Visibility.Visible;
            selectedModifierBox.SelectedIndex = (int)indices[1].Data;

            xBehavior.SelectedIndex = (int)indices[2].Data;
            xBehaviorModifiers.SelectedIndex = (int)indices[3].Data;

            XTitleBox.Text = bDoc.GetField<TextController>(KeyStore.ScriptTitleKey).Data;
            XScript.Text = bDoc.GetField<TextController>(KeyStore.ScriptTextKey).Data;

            ProcessScript(sender, e);
            xAddButton.Visibility = Visibility.Visible;
        }

        private void DeleteBehavior(object sender, RoutedEventArgs e)
        {
            ViewModel.RemoveBehavior((sender as Button)?.DataContext as DocumentController);
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
            var valid = xTriggeringEvent.SelectedIndex > -1 && 
                        xBehavior.SelectedIndex > -1 &&
                        _modifierMapping[xTriggeringEvent.SelectedIndex].SelectedIndex > -1;
            xAddButton.Visibility = valid ? Visibility.Visible : Visibility.Collapsed;
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

            if (xBehavior.SelectedItem != null && _modifierMapping[xTriggeringEvent.SelectedIndex].SelectedIndex > -1)
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

            if (xTriggeringEvent.SelectedItem != null && _modifierMapping[xTriggeringEvent.SelectedIndex].SelectedIndex > -1)
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

        private void ModifierChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox modifier && modifier.Visibility == Visibility.Visible && xTriggeringEvent.SelectedIndex > -1 && xBehavior.SelectedIndex > -1)
            {
                xAddButton.Visibility = Visibility.Visible;
                xAddNewBehaviorPanel.BorderBrush = ColorConverter.HexToBrush("#407BB1");
            }
        }

        private void ShowScript()
        {
            xSignatureText.Text = xTriggeringEvent.SelectedIndex == 2 ? "function (layoutDoc, fieldName, updatedValue) {" : "function (layoutDoc) {";
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
            var incompleteOrUnchanged = XScript.Text.Equals("") || 
                                         XTitleBox.Text.Equals("") ||
                                         XScript.Text.Equals(_scriptState) && 
                                         XTitleBox.Text.Equals(_titleState);
            xScriptAddButton.Visibility = incompleteOrUnchanged ? Visibility.Collapsed : Visibility.Visible;
        }

        private void EditScript(object sender, TappedRoutedEventArgs e) => ShowScript();

        private void TitleChanged(object sender, TextChangedEventArgs e)
        {
            if (XScriptEntry.Visibility == Visibility.Collapsed) return;
            var incompleteOrUnchanged = XScript.Text.Equals("") ||
                                         XTitleBox.Text.Equals("") ||
                                         XScript.Text.Equals(_scriptState) &&
                                         XTitleBox.Text.Equals(_titleState);
            xScriptAddButton.Visibility = incompleteOrUnchanged ? Visibility.Collapsed : Visibility.Visible;
        }

        private void TriggerDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (!(args.NewValue is DocumentController bDoc)) return;

            var triggerBinding = new FieldBinding<TextController>()
            {
                Document = bDoc,
                Mode = BindingMode.OneWay,
                Key = KeyStore.TriggerKey
            };
            sender.AddFieldBinding(TextBlock.TextProperty, triggerBinding);
        }

        private void BehaviorDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (!(args.NewValue is DocumentController bDoc)) return;

            var behaviorNameBinding = new FieldBinding<TextController>()
            {
                Document = bDoc,
                Mode = BindingMode.OneWay,
                Key = KeyStore.DocBehaviorNameKey
            };
            sender.AddFieldBinding(TextBlock.TextProperty, behaviorNameBinding);
        }
    }
}
