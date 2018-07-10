using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class DishScriptEditView : UserControl
    {
        #region Definitions and Initailization
        private DSL _dsl;

        private readonly DocumentController _dataDoc;
        private OuterReplScope _scope;

        private static readonly char[] Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
        private readonly List<char> _takenLetters = new List<char>();
        private readonly List<int> _takenNumbers = new List<int>();

        private int _forIndex = 0;
        private int _forInIndex = 0;

        private bool _running;

        private string _currentText;

        private bool _oneStar;

        public DishScriptEditView(DocumentController dataDoc)
        {
            _dataDoc = dataDoc;
            _scope = new OuterReplScope();
            InitializeComponent();

            //intialize lists to save data
            _currentText = dataDoc.GetField<TextController>(KeyStore.ScriptTextKey).Data;
            xTextBox.Text = _currentText ?? "";
        }
        #endregion


        #region Button click
        private void XRun_OnClick(object sender, RoutedEventArgs e)
        {
            //make new scope
            _scope = new OuterReplScope();
            _dsl = new DSL(_scope);

            FieldControllerBase returnValue;
            _running = true;
            try
            {
                returnValue = _dsl.Run(xTextBox.Text, true);
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

        #region On Type
        private void XTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var place1 = xTextBox.SelectionStart;
            var length1 = 0;
            var length2 = 0;
            var newText = "";
            var selectLength = 0;
            if (xTextBox.Text.TrimStart().Length >= "for ".Length && xTextBox.Text.Substring(xTextBox.Text.Length - "for ".Length).Equals("for "))
            {
                while (_scope.GetVariable(Alphabet[_forIndex].ToString()) != null || _takenLetters.Contains(Alphabet[_forIndex])) { _forIndex++; }

                length1 = 4;
                length2 = 16;
                selectLength = 5;
                var ct = Alphabet[_forIndex];
                _takenLetters.Add(ct);
                newText = xTextBox.Text + $"(var {ct} = 0; {ct} < UPPER; {ct}++)" + " {\r      \r}";
            }
            else if (xTextBox.Text.TrimStart().Length >= "forin ".Length && xTextBox.Text.Substring(xTextBox.Text.Length - "forin ".Length).Equals("forin "))
            {
                length1 = 6;
                length2 = 12;
                var varExp = (_scope.GetVariable("item") != null) ? "" : "var ";
                newText = xTextBox.Text.Substring(0, xTextBox.Text.Length - "forin ".Length) + $"for ({varExp}item in [])" + " {\r      item\r}";
            }
            else if (xTextBox.Text.TrimStart().Length >= "forin? ".Length && xTextBox.Text.Substring(xTextBox.Text.Length - "forin? ".Length).Equals("forin? "))
            {
                length1 = 7;
                length2 = 11;
                selectLength = 8;
                var varExp = (_scope.GetVariable("res") != null) ? "" : "var ";
                newText = xTextBox.Text.Substring(0, xTextBox.Text.Length - "forin? ".Length) + $"for ({varExp}res in f(\"\"))" + " {\r      res. = \r}";
            }
            else if (xTextBox.Text.TrimStart().Length >= "forin+ ".Length && xTextBox.Text.Substring(xTextBox.Text.Length - "forin+ ".Length).Equals("forin+ "))
            {
                var ret = xTextBox.Text.TrimStart().Length == 7 ? "" : "\r";
                while (_scope.GetVariable("var myList" + _forInIndex) != null || _takenNumbers.Contains(_forInIndex)) { _forInIndex++; }

                var newList = "myList" + _forInIndex;
                _takenNumbers.Add(_forInIndex);
                xTextBox.Text = xTextBox.Text.Insert(place1 - 7, $"{ret}var {newList} = []\r");
                var offset = _forInIndex.ToString().Length + ret.Length - 1;

                xTextBox.Text = xTextBox.Text.Substring(0, xTextBox.Text.Length - "forin+ ".Length) + $"for (var item in {newList})" + " {\r      item\r}";
                xTextBox.SelectionStart = place1 + 8 + offset;
            }
            else if (xTextBox.Text.TrimStart().Length >= "dowhile ".Length && xTextBox.Text.Substring(xTextBox.Text.Length - "dowhile ".Length).Equals("dowhile "))
            {
                length1 = 8;
                length2 = 1;
                newText = xTextBox.Text + "(condition) {\r      \r}";
                selectLength = 9;
            }
            else if (xTextBox.Text.TrimStart().Length >= "while ".Length && xTextBox.Text.Substring(xTextBox.Text.Length - "while ".Length).Equals("while "))
            {
                length1 = 6;
                length2 = 1;
                newText = xTextBox.Text + "(condition) {\r      \r}";
                selectLength = 9;
            }
            else if (xTextBox.Text.TrimStart().Length >= "if ".Length && xTextBox.Text.Substring(xTextBox.Text.Length - "if ".Length).Equals("if "))
            {
                length1 = 3;
                length2 = 1;
                newText = xTextBox.Text + "(condition) {\r      \r}";
                selectLength = 9;
            }

            if (length1 != 0)
            {
                if (xTextBox.Text.TrimStart().Length != length1)
                {
                    xTextBox.Text = xTextBox.Text.Insert(place1 - length1, "\r");
                    place1++;
                }

                xTextBox.Text = newText;
                xTextBox.SelectionStart = place1 + length2;
                xTextBox.SelectionLength = selectLength;
            }


            var textDiff = DishReplView.StringDiff(xTextBox.Text, _currentText);
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

            _currentText = xTextBox.Text;

            
          _dataDoc.GetField<TextController>(KeyStore.ScriptTextKey).Data = _currentText;

        }
         #endregion



    }
}
