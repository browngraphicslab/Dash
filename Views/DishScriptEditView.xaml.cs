using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

namespace Dash
{
    public sealed partial class DishScriptEditView : UserControl
    {
        private DSL _dsl;

        private readonly DocumentController _dataDoc;
        private OuterReplScope _scope;

        private static readonly char[] Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
        private readonly List<char> _takenLetters = new List<char>();
        private readonly List<int> _takenNumbers = new List<int>();

        private int _forIndex = 0;
        private int _forInIndex = 0;

        public DishScriptEditView(DocumentController dataDoc)
        {
            _dataDoc = dataDoc;
           InitializeComponent();

            //intialize lists to save data
            xTextBox.Text = _dataDoc.GetField<TextController>(KeyStore.ScriptTextKey).Data;

        }

        private void XRun_OnClick(object sender, RoutedEventArgs e)
        {
            //make new scope
            _scope = new OuterReplScope();
            _dsl = new DSL(_scope);

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

            xResult.Text = "Output: " + returnValue;
        }

        private void XTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (xTextBox.Text.TrimStart().Length >= "for ".Length && xTextBox.Text.Substring(xTextBox.Text.Length - "for ".Length).Equals("for "))
            {
                while (_scope.GetVariable(Alphabet[_forIndex].ToString()) != null || _takenLetters.Contains(Alphabet[_forIndex])) { _forIndex++; }
                var place = xTextBox.SelectionStart;
                if (xTextBox.Text.TrimStart().Length != 4)
                {
                    xTextBox.Text = xTextBox.Text.Insert(place - 4, "\r");
                    place++;
                }
                var ct = Alphabet[_forIndex];
                _takenLetters.Add(ct);
                xTextBox.Text += $"(var {ct} = 0; {ct} < UPPER; {ct}++)" + " {\r      \r}";
                xTextBox.SelectionStart = place + 16; //36 to get to body
                xTextBox.SelectionLength = 5;
            }
            else if (xTextBox.Text.TrimStart().Length >= "forin ".Length && xTextBox.Text.Substring(xTextBox.Text.Length - "forin ".Length).Equals("forin "))
            {
                var place = xTextBox.SelectionStart;
                if (xTextBox.Text.TrimStart().Length != 6)
                {
                    xTextBox.Text = xTextBox.Text.Insert(place - 6, "\r");
                    place++;
                }
                var varExp = (_scope.GetVariable("item") != null) ? "" : "var ";
                xTextBox.Text = xTextBox.Text.Substring(0, xTextBox.Text.Length - "forin ".Length) + $"for ({varExp}item in [])" + " {\r      item\r}";
                xTextBox.SelectionStart = place + 12;
            }
            else if (xTextBox.Text.TrimStart().Length >= "forin? ".Length && xTextBox.Text.Substring(xTextBox.Text.Length - "forin? ".Length).Equals("forin? "))
            {
                var place = xTextBox.SelectionStart;
                if (xTextBox.Text.TrimStart().Length != 7)
                {
                    xTextBox.Text = xTextBox.Text.Insert(place - 7, "\r");
                    place++;
                }
                var varExp = (_scope.GetVariable("res") != null) ? "" : "var ";
                xTextBox.Text = xTextBox.Text.Substring(0, xTextBox.Text.Length - "forin? ".Length) + $"for ({varExp}res in f(\"\"))" + " {\r      res. = \r}";
                xTextBox.SelectionStart = place + 12;
            }
            else if (xTextBox.Text.TrimStart().Length >= "forin+ ".Length && xTextBox.Text.Substring(xTextBox.Text.Length - "forin+ ".Length).Equals("forin+ "))
            {
                var place = xTextBox.SelectionStart;

                var ret = xTextBox.Text.TrimStart().Length == 7 ? "" : "\r";
                while (_scope.GetVariable("var myList" + _forInIndex) != null || _takenNumbers.Contains(_forInIndex)) { _forInIndex++; }

                var newList = "myList" + _forInIndex;
                _takenNumbers.Add(_forInIndex);
                xTextBox.Text = xTextBox.Text.Insert(place - 7, $"{ret}var {newList} = []\r");
                var offset = _forInIndex.ToString().Length + ret.Length - 1;

                xTextBox.Text = xTextBox.Text.Substring(0, xTextBox.Text.Length - "forin+ ".Length) + $"for (var item in {newList})" + " {\r      item\r}";
                xTextBox.SelectionStart = place + 8 + offset;
            }
            else if (xTextBox.Text.TrimStart().Length >= "dowhile ".Length && xTextBox.Text.Substring(xTextBox.Text.Length - "dowhile ".Length).Equals("dowhile "))
            {
                var place = xTextBox.SelectionStart;
                if (xTextBox.Text.TrimStart().Length != 8)
                {
                    xTextBox.Text = xTextBox.Text.Insert(place - 8, "\r");
                    place++;
                }
                xTextBox.Text += "(condition) {\r      \r}";
                xTextBox.SelectionStart = place + 1;
                xTextBox.SelectionLength = 9;
            }
            else if (xTextBox.Text.TrimStart().Length >= "while ".Length && xTextBox.Text.Substring(xTextBox.Text.Length - "while ".Length).Equals("while "))
            {
                var place = xTextBox.SelectionStart;
                if (xTextBox.Text.TrimStart().Length != 6)
                {
                    xTextBox.Text = xTextBox.Text.Insert(place - 6, "\r");
                    place++;
                }
                xTextBox.Text += "(condition) {\r      \r}";
                xTextBox.SelectionStart = place + 1;
                xTextBox.SelectionLength = 9;
            }
            else if (xTextBox.Text.TrimStart().Length >= "if ".Length && xTextBox.Text.Substring(xTextBox.Text.Length - "if ".Length).Equals("if "))
            {
                var place = xTextBox.SelectionStart;
                if (xTextBox.Text.TrimStart().Length != 3)
                {
                    xTextBox.Text = xTextBox.Text.Insert(place - 3, "\r");
                    place++;
                }
                xTextBox.Text += "(condition) {\r      \r}";
                xTextBox.SelectionStart = place + 1;
                xTextBox.SelectionLength = 9;
            }
        }
    }
}
