using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

// ReSharper disable once CheckNamespace
namespace Dash
{
    public sealed partial class DishReplView : UserControl
    {
        #region Defintions and Intilization  
        private readonly DocumentController _dataDoc;
        private readonly DocumentController _viewDoc;

        private DishReplViewModel ViewModel => DataContext as DishReplViewModel;
        private DSL _dsl;

        private int _currentHistoryIndex;

        //TODO Make this be a list of some class that knows if its a function, name, etc so that SelectPopup can be much simpler
        //TODO This also shouldn't be static
        private static List<ReplPopupSuggestion> _dataset;

        private int _currentTab = 3;

        private readonly ListController<TextController> _lineTextList;
        private readonly ListController<FieldControllerBase> _valueList;
        private readonly ListController<NumberController> _indents;

        private OuterReplScope _scope;

        private static readonly char[] Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
        private readonly List<char> _takenLetters = new List<char>();
        private readonly List<int> _takenNumbers = new List<int>();

        private int _forIndex = 0;
        private int _forInIndex = 0;

        public DishReplView(DocumentController doc)
        {
            InitializeComponent();

            _dataDoc = doc.GetDataDocument();
            _viewDoc = doc;

            DataContext = new DishReplViewModel();
            NewBlankScopeAndDSL();
            var dataset = OperatorScript.GetDataset();
            if (dataset != null) SetDataset(dataset);

            //intialize lists to save data
            _lineTextList = _dataDoc.GetField<ListController<TextController>>(KeyStore.ReplLineTextKey);
            _valueList = _dataDoc.GetField<ListController<FieldControllerBase>>(KeyStore.ReplValuesKey);
            _indents = _dataDoc.GetField<ListController<NumberController>>(KeyStore.ReplCurrentIndentKey);
            if (_indents.Count > 0) _currentTab = (int)_indents[_indents.Count - 1].Data;
            //var scopeDoc = dataDoc.GetField<DocumentController>(KeyStore.ReplScopeKey);
            //add items from lists to Repl
            var replItems = new ObservableCollection<ReplLineViewModel>();
            for (var i = 0; i < _lineTextList.Count; i++)
            {
                FieldControllerBase result = _valueList[i];
                string indentOffset = ReplLineNode.IsBaseCase(result) ? "   " : "";
                var newReplLine = new ReplLineViewModel
                {
                    LineText = _lineTextList[i].Data,
                    ResultText = indentOffset + result,
                    Value = result,
                    DisplayableOnly = true,
                    Indent = (int)_indents[i].Data
                };
                replItems.Add(newReplLine);
            }

            ViewModel.Items = replItems;
            _currentHistoryIndex = ViewModel.Items.Count;

            void ScrollViewerLoaded(object sender, RoutedEventArgs routedEventArgs)
            {
                xScrollViewer.Loaded -= ScrollViewerLoaded;
                ScrollToBottom();
            }

            xScrollViewer.Loaded += ScrollViewerLoaded;

            SetupTextBox();
        }

        public DishReplView(OuterReplScope scope)
        {
            _scope = scope;
        }

        public void SetIndent(int tab)
        {
            if (!(tab > 0 && tab < 6)) return;
            _currentTab = tab;
        }

        // ReSharper disable once InconsistentNaming
        private void NewBlankScopeAndDSL()
        {
            _scope = new OuterReplScope(_dataDoc.GetField<DocumentController>(KeyStore.ReplScopeKey));
            _dsl = new DSL(_scope);
        }
        #endregion

        #region Helper Functions

        public void Clear(bool clearData)
        {
            ViewModel.Items.Clear();
            if (!clearData) return;

            _currentHistoryIndex = 0;
            _dataDoc.SetField(KeyStore.ReplScopeKey, new DocumentController(), true);
            NewBlankScopeAndDSL();
            _lineTextList?.Clear();
            _valueList?.Clear();
            _indents?.Clear();
            _dataset = _dataset.Where(s => !(s is VariableSuggestion)).ToList();
        }

        public void Close(bool closeAll)
        {
            return;
            //if (closeAll)
            //{
            //    foreach (var vm in _viewModelsInSession)
            //    {
            //        if (vm.ArrowState == ReplLineNode.ArrowState.Open) vm.ArrowState = ReplLineNode.ArrowState.Closed;
            //    }
            //    return;
            //}
            //if (_viewModelsInSession.Count > 0 && _viewModelsInSession.Last().ArrowState is ReplLineNode.ArrowState.Open) _viewModelsInSession.Last().ArrowState = ReplLineNode.ArrowState.Closed;
        }

