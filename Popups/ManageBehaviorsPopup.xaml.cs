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

namespace Dash.Popups
{
    public sealed partial class ManageBehaviorsPopup : DashPopup
    {
        private TaskCompletionSource<List<OperatorController>> _tcs;

        public ManageBehaviorsPopup() => InitializeComponent();

        public void SetHorizontalOffset(double offset)
        {
            xBehaviorsPopup.HorizontalOffset = offset;
        }

        public void SetVerticalOffset(double offset)
        {
            xBehaviorsPopup.VerticalOffset = offset;
        }

        public FrameworkElement Self() => this;

        private void OnOpened(object sender, object e)
        {
            //xComboBox.SelectedItem = null;
        }

        public Task<List<OperatorController>> OpenAsync()
        {
            _tcs = new TaskCompletionSource<List<OperatorController>>();
            xBehaviorsPopup.IsOpen = true;
            MainPage.Instance.SetUpPopup(this);
            return _tcs.Task;
        }
    }
}
