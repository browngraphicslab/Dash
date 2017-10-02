using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
    public sealed partial class CollectionDBSchemaRecord : UserControl
    {
        static int count = 0;
        public CollectionDBSchemaRecord()
        {
            count++;
            Debug.WriteLine("Created " + count);
            this.InitializeComponent();
        }

        private void CollectionDBSchemaRecordField_Tapped(object sender, TappedRoutedEventArgs e)
        {

        }
    }

    public class CollectionDBSchemaRecordViewModel
    {
        public CollectionDBSchemaRecordViewModel(IEnumerable<CollectionDBSchemaRecordFieldViewModel> fields)
        {
            RecordFields = new ObservableCollection<Views.CollectionDBSchemaRecordFieldViewModel>(fields);
        }
        public DocumentController Document;
        public ObservableCollection<CollectionDBSchemaRecordFieldViewModel> RecordFields { get; set; }
    }
}