        public static void SetDataset(List<string> data) => _dataset = data.Select(func => new FunctionSuggestion(func) as ReplPopupSuggestion).ToList();

        public static void NewVariable(string var)
        {
            if (_dataset == null)
            {
                OperatorScript op = OperatorScript.Instance; //This will set _dataset
            }
            _dataset?.Add(new VariableSuggestion(var));
        }

        private void MoveCursorToEnd(TextBox elem = null, int? end = null)
        {
            elem = elem ?? xTextBox;
            elem.SelectionStart = end ?? xTextBox.Text.Length;
            elem.SelectionLength = 0;
        }

        public void ScrollToBottom()
        {
            xScrollViewer.UpdateLayout();
            xScrollViewer.ChangeView(null, float.MaxValue, null, true);
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
                if (chars[i] == '\r' && i > 0 && (Char.IsLetterOrDigit(chars[i - 1]) || chars[i - 1] == '-' || chars[i - 1] == '+'))
                {
                    resultChars.Add(';');
                    resultChars.Add(chars[i]);
                }
                else if (i == chars.Length - 1 && i > 0 &&
                         (Char.IsLetterOrDigit(chars[i]) || chars[i] == '-' || chars[i] == '+' || chars[i] == '"'))
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

        public void FinishFunctionCall(string text, TextBox box)
        {
            var place1 = box.SelectionStart;
            var stringLength = 0;
            var selectOffset = 0;
            var newText = "";
            var selectLength = 0;
            if (text.TrimStart().Length >= "for ".Length && text.Substring(text.Length - "for ".Length).Equals("for "))
            {
                while (_scope.TryGetVariable(Alphabet[_forIndex].ToString(), out var _) || _takenLetters.Contains(Alphabet[_forIndex])) { _forIndex++; }

                stringLength = 4;

                var ct = Alphabet[_forIndex];
                _takenLetters.Add(ct);
                newText = text + $"(var {ct} = 0; {ct} < UPPER; {ct}++)" + " {\r      \r}";
                selectOffset = 16; //36 to get to body
                selectLength = 5;
            }
            else if (text.TrimStart().Length >= "forin ".Length && text.Substring(text.Length - "forin ".Length).Equals("forin "))
            {
                stringLength = 6;

                var varExp = _scope.TryGetVariable("item", out var _) ? "" : "var ";
                newText = text.Substring(0, text.Length - "forin ".Length) + $"for ({varExp}item in [])" + " {\r      item\r}";
                selectOffset = 12;

            }
            else if (text.TrimStart().Length >= "forin? ".Length && text.Substring(text.Length - "forin? ".Length).Equals("forin? "))
            {
                stringLength = 7;
                var varExp = _scope.TryGetVariable("res", out var _) ? "" : "var ";
                newText = text.Substring(0, text.Length - "forin? ".Length) + $"for ({varExp}res in f(\":\"))" + " {\r      data_doc(res). = \r}";
                selectOffset = 12;
                selectLength = 0;
            }
            else if (text.TrimStart().Length >= "forin+ ".Length && text.Substring(text.Length - "forin+ ".Length).Equals("forin+ "))
            {
                var ret = text.TrimStart().Length == 7 ? "" : "\r";
                while (_scope.TryGetVariable("var myList" + _forInIndex, out var _) || _takenNumbers.Contains(_forInIndex)) { _forInIndex++; }

                var newList = "myList" + _forInIndex;
                _takenNumbers.Add(_forInIndex);
                box.Text = text.Insert(place1 - 7, $"{ret}var {newList} = []\r");
                var offset = _forInIndex.ToString().Length + ret.Length - 1;

                box.Text = box.Text.Substring(0, box.Text.Length - "forin+ ".Length) + $"for (var item in {newList})" + " {\r      item\r}";
                box.SelectionStart = place1 + 8 + offset;

            }
            else if (text.TrimStart().Length >= "dowhile ".Length && text.Substring(text.Length - "dowhile ".Length).Equals("dowhile "))
            {
                stringLength = 8;
                newText = text + "(condition) {\r      \r}";
                selectOffset = 1;
                selectLength = 9;
            }
            else if (text.TrimStart().Length >= "while ".Length && text.Substring(text.Length - "while ".Length).Equals("while "))
            {
                stringLength = 6;
                newText = text + "(condition) {\r      \r}";
                selectOffset = 1;
                selectLength = 9;
            }
            else if (text.TrimStart().Length >= "if ".Length && text.Substring(text.Length - "if ".Length).Equals("if "))
            {
                stringLength = 3;
                newText = text + "(condition) {\r      \r}";
                selectOffset = 1;
                selectLength = 9;
            }

            if (stringLength != 0)
            {
                if (text.TrimStart().Length != stringLength)
                {
                    box.Text = text.Insert(place1 - stringLength, "\r");

                    place1++;
                }

                box.Text = newText;
                box.SelectionStart = place1 + selectOffset;
                box.SelectionLength = selectLength;
            }
        }
        #endregion

        #region Toolbar

        private void XScript_OnClick(object sender, RoutedEventArgs e)
        {
            var collection = this.GetFirstAncestorOfType<CollectionView>()?.ViewModel;
            if (collection == null) return;
            //open DishScriptEditView with repl text
            string allCode = "";
            foreach (var line in _lineTextList)
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

        #region Repl Line Editing
        private void XInputBlock_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            DisableAllTextBoxes();

            ReplLineViewModel data = (sender as TextBlock)?.DataContext as ReplLineViewModel;
            data.EditTextValue = true;
        }

        private bool CheckSpecialCommands(string script)
        {
            switch (script.Trim())
            {
                case "clear":
                    Clear(false);
                    return true;
                case "clear all":
                    Clear(true);
                    return true;
                case "close":
                    Close(false);
                    return true;
                case "close all":
                    Close(true);
                    return true;
            }

            //TAB
            var indentSplit = script.Replace(" ", "").Split("=", StringSplitOptions.RemoveEmptyEntries);
            if (indentSplit[0].Equals("tab"))
            {
                // DEFAULT
                if (indentSplit.Length == 1)
                {
                    SetIndent(3);
                    return true;
                }

                if (double.TryParse(indentSplit[1].Trim(), out double tab))
                {
                    SetIndent((int)tab);
                    return true;
                }
            }

            return false;
        }

        private async void InputBoxSubmit(ReplLineViewModel data, string currentText, int? index = null)
        {
            if (data.EditTextValue)
            {
                //get element num
                if (index == null)
                {
                    for (int i = 0; i < ViewModel.Items.Count; i++)
                    {
                        if (ViewModel.Items[i] == data)
                        {
                            index = i;
                            break;
                        }
                    }
                }

                data.EditTextValue = false;

                var text = AddSemicolons(currentText);
                var oldText = data.LineText;
                data.LineText = text;

                //undo old variable declarations 
                await _dsl.Run(oldText, true, true);

                FieldControllerBase result;
                try
                {
                    result = await _dsl.Run(text, true);
                }
                catch (Exception ex)
                {
                    result = new TextController("There was an error: " + ex.StackTrace);
                }

                if (result == null) result = new TextController($" Exception:\n            InvalidInput\n      Feedback:\n            Input yielded an invalid return. Enter <help()> for a complete catalog of valid functions.");

                string indentOffset = ReplLineNode.IsBaseCase(result) ? "   " : "";
                data.ResultText = indentOffset + result;
                data.Value = result;
                data.DisplayableOnly = true;
                data.Update();
                //update input and outputs in list
                if (index != null)
                {
                    _valueList[index ?? 0] = result;
                    _lineTextList[index ?? 0] = new TextController(text);

                }
            }
        }

        private void XInputBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            var text = (sender as TextBox).Text;
            var model = (sender as TextBox)?.DataContext as ReplLineViewModel;
            InputBoxSubmit(model, text);
        }

