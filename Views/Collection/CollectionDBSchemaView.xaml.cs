using DashShared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using static Dash.CollectionDBSchemaHeader;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionDBSchemaView : ICollectionView
    {
        private DocumentController _parentDocument;

        private Dictionary<KeyController, HashSet<TypeInfo>> _typedHeaders;

        /// <summary>
        ///     Each element in the list renders a column in the records list view
        /// </summary>
        private ObservableCollection<CollectionDBSchemaColumnViewModel> ColumnViewModels { get; }

        /// <summary>
        ///     The observable collection of all documents displayed in the schema, a pointer to this is held by every
        ///     columnviewmodel so be very careful about changing it (i.e. don't add a setter)
        /// </summary>
        private ObservableCollection<DocumentController> CollectionDocuments { get; }

        ////bcz: this field isn't used, but if it's not here Field items won't be updated when they're changed.  Why???????
        //public ObservableCollection<CollectionDBSchemaRecordViewModel> Records { get; set; } =
        //    new ObservableCollection<CollectionDBSchemaRecordViewModel>();

        public ObservableCollection<HeaderViewModel> SchemaHeaders { get; }

        public CollectionViewModel ViewModel { get; set; }

        public DocumentController ParentDocument
        {
            get => _parentDocument;
            set
            {
                _parentDocument = value;
                if (value != null)
                    if (ParentDocument.GetField(CollectionDBView.FilterFieldKey) == null)
                        ParentDocument.SetField(CollectionDBView.FilterFieldKey, new KeyController(), true);
            }
        }

        public UserControl UserControl => this;

        public CollectionDBSchemaView()
        {
            InitializeComponent();
        }

        private void AddRow_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            // Add a new document to the schema view
            ViewModel.AddDocument(Util.BlankNote());
            e.Handled = true;
        }

        private void AddColumn_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var newHvm = new HeaderViewModel
            {
                SchemaView = this,
                SchemaDocument = ParentDocument,
                Width = 150,
                FieldKey = new KeyController("New Field", Guid.NewGuid().ToString())
            };
            SchemaHeaders.Add(newHvm);

            //var cvm = new CollectionDBSchemaColumnViewModel(newHvm.FieldKey, CollectionDocuments, newHvm);
            //ColumnViewModels.Add(cvm);
            //cvm.PropertyChanged += Cvm_PropertyChanged;

            e.Handled = true;
        }

        public void SetDropIndicationFill(Brush fill)
        {
        }
    }
}
