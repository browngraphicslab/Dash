using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Dash.Annotations;
using Dash.Models.DragModels;
using DashShared;
using Microsoft.Toolkit.Uwp.UI.Extensions;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

// ReSharper disable once CheckNamespace
namespace Dash
{
    public sealed partial class DishReplView : UserControl, INotifyPropertyChanged
    {
        #region Defintions and Intilization  
        private readonly DocumentController _dataDoc;
        private readonly DocumentController _viewDoc;

        private DishReplViewModel ViewModel => DataContext as DishReplViewModel;
        private DSL _dsl;

        private int _currentHistoryIndex;

        private static List<string> _dataset;
        private bool _textModified;

        private string _currentText = "";
        private string _typedText = "";

        private int _textHeight = 50;
        private const int StratOffset = 32;

        private readonly ListController<TextController> _inputList;
        private readonly ListController<FieldControllerBase> _outputList;

        private OuterReplScope _scope;

        private static readonly char[] Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
        private readonly List<char> _takenLetters = new List<char>();
        private readonly List<int> _takenNumbers = new List<int>();

        private int _forIndex = 0;
        private int _forInIndex = 0;

        private bool wayUp;
        private bool wayDown;

        private bool _oneStar;

        private int TextHeight
        {
            get => _textHeight;
            set
            {
                _textHeight = value;
                OnPropertyChanged();
            }
        }

        public DishReplView(DocumentController doc)
        {
            _dataDoc = doc.GetDataDocument();
            _viewDoc = doc;

            InitializeComponent();
            DataContext = new DishReplViewModel();
            NewBlankScopeAndDSL();
            xTextBox.GotFocus += XTextBoxOnGotFocus;
            xTextBox.LostFocus += XTextBoxOnLostFocus;
            var dataset = OperatorScript.GetDataset();
            if (dataset != null) SetDataset(dataset);

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
            _scope = new OuterReplScope(_dataDoc.GetField<DocumentController>(KeyStore.ReplScopeKey));
            _dsl = new DSL(_scope, this);
        }
        #endregion

        #region Helper Functions
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

        public void ScrollToBottom()
        {
            xScrollViewer.UpdateLayout();
            xScrollViewer.ChangeView(0, float.MaxValue, 1);
        }

        public static string StringDiff(string a, string b)
        {
            //a is the longer string
            var aL = a.ToCharArray();
            var bL = b?.ToCharArray();
            for (var i = 0; i < aL.Length; i++)
            {
                if (i < bL?.Length && aL[i] == bL[i]) continue;
             
                return aL[i].ToString();
              
            }

            return a;
        }

        public static bool IsProperLetter(char c) => c != ')' && c != '(' && c != ',' && c != ' ' && c != '}' && c != '{' && c != '\r' && c != '\n';

        private string InsertEnter(string text, char value, bool before = false)
        {
            var chars = text.ToCharArray();
            var resultChars = new List<char>();
            foreach (var ch in chars)
            {
                if (ch == value && before)
                {
                    resultChars.Add('\r');
                    resultChars.Add(ch);
                    resultChars.Add('\r');

                }
                else if (ch == value)
                {
                    resultChars.Add(ch);
                    resultChars.Add('\r');
                }
                else
                {
                    resultChars.Add(ch);
                }
            }

            return new string(resultChars.ToArray());
        }