        private void XInputBox_OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
            var text = (sender as TextBox).Text;
            var model = (sender as TextBox)?.DataContext as ReplLineViewModel;
            if (e.Key == VirtualKey.Enter)
            {
                InputBoxSubmit(model, text);
            }

            FinishFunctionCall(text, sender as TextBox);
        }

        private void DisableAllTextBoxes()
        {
            for (int i = 0; i < ViewModel.Items.Count; i++)
            {
                var item = ViewModel.Items[i];
                if (item.EditTextValue)
                {
                    InputBoxSubmit(item, item.LineText, i);
                }
            }
        }

        private async Task ReRunLine(ReplLineViewModel data, string text)
        {
            DisableAllTextBoxes();

            //get element num
            int? index = null;

            for (int i = 0; i < ViewModel.Items.Count; i++)
            {
                if (ViewModel.Items[i] == data)
                {
                    index = i;
                    break;
                }
            }

            //undo old variable declarations 
            await _dsl.Run(text, true, true);

            FieldControllerBase result;
            try
            {
                result = await _dsl.Run(text, true);
            }
            catch (Exception ex)
            {
                result = new TextController("There was an error: " + ex.StackTrace);
            }

            if (result == null)
                result =
                    new TextController($" Exception:\n            InvalidInput\n      Feedback:\n            Input yielded an invalid return. Enter <help()> for a complete catalog of valid functions.");

            string indentOffset = ReplLineNode.IsBaseCase(result) ? "   " : "";
            data.ResultText = indentOffset + result;
            data.Indent = _currentTab;
            data.Value = result;
            data.DisplayableOnly = true;
            data.Update();

            //update input and outputs in list
            if (index != null)
            {
                _valueList[index ?? 0] = result;
            }
        }

