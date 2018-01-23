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
using DashShared.Models;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class SearchOperatorView : UserControl
    {

        /// <summary>
        /// The document which has a field containing the SearchOperator that this view is associated with
        /// </summary>
        private DocumentController _operatorDoc;

        public SearchOperatorView()
        {
            this.InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Loaded += SearchOperatorView_Loaded;
        }

        private void SearchOperatorView_Loaded(object sender, RoutedEventArgs e)
        {
            xSearchBox.ShowCollectionDrag(false);
        }

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            // datacontext is a reference to the operator field
            var refToOp = DataContext as FieldReference;

            // get the document containing the operator
            _operatorDoc = refToOp?.GetDocumentController(null);

            // listen for when the input collection is changed
            _operatorDoc?.AddFieldUpdatedListener(SearchOperatorController.InputCollection, OnInputCollectionChanged);
            _operatorDoc?.AddFieldUpdatedListener(SearchOperatorController.Text, OnTextFieldChanged);
        }

        private void OnTextFieldChanged(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            //var dargs = (DocumentController.DocumentFieldUpdatedEventArgs)args;
            //var tfmc = dargs.NewValue.DereferenceToRoot<TextController>(null);
            //XTextFieldBox.Text = ContentController<FieldModel>.GetController<KeyController>(tfmc.Data).Name;
        }

        private void OnInputCollectionChanged(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            //var dargs = (DocumentController.DocumentFieldUpdatedEventArgs)args;
            //InputCollection = dargs.NewValue.DereferenceToRoot<ListController<DocumentController>>(null);
            //_allHeaders = Util.GetTypedHeaders(InputCollection); // TODO update the headers when a document is added to the input collection!
        }

    }
}
