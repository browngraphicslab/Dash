using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
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
        private DSL _dsl;

        public DishReplView()
        {
            this.InitializeComponent();
            this.DataContext = new DishReplViewModel();
            _dsl = new DSL(ScriptState.ContentAware(), true);
        }

        private void TextInputKeyDown(object sender, KeyRoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (e.OriginalKey == VirtualKey.Enter)
            {
                var currentText = textBox.Text;
                textBox.Text = "";
                var returnValue = _dsl.Run(currentText) as TextController;
                ViewModel.Items.Add(new ReplLineViewModel(currentText, returnValue.Data, new TextController("test")));
                xScrollViewer.ScrollToVerticalOffset(int.MaxValue);
            }
        }
    }
}