        private async void XInputArrow_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            //tap arrow to revaluate line
            var data = (sender as TextBlock)?.DataContext as ReplLineViewModel;
            var text = data?.LineText;

            await ReRunLine(data, text);
        }

        private async void XInputBlock_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var data = (sender as TextBlock)?.DataContext as ReplLineViewModel;
            var text = data?.LineText;

            await ReRunLine(data, text);
        }

        #endregion

        #region On Type Actions
        private void XTextBox_OnGettingFocus(UIElement sender, GettingFocusEventArgs args)
        {
            DisableAllTextBoxes();
            MoveCursorToEnd();
        }

        private void SetupTextBox()
        {
            async void EnterPressed(KeyRoutedEventArgs e)
            {
                if (Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down))
                {
                    return;
                }

                string command = xTextBox.Text;
                xTextBox.Text = "";

                if (string.IsNullOrEmpty(command)) return;
                if (CheckSpecialCommands(command))
                {
                    ScrollToBottom();
                    e.Handled = true;
                    return;
                }

                FieldControllerBase retVal;
                try
                {
                    retVal = await _dsl.Run(command, true);
                }
                catch (Exception ex)
                {
                    retVal = new TextController("There was an error: " + ex.Message);
                }
                if (retVal == null) retVal = new TextController("null");

                string indentOffset = ReplLineNode.IsBaseCase(retVal) ? "   " : "";
                var head = new ReplLineViewModel
                {
                    LineText = command,
                    ResultText = indentOffset + retVal,
                    Value = retVal,
                    DisplayableOnly = true,
                    Indent = _currentTab
                };
                //head.ResultText = head.GetValueFromResult(retVal);
                ViewModel.Items.Add(head);

                _currentHistoryIndex = ViewModel.Items.Count;
                _lineTextList.Add(new TextController(command));
                _valueList.Add(retVal);
                _indents.Add(new NumberController(_currentTab));

                ScrollToBottom();
                e.Handled = true;
            }

            string storedCommand = "";
            void UpPressed(KeyRoutedEventArgs e)
            {
                var beforeCursor = xTextBox.Text.Substring(0, xTextBox.SelectionStart);
                bool moveUp = !beforeCursor.Contains('\r');
                if (!MainPage.Instance.IsCtrlPressed() && !MainPage.Instance.IsShiftPressed() && moveUp)
                {
                    //get last terminal input entered
                    var index = _currentHistoryIndex - 1;
                    if (_currentHistoryIndex == ViewModel.Items.Count)
                    {
                        storedCommand = xTextBox.Text;
                    }
                    if (index >= 0)
                    {
                        _currentHistoryIndex--;
                        xTextBox.Text = ViewModel.Items[index].LineText;

                        MoveCursorToEnd();
                        e.Handled = true;
                    }
                }
                else if (MainPage.Instance.IsCtrlPressed())
                {
                    if (xSuggestions.SelectedIndex > -1 && xSuggestionsPopup.Visibility == Visibility.Visible)
                        xSuggestions.SelectedIndex--;
                    e.Handled = true;
                }
            }

            void DownPressed(KeyRoutedEventArgs e)
            {
                var afterCursor = xTextBox.Text.Substring(xTextBox.SelectionStart, xTextBox.Text.Length - xTextBox.SelectionStart);
                bool moveDown = !(afterCursor.Contains('\r'));

                var numItem = ViewModel.Items.Count;
                if (!MainPage.Instance.IsCtrlPressed() && !MainPage.Instance.IsShiftPressed() && moveDown)
                {
                    var index = _currentHistoryIndex + 1;
                    if (index > numItem)
                    {
                        return;
                    }
                    _currentHistoryIndex++;
                    xTextBox.Text = index < numItem ? ViewModel.Items[index].LineText : storedCommand;
                    MoveCursorToEnd();
                    e.Handled = true;
                }
                else if (MainPage.Instance.IsCtrlPressed())
                {
                    if (xSuggestions.SelectedIndex + 1 < xSuggestions.Items?.Count &&
                        xSuggestionsPopup.Visibility == Visibility.Visible) xSuggestions.SelectedIndex++;
                    e.Handled = true;
                }
            }

            void RightPressed(KeyRoutedEventArgs e)
            {
                if (xSuggestions.SelectedIndex > -1) SelectPopup(xSuggestions.SelectedItem as ReplPopupSuggestion);
                xSuggestions.SelectedIndex = -1;
            }

            void DeletePressed(KeyRoutedEventArgs e)
            {
                if (MainPage.Instance.IsCtrlPressed())
                    xTextBox.Text = "";
            }

            void AutoPair(string pairStart, string pairEnd, bool enter = false)
            {
                if (pairStart == pairEnd && AutoPairClose(pairEnd))
                {
                    return;
                }
                var index = xTextBox.SelectionStart;
                var text = xTextBox.Text;
                text = text.Insert(index, pairStart);
                var offset = pairStart.Length;

                while (index + offset < text.Length && IsProperLetter(text[index + offset])) { offset++; }

                if (enter)
                {
                    pairEnd = "\r  \r" + pairEnd;
                    xTextBox.Text = text.Insert(index + offset, pairEnd);
                    xTextBox.SelectionStart = index + pairStart.Length + pairEnd.Length - 2;
                }
                else
                {
                    xTextBox.Text = text.Insert(index + offset, pairEnd);
                    xTextBox.SelectionStart = index + pairStart.Length;
                }
            }

            bool AutoPairClose(string pairEnd)
            {
                var index = xTextBox.SelectionStart;
                var text = xTextBox.Text;
                if (index + pairEnd.Length <= text.Length && text.Substring(index, pairEnd.Length) == pairEnd)
                {
                    xTextBox.SelectionStart = index + 1;
                    return true;
                }

                return false;
            }

            xTextBox.AddKeyHandler(VirtualKey.Enter, EnterPressed);
            xTextBox.AddKeyHandler(VirtualKey.Up, UpPressed);
            xTextBox.AddKeyHandler(VirtualKey.Down, DownPressed);
            xTextBox.AddKeyHandler(VirtualKey.Right, RightPressed);
            xTextBox.AddKeyHandler(VirtualKey.Delete, DeletePressed);
            xTextBox.AddKeyHandler((VirtualKey)222, args => // "/' key
            {
                if (MainPage.Instance.IsShiftPressed())//"
                {
                    AutoPair("\"", "\"");
                }
                else//'
                {
                    AutoPair("'", "'");
                }

                args.Handled = true;
            });
            xTextBox.AddKeyHandler((VirtualKey)219, args => // [/{ key
            {
                if (MainPage.Instance.IsShiftPressed())//{
                {
                    AutoPair("{", "}", true);
                }
                else//[
                {
                    AutoPair("[", "]");
                }

                args.Handled = true;
            });
            xTextBox.AddKeyHandler((VirtualKey)221, args => // [/{ key
            {
                args.Handled = AutoPairClose(MainPage.Instance.IsShiftPressed() ? "}" : "]");
            });
            xTextBox.AddKeyHandler(VirtualKey.Number9, args =>
            {
                if (MainPage.Instance.IsShiftPressed())
                {
                    AutoPair("(", ")");
                    args.Handled = true;
                }
            });
            xTextBox.AddKeyHandler(VirtualKey.Number0, args =>
            {
                if (MainPage.Instance.IsShiftPressed())
                {
                    args.Handled = AutoPairClose(")");
                }
            });
            xTextBox.AddKeyHandler((VirtualKey)188, args =>// < key
            {
                if (MainPage.Instance.IsShiftPressed())
                {
                    AutoPair("<", ">");
                    args.Handled = true;
                }
            });
            xTextBox.AddKeyHandler((VirtualKey)190, args =>// < key
            {
                if (MainPage.Instance.IsShiftPressed())
                {
                    args.Handled = AutoPairClose(">");
                }
            });

            xTextBox.AddKeyHandler(VirtualKey.Space, args =>
            {
                if (MainPage.Instance.IsCtrlPressed())
                {
                    FinishFunctionCall(xTextBox.Text, xTextBox);
                    args.Handled = true;
                }
            });

        }

        private void XTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            //_currentHistoryIndex = ViewModel.Items.Count;

            if (xTextBox.Text.Equals(""))
            {
                _takenNumbers.Clear();
                _takenLetters.Clear();
            }

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
                    var op = OperatorScript.Instance; //This creates the OperatorScript if necessary, which calls init
                }

                var suggestions = _dataset?.Where(x => x.Name.StartsWith(lastWord)).ToList();
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
        }
        #endregion

        #region Suggestions 
        private void Suggestions_OnItemClick(object sender, ItemClickEventArgs e)
        {
            //get selected item
            var selectedItem = e.ClickedItem as ReplPopupSuggestion;
            SelectPopup(selectedItem);
        }

        private void SelectPopup(ReplPopupSuggestion selectedItem)
        {
            //only change last word to new text
            var currentText = xTextBox.Text.Split(new []{' ', '\r'}, StringSplitOptions.RemoveEmptyEntries);
            var keepText = "";
            if (currentText.Length > 1)
            {
                int lastWordLength = currentText[currentText.Length - 1].Length;
                keepText = xTextBox.Text.Substring(0, xTextBox.Text.Length - lastWordLength);
            }

            xTextBox.Text = keepText + selectedItem.FormattedText();
            xTextBox.Focus(FocusState.Pointer);
            MoveCursorToEnd(xTextBox, (keepText + selectedItem.Name).Length);

            xSuggestionsPopup.IsOpen = false;
            xSuggestionsPopup.Visibility = Visibility.Collapsed;
        }
        #endregion

        private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            var docView = this.GetFirstAncestorOfType<DocumentView>();
            using (UndoManager.GetBatchHandle())
                docView.DeleteDocument();
        }
    }
    /*
function grid(col, spacing) {
  var w = 0;
  var h = 0;
  for (var doc in col.Data) {
    w = max(w, doc.ActualSize.x());
    h = max(h, doc.ActualSize.y());
  }
  var startX = -col._PanPosition.x() + spacing;
  var startY = -col._PanPosition.y() + spacing;
  var endX = startX + col.ActualSize.x() - 2 * spacing;
  var x = startX;
  var y = startY;
  for (var doc in col.Data) {
    if (x + doc.ActualSize.x() > endX) {
      x = startX;
      y = y + h + spacing;
    }
    doc.Position = point(x, y);
    x = x + w + spacing;
  }
}
 
function grid2(docs, numCols, spacing, startPos) {
  var w = 0;
  var h = 0;
  for (var doc in docs) {
    w = max(w, doc.ActualSize.x());
    h = max(h, doc.ActualSize.y());
  }
  var startX = startPos.x();
  var startY = startPos.y();
  var x = startX;
  var y = startY;
  var currentCol = 0;
  for (var doc in docs) {
    if (currentCol == numCols) {
      x = startX;
      y = y + h + spacing;
      currentCol = 0;
    }
    currentCol++;
    doc.Position = point(x, y);
    x = x + w + spacing;
  }
  return w * numCols + spacing * (numCols - 1);
}

function group(docs, groupingField, numCols, spacing, startPos) {
  var map = {};
  for (var doc in docs) {
    var compField = doc.get_field(groupingField).to_string();
    var l = map.get_field(compField);
    if (l == null) {
      l = [doc];
    } else {
      l = l + doc;
    }
    map.set_field(compField, l);
  }

  var x = startPos.x();
  var y = startPos.y();
  for (var key in map.keys()) {
    var l = map.get_field(key);
    var width = grid(l, numCols, spacing, point(x, y));
    x = x + width + spacing * 2;
  }
}

function spiral(col) {
  count = col.Data.count();
  var angle = 0;
  for (i = 0; i < count; i++) {
    var doc = col.Data[i];
    var r = 100 + 40 * i;
    var x = cos(angle) * r - doc.ActualSize.x() / 2;
    var y = sin(angle) * r - doc.ActualSize.y() / 2;
    angle = angle + 200 / r;
    doc.Position = point(x, y);
  }
}


     */
}
