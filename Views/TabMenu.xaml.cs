using System;
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
            this.LostFocus += OnLostFocus;
        }

        private void OnLostFocus(object sender, RoutedEventArgs routedEventArgs)
        {
            MainPage.Instance.xCanvas.Children.Remove(this);
        }

        private void MakeView()
        {
            var add = OperationCreationHelper.Operators["Add"].OperationDocumentConstructor;
            var subtract = OperationCreationHelper.Operators["Subtract"].OperationDocumentConstructor;
            var multiply = OperationCreationHelper.Operators["Multiply"].OperationDocumentConstructor;
            var divide = OperationCreationHelper.Operators["Divide"].OperationDocumentConstructor;
            var union = OperationCreationHelper.Operators["Union"].OperationDocumentConstructor;
            var intersection = OperationCreationHelper.Operators["Intersection"].OperationDocumentConstructor;
            var zip = OperationCreationHelper.Operators["Zip"].OperationDocumentConstructor;
            var uriToImage = OperationCreationHelper.Operators["UriToImage"].OperationDocumentConstructor;
            var map = OperationCreationHelper.Operators["Map"].OperationDocumentConstructor;
            var api = OperationCreationHelper.Operators["Api"].OperationDocumentConstructor;
            var concat = OperationCreationHelper.Operators["Concat"].OperationDocumentConstructor;
            var docAppend = OperationCreationHelper.Operators["Append"].OperationDocumentConstructor;
            var filter = OperationCreationHelper.Operators["Filter"].OperationDocumentConstructor;
            var compound = OperationCreationHelper.Operators["Compound"].OperationDocumentConstructor;
            Func<DocumentController> createBlankDocument = BlankDoc;
            Func<DocumentController> createBlankCollection = BlankCollection;
            Func<DocumentController> createBlankPostitNote = BlankNote;

            var all = new ObservableCollection<Func<DocumentController>>
            {
                createBlankDocument,
                createBlankPostitNote,
                createBlankCollection,
                add,
                subtract,
                multiply,
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

        public DocumentController BlankDoc()
        {
            var docfields = new Dictionary<KeyController, FieldModelController>()
            {
                [KeyStore.TitleKey] = new TextFieldModelController("Document")
            };
            var blankDocument = new DocumentController(docfields, DocumentType.DefaultType);
            var layout = new FreeFormDocument(new List<DocumentController>(), new Point(0, 0), new Size(200, 200)).Document;
            blankDocument.SetActiveLayout(layout, true, true);
            return blankDocument;
        }

        public DocumentController BlankCollection()
        {
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
                        DocumentCollectionFieldModelController.CollectionKey), 0, 0, 200, 200).Document, true, true);
            return colDoc;
        }

        public DocumentController BlankNote()
        {
            DocumentController postitNote = new NoteDocuments.RichTextNote(NoteDocuments.PostitNote.DocumentType).Document;
            postitNote.SetField(KeyStore.TitleKey, new TextFieldModelController("Note"), true);
            return postitNote;
        }

        public void SetTextBoxFocus()
        {
            SearchView?.SetTextBoxFocus();
        }

        private void XMainGrid_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            MainPage.Instance.xCanvas.Children.Remove(Instance);
        }

        public static void ShowAt(Canvas canvas, Point position, bool isTouch=false)
        {
            if (Instance != null)
            {
                if (!canvas.Children.Contains(Instance))
                {
                    canvas.Children.Add(Instance);
                }
                if (isTouch) Instance.SearchView.ConfigureForTouch();
                else Instance.SearchView.ConfigureForMouse();
                Canvas.SetLeft(Instance, position.X);
                Canvas.SetTop(Instance, position.Y);
                Instance.SearchView.SetNoSelection();
            }
        }
    }
}
