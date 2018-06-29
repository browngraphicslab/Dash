﻿using Dash.Models.DragModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
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
    public sealed partial class DishReplView : UserControl 
    {
        private DishReplViewModel ViewModel => DataContext as DishReplViewModel;
        private readonly DSL _dsl;

        private int _currentHistoryIndex = 0;

        private static List<String> _dataset;
        private bool _textModified;

        private bool _shiftPressed;

        public DishReplView()
        {
            this.InitializeComponent();
            this.DataContext = new DishReplViewModel();
            _dsl = new DSL(new Scope());
            xTextBox.GotFocus += XTextBoxOnGotFocus;
            xTextBox.LostFocus += XTextBoxOnLostFocus;
        }
        public FieldControllerBase TargetFieldController { get; set; }
        public Context TargetDocContext { get; set; }

        public static void SetDataset(List<string> data)
        {
            _dataset = data;
        }

        public static void NewVaraible(string var)
        {
            _dataset.Add(var);
        }

        private void XTextBoxOnLostFocus(object sender, RoutedEventArgs routedEventArgs)
        {
            Window.Current.CoreWindow.KeyUp -= CoreWindowOnKeyUp;
        }

        private void XTextBoxOnGotFocus(object sender, RoutedEventArgs routedEventArgs)
        {
            Window.Current.CoreWindow.KeyUp += CoreWindowOnKeyUp;
        }

        private void moveCursorToEnd(int? end = null)
        {
            if (xTextBox.Text.Length != 0)
            {
                xTextBox.SelectionStart = end ?? xTextBox.Text.Length;
                xTextBox.SelectionLength = 0;
            }
        }

        private void CoreWindowOnKeyUp(CoreWindow sender, KeyEventArgs args)
        {
            var numItem = ViewModel.Items.Count;
            switch (args.VirtualKey)
                {
                    case VirtualKey.Up:
                        var index1 = numItem - (_currentHistoryIndex + 1);
                        if (numItem > index1 && index1 >= 0)
                         {
                        _currentHistoryIndex++;
                        xTextBox.Text = ViewModel.Items.ElementAt(index1)?.LineText?.Substring(3) ?? xTextBox.Text;
                             moveCursorToEnd();
                         }

                        break;
                    case VirtualKey.Down:
                        var index = numItem - (_currentHistoryIndex - 1);
                        if (numItem > index && index >= 0)
                        {
                            _currentHistoryIndex--;
                            xTextBox.Text = ViewModel.Items.ElementAt(index)?.LineText?.Substring(3) ?? xTextBox.Text;
                            moveCursorToEnd();
                        } else if (index == numItem)
                        {
                            _currentHistoryIndex--;
                            xTextBox.Text = "";
                        }

                        break;
                }
        }

        private void XTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            //enter pressed without shift
            if (xTextBox.Text.Length > 1 && xTextBox.Text[xTextBox.Text.Length - 1] == '\r' && 
                !Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down))
                {
                    _currentHistoryIndex = 0;
                    //get text replacing newlines with spaces
                    var currentText = xTextBox.Text.Replace('\r', ' ');
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

                    //scroll to bottom
                    xScrollViewer.UpdateLayout();
                    xScrollViewer.ChangeView(0, xScrollViewer.ScrollableHeight, 1);
            }
            else if (!_textModified && xTextBox.Text != "")
            {
                //only give suggestions on last word
                var allText = xTextBox.Text.Split(' ');
                var lastWord = "";
                if (allText.Length > 0)
                {
                    lastWord = allText[allText.Length - 1];
                }

                if (_dataset == null)
                {
                    OperatorScript.Init();
                }

                var suggestions = _dataset?.Where(x => x.StartsWith(lastWord)).ToList();

                Suggestions.ItemsSource = suggestions;

                var numSug = suggestions.Count;

                if (numSug > 0)
                {
                    SuggestionsPopup.IsOpen = true;
                    SuggestionsPopup.Visibility = Visibility.Visible;
                }
                else
                {
                    SuggestionsPopup.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                SuggestionsPopup.IsOpen = false;
                SuggestionsPopup.Visibility = Visibility.Collapsed;
            }

            _textModified = false;
        }

        private void Suggestions_OnItemClick(object sender, ItemClickEventArgs e)
        {
            //get selected item
            var selectedItem = e.ClickedItem.ToString();
            _textModified = true;

            //only change last word to new text
            var currentText = xTextBox.Text.Split(' ');
            var keepText = "";
            if (currentText.Length > 1)
            {
                var lastWordLength = currentText[currentText.Length - 1].Length;
                keepText = xTextBox.Text.Substring(0, xTextBox.Text.Length - lastWordLength);
            }

            //if it is function, set up sample inputs
            var numInputs = OperatorScript.GetAmountInputs(selectedItem);
            var functionEnding = " ";
            var offset = 1;
            if (numInputs != null)
            {
                functionEnding = "(";
                offset = 2;
                for (var i = 0; i < numInputs; i++)
                {
                    functionEnding = functionEnding + "_, ";
                }
                //delete last comma and space and add ending paranthesis
                functionEnding = functionEnding.Substring(0, functionEnding.Length - 2) + ")";
            }

            xTextBox.Text = keepText + selectedItem + functionEnding;
            xTextBox.Focus(FocusState.Pointer);
            moveCursorToEnd((keepText + selectedItem).Length + offset);

            SuggestionsPopup.IsOpen = false;
            SuggestionsPopup.Visibility = Visibility.Collapsed;
        }


        private void UIElement_OnDragStarting(UIElement sender, DragStartingEventArgs args)
        {
            //Todo: find a better way to make doc controller for non text
            var output = (sender as FrameworkElement).DataContext as ReplLineViewModel;
            var outputData = output.Value;
            var postitNote = new RichTextNote(text: outputData.ToString()).Document;


            //TODO: get collection view model
            var collection = MainPage.Instance.MainDocument.GetDataDocument();


            //Todo: get real point
            var where = new Point(0, 0);


            //  Actions.DisplayDocument(ViewModel, postitNote, where);
        }
    }
}
