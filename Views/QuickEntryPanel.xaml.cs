using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using DashShared;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views
{
    public sealed partial class QuickEntryPanel : UserControl
    {
        private string _lastValueInput;
        private bool   _articialChange;
        private bool   _clearByClose;
        private string _mostRecentPrefix;
        private bool   _isQuickEntryOpen;
        private bool   _animationBusy;
        public QuickEntryPanel()
        {
            InitializeComponent();
            Window.Current.CoreWindow.KeyDown += (sender, args) =>
            {
                if (SelectionManager.SelectedDocViewModels.Any())
                {
                    if (MainPage.Instance.IsShiftPressed() && args.VirtualKey == VirtualKey.PageDown && !_isQuickEntryOpen || args.VirtualKey == VirtualKey.PageUp && _isQuickEntryOpen)
                    {
                        if (!_isQuickEntryOpen)
                        {
                            _clearByClose = true;
                            ClearQuickEntryBoxes();
                            xKeyBox.Focus(FocusState.Keyboard);
                        }

                        ToggleQuickEntry();
                        args.Handled = true;
                    }
                    else if (MainPage.Instance.IsShiftPressed() && args.VirtualKey == VirtualKey.PageDown && _isQuickEntryOpen)
                    {
                        if (xKeyBox.FocusState != FocusState.Unfocused)
                        {
                            _articialChange = true;
                            int pos = xKeyBox.SelectionStart;
                            if (xKeyBox.Text.ToLower().StartsWith("v")) xKeyBox.Text = "d" + xKeyBox.Text.Substring(1);
                            else if (xKeyBox.Text.ToLower().StartsWith("d")) xKeyBox.Text = "v" + xKeyBox.Text.Substring(1);
                            xKeyBox.SelectionStart = pos;
                        }
                        args.Handled = true;
                    }
                }
            };


            xKeyBox.AddKeyHandler(VirtualKey.Enter, KeyBoxOnEnter);
            xValueBox.AddKeyHandler(VirtualKey.Enter, ValueBoxOnEnter);

            _lastValueInput = "";

            xQuickEntryIn.Completed += (sender, o) =>
            {
                xKeyBox.Text = "d.";
                xKeyBox.SelectionStart = 2;
            };

            xKeyEditSuccess.Completed += SetFocusToKeyBox;
            xValueErrorFailure.Completed += SetFocusToKeyBox;

            xKeyBox.TextChanged += XKeyBoxOnTextChanged;
            xKeyBox.BeforeTextChanging += XKeyBoxOnBeforeTextChanging;
            xValueBox.TextChanged += XValueBoxOnTextChanged;

            xValueBox.GotFocus += XValueBoxOnGotFocus;

            LostFocus += (sender, args) =>
            {
                if (_isQuickEntryOpen && xKeyBox.FocusState == FocusState.Unfocused &&
                    xValueBox.FocusState == FocusState.Unfocused) ToggleQuickEntry();

                MainPage.Instance.xPresentationView.ClearHighlightedMatch();
            };
        }
        private void XValueBoxOnTextChanged(object sender1, TextChangedEventArgs e)
        {
            if (_articialChange)
            {
                _articialChange = false;
                return;
            }
            _lastValueInput = xValueBox.Text.Trim();
        }

        private void XKeyBoxOnTextChanged(object sender1, TextChangedEventArgs textChangedEventArgs)
        {
            var split = xKeyBox.Text.Split(".", StringSplitOptions.RemoveEmptyEntries);
            if (split == null || split.Length != 2) return;

            string docSpec = split[0];

            if (!(docSpec.Equals("d") || docSpec.Equals("v"))) return;

            foreach (var doc in SelectionManager.SelectedDocViewModels)
            {
                DocumentController target = docSpec.Equals("d") ? doc.DataDocument : doc.LayoutDocument;
                string keyInput = split[1].Replace("_", " ");

                var val = target.GetDereferencedField(KeyController.Get(keyInput), null);
                if (val == null)
                {
                    xValueBox.SelectionLength = 0;
                    xValueBox.Text = "";
                    return;
                }

                _articialChange = true;
                xValueBox.Text = val.GetValue().ToString();

                if (double.TryParse(xValueBox.Text.Trim(), out double res))
                {
                    xValueBox.Text = "=" + xValueBox.Text;
                    xValueBox.SelectionStart = 1;
                    xValueBox.SelectionLength = xValueBox.Text.Length - 1;
                }
                else
                {
                    xValueBox.SelectAll();
                }
            }
        }

        private void XValueBoxOnGotFocus(object sender1, RoutedEventArgs routedEventArgs)
        {
            if (xValueBox.Text.StartsWith("="))
            {
                xValueBox.SelectionStart = 1;
                xValueBox.SelectionLength = xValueBox.Text.Length - 1;
            }
            else
            {
                xValueBox.SelectAll();
            }
        }

        private async Task ProcessInput()
        {
            string rawKeyText = xKeyBox.Text;
            string rawValueText = xValueBox.Text;

            var emptyKeyFailure = false;
            var emptyValueFailure = false;

            if (string.IsNullOrEmpty(rawKeyText))
            {
                xKeyEditFailure.Begin();
                emptyKeyFailure = true;
            }
            if (string.IsNullOrEmpty(rawValueText))
            {
                xValueEditFailure.Begin();
                emptyValueFailure = true;
            }

            if (emptyKeyFailure || emptyValueFailure) return;

            var components = rawKeyText.Split(".", StringSplitOptions.RemoveEmptyEntries);
            string docSpec = components[0].ToLower();

            if (components.Length != 2 || !(docSpec.Equals("v") || docSpec.Equals("d")))
            {
                xKeyEditFailure.Begin();
                return;
            }

            FieldControllerBase computedValue = await DSL.InterpretUserInput(rawValueText, true);
            foreach (var d in SelectionManager.SelectedDocViewModels)
            {
                DocumentController target = docSpec.Equals("d") ? d.DataDocument : d.LayoutDocument;
                if (computedValue is DocumentController doc && doc.DocumentType.Equals(DashConstants.TypeStore.ErrorType))
                {
                    computedValue = new TextController(xValueBox.Text.Trim());
                    xValueErrorFailure.Begin();
                }

                string key = components[1].Replace("_", " ");

                target.SetField(KeyController.Get(key), computedValue, true);
            }

            _mostRecentPrefix = xKeyBox.Text.Substring(0, 2);
            xKeyEditSuccess.Begin();
            xValueEditSuccess.Begin();

            ClearQuickEntryBoxes();
        }

        private void SetFocusToKeyBox(object sender1, object o2)
        {
            xKeyBox.Text = _mostRecentPrefix;
            xKeyBox.SelectionStart = 2;
            xKeyBox.Focus(FocusState.Keyboard);
        }

        private async void KeyBoxOnEnter(KeyRoutedEventArgs obj)
        {
            obj.Handled = true;
            await ProcessInput();
        }

        private async void ValueBoxOnEnter(KeyRoutedEventArgs obj)
        {
            obj.Handled = true;
            using (UndoManager.GetBatchHandle())
            {
                await ProcessInput();
            }

        }

        private void XKeyBoxOnBeforeTextChanging(TextBox textBox, TextBoxBeforeTextChangingEventArgs e)
        {
            if (!_clearByClose && e.NewText.Length <= xKeyBox.Text.Length)
            {
                if (xKeyBox.Text.Length <= 2 && !(e.NewText.StartsWith("d.") || e.NewText.StartsWith("v.")))
                {
                    e.Cancel = true;
                }
                else
                {
                    if (string.IsNullOrEmpty(e.NewText))
                    {
                        xKeyBox.Text = xKeyBox.Text.Substring(0, 2);
                        xKeyBox.SelectionStart = 2;
                        xKeyBox.Focus(FocusState.Keyboard);
                    }
                }
            }
            else
            {
                if (!(e.NewText.StartsWith("d.") || e.NewText.StartsWith("v."))) e.Cancel = true;
            }
            _clearByClose = false;
        }

        private void ClearQuickEntryBoxes()
        {
            _lastValueInput = "";
            xKeyBox.Text = "";
            xValueBox.Text = "";
        }

        private void ToggleQuickEntry()
        {
            var allTopLevel = true;
            foreach (var doc in SelectionManager.SelectedDocViewModels)
            {
                if (!SplitManager.IsRoot(doc))
                {
                    allTopLevel = false;
                }
            }
            if (_animationBusy || allTopLevel || Equals(MainPage.Instance.xMapDocumentView)) return;

            _isQuickEntryOpen = !_isQuickEntryOpen;
            Storyboard animation = _isQuickEntryOpen ? xQuickEntryIn : xQuickEntryOut;

            if (animation == xQuickEntryIn) xKeyValueBorder.Width = double.NaN;

            _animationBusy = true;
            //_selectedDocs.ForEach(d =>
            //{
            //    if (_isQuickEntryOpen)
            //    {
            //        //d.Margin = new Thickness(0, 60, 0, 0);
            //        d.xQuickEntryIn.Begin();
            //    }
            //    else
            //    {
            //        //d.Margin = new Thickness(0);
            //        d.xQuickEntryOut.Begin();
            //    }
            //});
            animation.Begin();
            animation.Completed += AnimationCompleted;

            void AnimationCompleted(object sender, object e)
            {
                animation.Completed -= AnimationCompleted;
                if (animation == xQuickEntryOut)
                {
                    xKeyValueBorder.Width = 0;
                    Focus(FocusState.Programmatic);
                }
                else
                {
                    xKeyBox.Focus(FocusState.Programmatic);
                }

                _animationBusy = false;
            }
        }
    }
}
