using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class DishScriptEditView : UserControl
    {
        #region Definitions and Initailization
        private DSL _dsl;

        private readonly DocumentController _viewDoc;
        private readonly DocumentController _dataDoc;
        private DocumentScope _scope;

        private static readonly char[] Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
        private readonly List<char> _takenLetters = new List<char>();
        private readonly List<int> _takenNumbers = new List<int>();

        private int _forIndex = 0;
        private int _forInIndex = 0;

        private bool _running;

        private string _currentText;

        private bool _oneStar;

        public DishScriptEditView(DocumentController doc)
        {
            _viewDoc = doc;
            _dataDoc = doc.GetDataDocument();
            _scope = new DocumentScope();
            InitializeComponent();

            //intialize lists to save data
            _currentText = _dataDoc.GetField<TextController>(KeyStore.ScriptTextKey).Data;
            xTextBox.Text = _currentText ?? "";

            ResetLineNums();
        }
        #endregion


        #region Button click

        private void textToCommands(char[] letters, string growing, ListController<TextController> output,
            int inBrackets = 0, bool inQuotes = false)
        {
            for (int i = 0; i < letters.Length; i++)
            {
                var letter = letters[i];
                var newText = growing + letter;
                if (i == letters.Length - 1 && letter != '\r')
                {
                    //last char
                    if (newText != " ")
                    {
                        output.Add(new TextController(newText));
                    }
                } else if (letter == '"' || letter == '\'')
                {
                    inQuotes = !inQuotes;
                    growing = newText;
                }
                else if ((letter == ';' || letter == '\r') && inBrackets == 0 && !inQuotes)
                {
                    //end of command
                    if (newText.Trim('\r') != "" && newText.Trim('\r') != " ")
                    {
                            output.Add(new TextController(newText));
                            growing = "";
                    }
                } else if (letter == '}' && !inQuotes)
                {
                    //end of loop
                    inBrackets--;
                    if (inBrackets == 0 && newText != " ")
                    {
                        output.Add(new TextController(newText));
                        growing = "";
                    }
                }  else if (letter == '{' || letter == '(' && !inQuotes)
                {
                    inBrackets++;
                    growing = newText;
                } else if (letter == ')' && !inQuotes)
                {
                    inBrackets--;
                    growing = newText;
                }
                else if(letter != '\r')
                {
                    growing = newText;
                }
            }

        }


        private async void XRepl_OnClick(object sender, RoutedEventArgs e)
        {
            var collection = this.GetFirstAncestorOfType<CollectionView>()?.ViewModel;
            if (collection == null) return;
            //open Repl with a command for each input
           //split _currentText into commands
            var commands = new ListController<TextController>();
             textToCommands(_currentText.ToCharArray(), "", commands);

            _scope = new DocumentScope();
            _dsl = new DSL(_scope);
            var results = new ListController<FieldControllerBase>();
            foreach (var command in commands)
            {
                FieldControllerBase returnValue;
                try
                {
                    returnValue = await _dsl.Run(command.Data, true);
                }
                catch (Exception ex)
                {
                    returnValue = new TextController("There was an error: " + ex.StackTrace);
                }
                results.Add(returnValue);
            }

            var pt = _viewDoc.GetPositionField().Data;
            var width = _viewDoc.GetWidthField().Data;
            var height = _viewDoc.GetHeightField().Data;

            var indents = new ListController<NumberController>();
            foreach (var unused in results)
            {
                indents.Add(new NumberController(3));
            }

            var note = new DishReplBox(pt.X - width - 15, pt.Y, width, height, commands, results, _scope.VariableDoc(), indents);

            Actions.DisplayDocument(collection, note.Document);
        
         }

        private async void XRun_OnClick(object sender, RoutedEventArgs e)
        {
            //make new scope
            _scope = new DocumentScope();
            _dsl = new DSL(_scope);

            FieldControllerBase returnValue;
            _running = true;
            try
            {
                returnValue = await _dsl.Run(xTextBox.Text, true);
            }
            catch (Exception ex)
            {
                returnValue = new TextController("There was an error: " + ex.StackTrace);
            }

            _running = false;

            if (returnValue == null) returnValue = new TextController($" Exception:\n            InvalidInput\n      Feedback:\n            Input yielded an invalid return. Enter <help()> for a complete catalog of valid functions.");

            xResult.Text = "Output: " + returnValue;
        }

        #endregion

        #region Helper Function
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

        private void ResetLineNums()
        {
            var text = xTextBox.Text.Split('\r');
            var textNums = "";
            for(int i =0; i < text.Length; i++)
            {
                textNums += (i + 1) + "\r";
            }

            xTextLines.Text = textNums;
        }

        #endregion

        #region On Type
        private void XTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            DishReplView view = new DishReplView(_scope);
            view.FinishFunctionCall(xTextBox.Text, xTextBox);

            var textDiff = StringDiff(xTextBox.Text, _currentText);
            switch (textDiff)
            {
                case "\"" when xTextBox.Text.Length > _currentText.Length:
                    var place = xTextBox.SelectionStart;
                    var offset = 0;

                    while (place + offset < xTextBox.Text.Length &&
                           DishReplView.IsProperLetter(xTextBox.Text[place + offset]))
                    {
                        offset++;
                    }

                    place += offset;

                    xTextBox.Text = xTextBox.Text.Insert(place, "\"");
                    xTextBox.SelectionStart = place;
                    break;
                case "(" when xTextBox.Text.Length > _currentText.Length:
                    place = xTextBox.SelectionStart;
                    offset = 0;

                    while (place + offset < xTextBox.Text.Length &&
                           DishReplView.IsProperLetter(xTextBox.Text[place + offset]))
                    {
                        offset++;
                    }

                    place += offset;

                    xTextBox.Text = xTextBox.Text.Insert(place, ")");
                    xTextBox.SelectionStart = place;
                    break;
                case "\'" when xTextBox.Text.Length > _currentText.Length:
                    place = xTextBox.SelectionStart;
                    offset = 0;

                    while (place + offset < xTextBox.Text.Length &&
                           DishReplView.IsProperLetter(xTextBox.Text[place + offset]))
                    {
                        offset++;
                    }

                    place += offset;

                    xTextBox.Text = xTextBox.Text.Insert(place, "\'");
                    xTextBox.SelectionStart = place;
                    break;
                case "{" when xTextBox.Text.Length > _currentText.Length:
                    place = xTextBox.SelectionStart;
                    xTextBox.Text = xTextBox.Text.Insert(place, "\r      \r}");
                    xTextBox.SelectionStart = place + 7;
                    break;
                case "*":
                    if (_oneStar)
                    {
                        place = xTextBox.SelectionStart;
                        xTextBox.Text = xTextBox.Text.Insert(place, "      */");
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
                    break;
            }
            ResetLineNums();

            _currentText = xTextBox.Text;

            
          _dataDoc.GetField<TextController>(KeyStore.ScriptTextKey).Data = _currentText;

        }

        #endregion

        private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                this.GetDocumentView().DeleteDocument();
            }
        }
    }
}
