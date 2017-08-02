using System;
using System.Collections.Generic;
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

namespace Dash.Views
{
    public sealed partial class ApiOpView : UserControl
    {
        private DocumentController _operatorDoc;
        private Dictionary<string, ApiProperty> headers;
        private Dictionary<string, ApiProperty> parameters;
        private Dictionary<string, ApiProperty> authHeaders;
        private Dictionary<string, ApiProperty> authParameters;

        public ApiOpView()
        {
            this.InitializeComponent();
            DataContextChanged += OnDataContextChanged;

        }

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var refToOp = args.NewValue as FieldReference;
            var doc = refToOp.GetDocumentController(null);
            _operatorDoc = doc;
            xGrid.Children.Add(ApiDocumentModel.MakeView(_operatorDoc, null, false));
        }
    }
}
