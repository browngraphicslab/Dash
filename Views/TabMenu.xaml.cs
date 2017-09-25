using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using DashShared;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class TabMenu : UserControl
    {
        private static TabMenu _instance;
        public static TabMenu Instance => _instance ?? (_instance = new TabMenu());


        public SearchView SearchView { get; private set; }
        public static CollectionFreeformView AddsToThisCollection = MainPage.Instance.GetMainCollectionView().CurrentView as CollectionFreeformView;

        private TabMenu()
        {
            this.InitializeComponent();
            this.MakeView();
        }

        private void MakeView()
        {
            var divide = OperationCreationHelper.Operators["Divide"].OperationDocumentConstructor.Invoke();
            var union = OperationCreationHelper.Operators["Union"].OperationDocumentConstructor.Invoke();
            var intersection = OperationCreationHelper.Operators["Intersection"].OperationDocumentConstructor.Invoke();
            var zip = OperationCreationHelper.Operators["Zip"].OperationDocumentConstructor.Invoke();
            var uriToImage = OperationCreationHelper.Operators["UriToImage"].OperationDocumentConstructor.Invoke();
            var map = OperationCreationHelper.Operators["Map"].OperationDocumentConstructor.Invoke();
            var api = OperationCreationHelper.Operators["Api"].OperationDocumentConstructor.Invoke();
            var concat = OperationCreationHelper.Operators["Concat"].OperationDocumentConstructor.Invoke();
            var docAppend = OperationCreationHelper.Operators["Append"].OperationDocumentConstructor.Invoke();
            var filter = OperationCreationHelper.Operators["Filter"].OperationDocumentConstructor.Invoke();
            var compound = OperationCreationHelper.Operators["Compound"].OperationDocumentConstructor.Invoke();

            var docfields = new Dictionary<KeyController, FieldModelController>()
            {
                [KeyStore.TitleKey] = new TextFieldModelController("Document")
            };
            var blankDocument = new DocumentController(docfields, DocumentType.DefaultType);
            var layout = new FreeFormDocument(new List<DocumentController>(), new Point(0,0), new Size(200,200)).Document;
            blankDocument.SetActiveLayout(layout, true, true);

            var colfields = new Dictionary<KeyController, FieldModelController>
            {
                [DocumentCollectionFieldModelController.CollectionKey] =
                    new DocumentCollectionFieldModelController(),
                [KeyStore.TitleKey] = new TextFieldModelController("Collection")
            };
            var colDoc = new DocumentController(colfields, DocumentType.DefaultType);
            colDoc.SetActiveLayout(
                new CollectionBox(
                    new ReferenceFieldModelController(colDoc.GetId(),
                        DocumentCollectionFieldModelController.CollectionKey), 0,0,200,200).Document, true, true);

            DocumentController postitNote = new NoteDocuments.RichTextNote(NoteDocuments.PostitNote.DocumentType).Document;
            postitNote.SetField(KeyStore.TitleKey, new TextFieldModelController("Note"), true);

            var all = new ObservableCollection<DocumentController>
            {
                blankDocument,
                postitNote,
                colDoc,
                divide,
                union,
                intersection,
                zip,
                filter,
                api, 
                concat,
                docAppend,
                compound,
                map,
            };

            xMainGrid.Children.Add(SearchView = new SearchView(new SearchCategoryItem("∀", "ALL", all)));
        }

        public void SetTextBoxFocus()
        {
            SearchView?.SetTextBoxFocus();
        }

        private void XMainGrid_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            MainPage.Instance.xCanvas.Children.Remove(Instance);
        }

        public static void ShowAt(Canvas canvas, Point position)
        {
            if (Instance != null)
            {
                if (!canvas.Children.Contains(Instance))
                {
                    canvas.Children.Add(Instance);
                }
                Canvas.SetLeft(Instance, position.X);
                Canvas.SetTop(Instance, position.Y);
                Instance.SearchView.SetNoSelection();
            }
        }
    }
}