        private string AddSemicolons(string code)
        {
            var chars = code.ToCharArray();
            var resultChars = new List<char>();
            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] == '\r' && i > 0 && (Char.IsLetterOrDigit(chars[i - 1]) || chars[i-1] == '-' || chars[i-1] == '+'))
                {
                   resultChars.Add(';');
                   resultChars.Add(chars[i]);
                } else if (i == chars.Length - 1 && i > 0 &&
                           (Char.IsLetterOrDigit(chars[i - 1]) || chars[i - 1] == '-' || chars[i - 1] == '+'))
                {
                    resultChars.Add(chars[i]);
                    resultChars.Add(';');
                }
                else
                {
                    resultChars.Add(chars[i]);
                }
            }

            return new string(resultChars.ToArray());

        }
        #endregion

        #region Toolbar

        private void XScript_OnClick(object sender, RoutedEventArgs e)
        {
            var collection = this.GetFirstAncestorOfType<CollectionView>()?.ViewModel;
            if (collection == null) return;
            //open DishScriptEditView with repl text
            string allCode = "";
            foreach (var line in _inputList)
            {
                var result1 = InsertEnter(line.Data, '{');
                var result2 = InsertEnter(result1, '}', true);

                allCode += result2 + "\r";
            }

            var pt = _viewDoc.GetPositionField().Data;
            var width = _viewDoc.GetWidthField().Data;
            var height = _viewDoc.GetHeightField().Data;

            var note = new DishScriptBox(pt.X + width + 15, pt.Y, width, height, allCode);
                
            Actions.DisplayDocument(collection, note.Document);
        }
        #endregion

        #region On Type Actions
        private void CoreWindowOnKeyUp(CoreWindow sender, KeyEventArgs args)
        {
            var numItem = ViewModel.Items.Count;
            switch (args.VirtualKey)
            {
                case VirtualKey.Up when !MainPage.Instance.IsCtrlPressed() && !MainPage.Instance.IsShiftPressed() && wayUp:
                    //get last terminal input entered
                    var index1 = numItem - (_currentHistoryIndex + 1);
                    if (index1 + 1 == numItem)
                    {
                        _typedText = _currentText;
                    }
                    if (numItem > index1 && index1 >= 0)
                    {
                        _currentHistoryIndex++;
                        _textModified = true;
                        xTextBox.Text = ViewModel.Items.ElementAt(index1)?.LineText?.Substring(3) ?? xTextBox.Text;
                         MoveCursorToEnd();
                    }

                    TextHeight = 50;
                    TextGrid.Height = new GridLength(50);
                    break;
                case VirtualKey.Up when MainPage.Instance.IsCtrlPressed():
                    if (xSuggestions.SelectedIndex > -1 && xSuggestionsPopup.Visibility == Visibility.Visible) xSuggestions.SelectedIndex--;
                    break;
                case VirtualKey.Down when !MainPage.Instance.IsCtrlPressed() && !MainPage.Instance.IsShiftPressed() && wayDown:
                    var index = numItem - (_currentHistoryIndex - 1);
                    if (numItem > index && index >= 0)
                    {
                        _currentHistoryIndex--;
                        _textModified = true;
                        xTextBox.Text = ViewModel.Items.ElementAt(index)?.LineText?.Substring(3) ?? xTextBox.Text;
                        MoveCursorToEnd();
                    }
                    else if (index == numItem)
                    {
                        _currentHistoryIndex--;
                        _textModified = true;
                        xTextBox.Text = _typedText;
                        MoveCursorToEnd();
                    }
                    var numEnter = xTextBox.Text.Split('\r').Length - 1;
                    var newTextSize = 50 + (numEnter * 20);
                    TextHeight = newTextSize;
                    TextGrid.Height = new GridLength(newTextSize);
                    break;
                case VirtualKey.Down when MainPage.Instance.IsCtrlPressed():
                    if (xSuggestions.SelectedIndex + 1 < xSuggestions.Items?.Count && xSuggestionsPopup.Visibility == Visibility.Visible) xSuggestions.SelectedIndex++;
                    break;
                case VirtualKey.Right:
                    if (xSuggestions.SelectedIndex > -1) SelectPopup(xSuggestions.SelectedItem?.ToString());
                    xSuggestions.SelectedIndex = -1;
                    break;
                case VirtualKey.Delete when MainPage.Instance.IsCtrlPressed():
                    xTextBox.Text = "";
                    break;
            }
            
            var beforeCursor = xTextBox.Text.Substring(0, xTextBox.SelectionStart);
            wayUp = !(beforeCursor.Contains('\r'));
            var afterCursor = xTextBox.Text.Substring(xTextBox.SelectionStart, xTextBox.Text.Length - xTextBox.SelectionStart);
            wayDown = !(afterCursor.Contains('\r'));

            _currentText = xTextBox.Text;
        }


        private void XTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_textModified)
            {
                _currentHistoryIndex = 0;

                //get most recent char typed
                var textDiff = StringDiff(xTextBox.Text, _currentText);


                if (xTextBox.Text.Equals(""))
                {
                    TextHeight = 50;
                    TextGrid.Height = new GridLength(TextHeight);
                    _takenNumbers.Clear();
                    _takenLetters.Clear();
                }

                var place1 = xTextBox.SelectionStart;
                var stringLength = 0;
                var selectOffset = 0;
                var newText = "";
                var selectLength = 0;
                if (xTextBox.Text.TrimStart().Length >= "for ".Length && xTextBox.Text.Substring(xTextBox.Text.Length - "for ".Length).Equals("for "))
                {
                    while (_scope.GetVariable(Alphabet[_forIndex].ToString()) != null || _takenLetters.Contains(Alphabet[_forIndex])) { _forIndex++; }

                    stringLength = 4;
                    
                    var ct = Alphabet[_forIndex];
                    _takenLetters.Add(ct);
                    newText = xTextBox.Text + $"(var {ct} = 0; {ct} < UPPER; {ct}++)" + " {\r      \r}";
                    selectOffset =  16; //36 to get to body
                    selectLength = 5;
                }
                else if (xTextBox.Text.TrimStart().Length >= "forin ".Length && xTextBox.Text.Substring(xTextBox.Text.Length - "forin ".Length).Equals("forin "))
                {
                    stringLength = 6;
                    
                    var varExp = (_scope.GetVariable("item") != null) ? "" : "var ";
                    newText = xTextBox.Text.Substring(0, xTextBox.Text.Length - "forin ".Length) + $"for ({varExp}item in [])" + " {\r      item\r}";
                    selectOffset=  12;
                    
                }
                else if (xTextBox.Text.TrimStart().Length >= "forin? ".Length && xTextBox.Text.Substring(xTextBox.Text.Length - "forin? ".Length).Equals("forin? "))
                {
                    stringLength = 7;
                    var varExp = (_scope.GetVariable("res") != null) ? "" : "var ";
                    newText = xTextBox.Text.Substring(0, xTextBox.Text.Length - "forin? ".Length) + $"for ({varExp}res in f(\":\"))" + " {\r      data_doc(res). = \r}";
                    selectOffset = 12;
                    selectLength = 0;
                }
                else if (xTextBox.Text.TrimStart().Length >= "forin+ ".Length && xTextBox.Text.Substring(xTextBox.Text.Length - "forin+ ".Length).Equals("forin+ "))
                {
                    var ret = xTextBox.Text.TrimStart().Length == 7 ? "" : "\r";
                    while (_scope.GetVariable("var myList" + _forInIndex) != null || _takenNumbers.Contains(_forInIndex)) { _forInIndex++; }

                    var newList = "myList" + _forInIndex;
                    _takenNumbers.Add(_forInIndex);
                    xTextBox.Text = xTextBox.Text.Insert(place1 - 7, $"{ret}var {newList} = []\r");
                    TextHeight += StratOffset * 2;
                    var offset = _forInIndex.ToString().Length + ret.Length - 1;

                    xTextBox.Text = xTextBox.Text.Substring(0, xTextBox.Text.Length - "forin+ ".Length) + $"for (var item in {newList})" + " {\r      item\r}";
                    xTextBox.SelectionStart = place1 + 8 + offset;
                    TextHeight += 40;
                    TextGrid.Height = new GridLength(TextHeight);
                }
                else if (xTextBox.Text.TrimStart().Length >= "dowhile ".Length && xTextBox.Text.Substring(xTextBox.Text.Length - "dowhile ".Length).Equals("dowhile "))
                {
                    stringLength = 8;
                    newText = xTextBox.Text + "(condition) {\r      \r}";
                    selectOffset =  1;
                    selectLength = 9;
                }
                else if (xTextBox.Text.TrimStart().Length >= "while ".Length && xTextBox.Text.Substring(xTextBox.Text.Length - "while ".Length).Equals("while "))
                {
                    stringLength = 6;
                    newText = xTextBox.Text + "(condition) {\r      \r}";
                    selectOffset =  1;
                    selectLength = 9;
                }
                else if (xTextBox.Text.TrimStart().Length >= "if ".Length && xTextBox.Text.Substring(xTextBox.Text.Length - "if ".Length).Equals("if "))
                {
                    stringLength = 3;
                    newText = xTextBox.Text + "(condition) {\r      \r}";
                    selectOffset = 1;
                    selectLength = 9;
                }

                if (stringLength != 0)
                {
                    if (xTextBox.Text.TrimStart().Length != stringLength)
                    {
                        xTextBox.Text = xTextBox.Text.Insert(place1 - stringLength, "\r");
                        TextHeight += StratOffset;
                        place1++;
                    }

                    xTextBox.Text = newText;
                    xTextBox.SelectionStart = place1 + selectOffset;
                    xTextBox.SelectionLength = selectLength;
                    TextHeight += 40;
                    TextGrid.Height = new GridLength(TextHeight);
                }

                
                switch (textDiff)
                {
                    case "\r" when xTextBox.Text.Length > _currentText.Length:
                        if (Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down))
                        {
                            //if enter is pressed with shift, make text box larger
                            TextHeight += 20;
                            TextGrid.Height = new GridLength(TextHeight);
                        }
                        else
                        {
                            //get rid of enter
                            xTextBox.Text = _currentText;

                            //enter pressed without key modifiers - send code to terminal
                            _takenLetters.Clear();
                            _takenNumbers.Clear();
                            //_scope.SetVariable(Alphabet[_forIndex].ToString(), null);

                            //put textbox size back to default
                            TextHeight = 50;
                            TextGrid.Height = new GridLength(50);

                            _currentHistoryIndex = 0;
                            
                            
                            FieldControllerBase returnValue;
                            try
                            {
                                returnValue = _dsl.Run(xTextBox.Text, true);
                            }
                            catch (Exception ex)
                            {
                                returnValue = new TextController("There was an error: " + ex.StackTrace);
                            }

                            if (returnValue == null) returnValue = new TextController($" Exception:\n            InvalidInput\n      Feedback:\n            Input yielded an invalid return. Enter <help()> for a complete catalog of valid functions.");

                            //get text replacing newlines with spaces

                            var currentText = AddSemicolons(xTextBox.Text).Replace('\r', ' ');

                            xTextBox.Text = ""; xTextBox.Text = "";
                            ViewModel.Items.Add(new ReplLineViewModel(currentText, returnValue, new TextController("test")));

                            //save input and output data
                            _inputList.Add(new TextController(currentText));
                            _outputList.Add(returnValue);

                            ScrollToBottom();
                        }

                        break;
                    case "\"" when xTextBox.Text.Length > _currentText.Length:
                        var place = xTextBox.SelectionStart;
                        var offset = 0;

                        while (place + offset < xTextBox.Text.Length && IsProperLetter(xTextBox.Text[place + offset])) { offset++; }
                        place += offset;

                        xTextBox.Text = xTextBox.Text.Insert(place, "\"");
                        xTextBox.SelectionStart = place;
                        break;
                    case "(" when xTextBox.Text.Length > _currentText.Length:
                        place = xTextBox.SelectionStart;
                        offset = 0;

                        while (place + offset < xTextBox.Text.Length && IsProperLetter(xTextBox.Text[place + offset])) { offset++; }
                        place += offset;

                        xTextBox.Text = xTextBox.Text.Insert(place, ")");
                        xTextBox.SelectionStart = place;
                        break;
                    case "\'" when xTextBox.Text.Length > _currentText.Length:
                        place = xTextBox.SelectionStart;
                        offset = 0;

                        while (place + offset < xTextBox.Text.Length && IsProperLetter(xTextBox.Text[place + offset])) { offset++; }
                        place += offset;

                        xTextBox.Text = xTextBox.Text.Insert(place, "\'");
                        xTextBox.SelectionStart = place;
                        break;
                    case "{" when xTextBox.Text.Length > _currentText.Length:
                        place = xTextBox.SelectionStart;
                        xTextBox.Text = xTextBox.Text.Insert(place, "\r      \r}");
                        xTextBox.SelectionStart = place + 7;
                        TextHeight += 40;
                        TextGrid.Height = new GridLength(TextHeight);
                        break;
                    case "*":
                        if (_oneStar)
                        {
                            place = xTextBox.SelectionStart;
                            xTextBox.Text += "      */";
                            xTextBox.SelectionStart = place + 1;
                            _oneStar = false;
                        }
                        else
                        {
                            _oneStar = true;
                        }
                        break;
                    default:
                        _oneStar = false;
                        if (xTextBox.Text != "")
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

                            var numSug = suggestions?.Count;
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

                        break;
                }
            }

            _currentText = xTextBox.Text;
            _textModified = false;
        }
        #endregion

        #region Sugestions 
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
            var funcName = Op.Parse(selectedItem);
            var isOverloaded = OperatorScript.IsOverloaded(funcName);
            var inputTypes = OperatorScript.GetDefaultInputTypeListFor(funcName);
            var numInputs = inputTypes.Count;

            var functionEnding = " ";
            var offset = 1;
            if (numInputs > 0)
            {
                functionEnding = "(";
                if (inputTypes[0] == TypeInfo.Text) offset++;

                for (var i = 0; i < numInputs; i++)
                {
                    var symbol = "_";
                    if (!isOverloaded)
                    {
                        switch (inputTypes[i])
                        {
                            case TypeInfo.Text:
                                symbol = "\"\"";
                                break;
                            case TypeInfo.Number:
                                symbol = "#";
                                break;
                        }
                    }
                    functionEnding = functionEnding + $"{symbol}, ";
                }
                //delete last comma and space and add ending paranthesis
                functionEnding = functionEnding.Substring(0, functionEnding.Length - 2) + ")";
            }
            else
            {
                functionEnding = "()";
            }

            xTextBox.Text = keepText + selectedItem + functionEnding;
            xTextBox.Focus(FocusState.Pointer);
            MoveCursorToEnd((keepText + selectedItem).Length + offset);

            xSuggestionsPopup.IsOpen = false;
            xSuggestionsPopup.Visibility = Visibility.Collapsed;
        }
        #endregion

 

    }
}
