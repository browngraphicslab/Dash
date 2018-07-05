using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Dash.Annotations;
using Dash.Models.DragModels;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

// ReSharper disable once CheckNamespace
namespace Dash
{
    public sealed partial class DishReplView : UserControl, INotifyPropertyChanged
    {
        private readonly DocumentController _dataDoc;
        private DishReplViewModel ViewModel => DataContext as DishReplViewModel;
        private DSL _dsl;

        private int _currentHistoryIndex;

        private static List<string> _dataset;
        private bool _textModified;

        private string _currentText = "";
        private string _typedText = "";

        private int _textHeight = 50;

        private readonly ListController<TextController> _inputList;
        private readonly ListController<FieldControllerBase> _outputList;

        private int TextHeight
        {
            get => _textHeight;
            set
            {
                _textHeight = value;
                OnPropertyChanged();
            }
        }

        public DishReplView(DocumentController dataDoc)
        {
            _dataDoc = dataDoc;
            InitializeComponent();
            DataContext = new DishReplViewModel();
            NewBlankScopeAndDSL();
            xTextBox.GotFocus += XTextBoxOnGotFocus;
            xTextBox.LostFocus += XTextBoxOnLostFocus;

            //intialize lists to save data
            _inputList =_dataDoc.GetField<ListController<TextController>>(KeyStore.ReplInputsKey);
            _outputList = _dataDoc.GetField<ListController<FieldControllerBase>>(KeyStore.ReplOutputsKey);
            //var scopeDoc = dataDoc.GetField<DocumentController>(KeyStore.ReplScopeKey);
            //add items from lists to Repl
            var replItems = new ObservableCollection<ReplLineViewModel>();
            for(var i = 0; i < _inputList.Count; i++)
            {
                var newReplLine = new ReplLineViewModel(_inputList[i].Data, _outputList[i], new TextController("test"));
                replItems.Add(newReplLine);
            }

            ViewModel.Items = replItems;
            ScrollToBottom();
        }

        // ReSharper disable once InconsistentNaming
        private void NewBlankScopeAndDSL()
        {
            var scope = new OuterReplScope(_dataDoc.GetField<DocumentController>(KeyStore.ReplScopeKey));
            _dsl = new DSL(scope, this);
        }

        public void Clear(bool clearData)
        {
            ViewModel.Items.Clear();
            if (!clearData) return;
            _dataDoc.SetField(KeyStore.ReplScopeKey, new DocumentController(), true);
            NewBlankScopeAndDSL();
            _inputList?.Clear();
            _outputList?.Clear();
        }

        public static void SetDataset(List<string> data) => _dataset = data;

        public static void NewVariable(string var) => _dataset.Add(var);

        private void XTextBoxOnLostFocus(object sender, RoutedEventArgs routedEventArgs)
        {
            Window.Current.CoreWindow.KeyUp -= CoreWindowOnKeyUp;
        }

        private void XTextBoxOnGotFocus(object sender, RoutedEventArgs routedEventArgs)
        {
            Window.Current.CoreWindow.KeyUp += CoreWindowOnKeyUp;
        }

        private void MoveCursorToEnd(int? end = null)
        {
            if (xTextBox.Text.Length == 0) return;

            xTextBox.SelectionStart = end ?? xTextBox.Text.Length;
            xTextBox.SelectionLength = 0;
        }

        private void CoreWindowOnKeyUp(CoreWindow sender, KeyEventArgs args)
        {
            var numItem = ViewModel.Items.Count;
            switch (args.VirtualKey)
            {
                case VirtualKey.Up when !MainPage.Instance.IsCtrlPressed():
                    var index1 = numItem - (_currentHistoryIndex + 1);
                    if (index1 + 1 == numItem)
                    {
                        _typedText = _currentText;
                    }
                    if (numItem > index1 && index1 >= 0)
                    {
                        _currentHistoryIndex++;
                        xTextBox.Text = ViewModel.Items.ElementAt(index1)?.LineText?.Substring(3) ?? xTextBox.Text;
                         MoveCursorToEnd();
                    }

                    TextHeight = 50;
                    TextGrid.Height = new GridLength(50);
                    break;
                case VirtualKey.Up when MainPage.Instance.IsCtrlPressed():
                    if (xSuggestions.SelectedIndex > -1) xSuggestions.SelectedIndex--;
                    break;
                case VirtualKey.Down when !MainPage.Instance.IsCtrlPressed():
                    var index = numItem - (_currentHistoryIndex - 1);
                    if (numItem > index && index >= 0)
                    {
                        _currentHistoryIndex--;
                        xTextBox.Text = ViewModel.Items.ElementAt(index)?.LineText?.Substring(3) ?? xTextBox.Text;
                        MoveCursorToEnd();
                    }
                    else if (index == numItem)
                    {
                        _currentHistoryIndex--;
                        xTextBox.Text = _typedText;
                        MoveCursorToEnd();
                    }
                    var numEnter = xTextBox.Text.Split('\r').Length - 1;
                    var newTextSize = 50 + (numEnter * 20);
                    TextHeight = newTextSize;
                    TextGrid.Height = new GridLength(newTextSize);
                    break;
                case VirtualKey.Down when MainPage.Instance.IsCtrlPressed():
                    if (xSuggestions.SelectedIndex + 1 < xSuggestions.Items?.Count) xSuggestions.SelectedIndex++;
                    break;
                case VirtualKey.Right:
                    if (xSuggestions.SelectedIndex > -1) SelectPopup(xSuggestions.SelectedItem?.ToString());
                    xSuggestions.SelectedIndex = -1;
                    break;
            }
            _currentText = xTextBox.Text;
        }

