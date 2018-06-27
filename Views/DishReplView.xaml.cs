using Dash.Models.DragModels;
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
       


        private void XTextBoxOnLostFocus(object sender, RoutedEventArgs routedEventArgs)
        {
            Window.Current.CoreWindow.KeyUp -= CoreWindowOnKeyUp;
        }

        private void XTextBoxOnGotFocus(object sender, RoutedEventArgs routedEventArgs)
        {
            Window.Current.CoreWindow.KeyUp += CoreWindowOnKeyUp;
        }

        private void CoreWindowOnKeyUp(CoreWindow sender, KeyEventArgs args)
        {
            switch (args.VirtualKey)
            {
                case VirtualKey.Up:
                    _currentHistoryIndex++;
                    xTextBox.Text = ViewModel.Items.ElementAt(Math.Max(0,ViewModel.Items.Count - _currentHistoryIndex))?.LineText?.Substring(3) ?? xTextBox.Text;
                    break;
                case VirtualKey.Down:
                    _currentHistoryIndex = Math.Max(1, _currentHistoryIndex - 1);
                    xTextBox.Text = ViewModel.Items.ElementAt(Math.Max(0, ViewModel.Items.Count - _currentHistoryIndex))?.LineText?.Substring(3) ?? xTextBox.Text;
                    break;
            }
        }


        private void TextInputKeyDown(object sender, KeyRoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (e.OriginalKey == VirtualKey.Enter)
            {
                _currentHistoryIndex = 0;
                var currentText = textBox.Text;
                textBox.Text = "";
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
        }

        private void UIElement_OnDragStarting(UIElement sender, DragStartingEventArgs args)
        {
            var line = (sender as FrameworkElement).DataContext as ReplLineViewModel;

            var test = MainPage.Instance.MainDocument.GetDataDocument();
        }
    }
}
