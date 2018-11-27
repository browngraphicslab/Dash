using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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
    public sealed partial class TextInputPopup : UserControl
    {
        public TextInputPopup()
        {
            InitializeComponent();
        }

        private TaskCompletionSource<(string, string)> _tcs;

        public Task<(string title, string functionString)> OpenAsync()
        {
            _tcs = new TaskCompletionSource<(string, string)>();

            MainPage.Instance.XGrid.Children.Add(this);

            return _tcs.Task;
        }

        private void Submit_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var title = XTitleTextBox.Text;
            var funcStart = XFuncStartBlock.Text;
            var funcBody = XFunctionTextBox.Text.Replace("\n", "\n\t");
            var funcEnd = XFuncEndBlock.Text;
            
            MainPage.Instance.XGrid.Children.Remove(this);

            _tcs.SetResult((title, $"{funcStart}\n\t{funcBody}\n{funcEnd}"));
        }

        private void Cancel_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            MainPage.Instance.XGrid.Children.Remove(this);
            _tcs.SetResult((null, null));
        }
    }
}