        private static string StringDiff(string a, string b, bool remove = false)
        {
            //a is the longer string
            var aL = a.ToCharArray();
            var bL = b.ToCharArray();
            for(var i = 0; i < aL.Length; i++)
            {
                if (i < bL.Length && aL[i] == bL[i]) continue;
                //remove last character if it was enter
                if (remove && aL[i] == '\r')
                {
                    //remove new character
                    var aL2 = aL.ToList();
                    aL2.RemoveAt(i);
                    return new string(aL2.ToArray());
                }

                if (!remove)
                {
                    return aL[i].ToString();
                }
            }

            return a;
        }

        public void ScrollToBottom()
        {
            xScrollViewer.UpdateLayout();
            xScrollViewer.ChangeView(0, xScrollViewer.ScrollableHeight, 1);
        }

        private void XTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            //get most recent char typed

            if (!_textModified)
            {
                var addedText = ' ';

                var textDiff = StringDiff(xTextBox.Text, _currentText);

                if (textDiff == "\r")
                {
                    if (Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down))
                    {
                        //if enter is pressed with shift, make text box larger
                        TextHeight = TextHeight + 20;
                        TextGrid.Height = new GridLength(TextHeight);
                    }
                    else
                    {
                        //enter pressed without key modifiers - send code to terminal

                        //put textbox size back to default
                        TextHeight = 50;
                        TextGrid.Height = new GridLength(50);

                        _currentHistoryIndex = 0;
                        //get text replacing newlines with spaces
                        var currentText = StringDiff(xTextBox.Text, _currentText, true).Replace('\r', ' ');
                        xTextBox.Text = "";
                        FieldControllerBase returnValue;
                        try
                        {
                            returnValue = _dsl.Run(currentText, true);
                        }
                        catch (Exception ex)
                        {
                            returnValue = new TextController("There was an error: " + ex.StackTrace);
                        }

                        ViewModel.Items.Add(new ReplLineViewModel(currentText, returnValue, new TextController("test")));

                        //save input and output data
                        if (returnValue != null)
                        {
                            _inputList.Add(new TextController(currentText));
                            _outputList.Add(returnValue);
                        }

                        ScrollToBottom();
                    }
                }

                else if (xTextBox.Text != "")
                {
                    //only give suggestions on last word
                    var allText = xTextBox.Text.Replace('\r', ' ').Split(' ');
                    var lastWord = "";
                    if (allText.Length > 0)
                    {
                        lastWord = allText[allText.Length - 1];
                    }

                    if (_dataset == null)
                    {
                        OperatorScript.Instance.Init();
                    }

                    var suggestions = _dataset?.Where(x => x.StartsWith(lastWord)).ToList();

                    xSuggestions.ItemsSource = suggestions;

                    var numSug = suggestions.Count;

                    if (numSug > 0)
                    {
                        xSuggestionsPopup.IsOpen = true;
                        xSuggestionsPopup.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        xSuggestionsPopup.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    xSuggestionsPopup.IsOpen = false;
                    xSuggestionsPopup.Visibility = Visibility.Collapsed;
                }

                
            }

            _currentText = xTextBox.Text;
            _textModified = false;
            
        }

        private void Suggestions_OnItemClick(object sender, ItemClickEventArgs e)
        {
            //get selected item
            var selectedItem = e.ClickedItem.ToString();
            SelectPopup(selectedItem);
        }

        private void SelectPopup(string selectedItem)
        {
            _textModified = true;

            //only change last word to new text
            var currentText = xTextBox.Text.Replace('\r', ' ').Split(' ');
            var keepText = "";
            if (currentText.Length > 1)
            {
                var lastWordLength = currentText[currentText.Length - 1].Length;
                keepText = xTextBox.Text.Substring(0, xTextBox.Text.Length - lastWordLength);
            }

            //if it is function, set up sample inputs
            var numInputs = OperatorScript.GetAmountInputs(Op.Parse(selectedItem));
            var functionEnding = " ";
            var offset = 1;
            if (numInputs != null && numInputs != 0)
            {
                offset = 2;
                if (selectedItem != "help")
                {
                    functionEnding = "(";
                    for (var i = 0; i < numInputs; i++)
                    {
                        functionEnding = functionEnding + "_, ";
                    }
                    //delete last comma and space and add ending paranthesis
                    functionEnding = functionEnding.Substring(0, functionEnding.Length - 2) + ")";
                }
                else
                {
                    functionEnding = "(\"\")";
                }
            }

            xTextBox.Text = keepText + selectedItem + functionEnding;
            xTextBox.Focus(FocusState.Pointer);
            MoveCursorToEnd((keepText + selectedItem).Length + offset);

            xSuggestionsPopup.IsOpen = false;
            xSuggestionsPopup.Visibility = Visibility.Collapsed;
        }

        private void UIElement_OnDragStarting(UIElement sender, DragStartingEventArgs args)
        {
            var output = (sender as FrameworkElement).DataContext as ReplLineViewModel;
            var outputData = output.Value;
            var dataBox = new DataBox(outputData).Document;
            args.Data.Properties[nameof(DragDocumentModel)] = new DragDocumentModel(dataBox, true);
            args.AllowedOperations = DataPackageOperation.Link | DataPackageOperation.Move | DataPackageOperation.Copy;
            args.Data.RequestedOperation =
                DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
