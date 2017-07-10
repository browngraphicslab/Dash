using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class KeyValuePane : UserControl
    {
        public static readonly string DragPropertyKey = "key";

        private DocumentController _documentControllerDataContext;
        private ObservableCollection<KeyFieldContainer> ListItemSource { get; }

        public KeyValuePane()
        {
            InitializeComponent();
            ListItemSource = new ObservableCollection<KeyFieldContainer>();
            DataContextChanged += KeyValuePane_DataContextChanged;
        }

        public void SetDataContextToDocumentController(DocumentController documentToDisplay)
        {
            _documentControllerDataContext = documentToDisplay;
            DataContext = documentToDisplay; // this line fires data context changed
        }

        private void KeyValuePane_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext == null) return;

            SetListItemSourceToCurrentDataContext();
        }

        private void SetListItemSourceToCurrentDataContext()
        {
            ListItemSource.Clear();
            foreach (var keyFieldPair in _documentControllerDataContext.EnumFields())
                ListItemSource.Add(new KeyFieldContainer(keyFieldPair.Key, keyFieldPair.Value));
        }


        private void XKeyValueListView_OnDragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            Debug.WriteLine(e.Items.Count);
            var item = e.Items.FirstOrDefault();

            // item type has to be the same as ListItemSource item type
            if (item is KeyFieldContainer )
            {
                var container = item as KeyFieldContainer;
                e.Data.RequestedOperation = DataPackageOperation.Move;
                e.Data.Properties.Add(DragPropertyKey, container.Key);
            }
        }
    }

    /// <summary>
    /// A container which represents a single row in the list created by the <see cref="KeyValuePane"/>
    /// </summary>
    public class KeyFieldContainer
    {
        public Key Key { get; }
        public FieldModelController Controller { get; }

        public KeyFieldContainer(Key key, FieldModelController controller)
        {
            Key = key;
            Controller = controller;
        }
    }
}