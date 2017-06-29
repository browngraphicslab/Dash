using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.FileProperties;
using Windows.UI;
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
    public sealed partial class InterfaceBuilder : WindowTemplate
    {

        private List<GuideLineViewModel> _guides = new List<GuideLineViewModel>();

        /// <summary>
        /// The document view model of the document which is being edited
        /// </summary>
        private DocumentViewModel _documentViewModel;

        /// <summary>
        /// the document controller of the document which is being edited. This contains the fields for all the data
        /// that is going to be displayed
        /// </summary>
        private DocumentController _documentController;

        /// <summary>
        /// The document view of the document which is being edited
        /// </summary>
        private DocumentView _documentView;

        /// <summary>
        /// The document controller of the layout document for the document which is being edited. The fields in this
        /// document are courtesy documents used to display data from fields in the _documentController
        /// </summary>
        private DocumentCollectionFieldModelController _layoutDocumentCollection;

        private ObservableCollection<KeyValuePair<Key, string>> _keyValuePairs;

        public InterfaceBuilder(DocumentViewModel viewModel, int width = 800, int height = 500)
        {
            this.InitializeComponent();
            Width = width;
            Height = height;

            // set the view model, document model and view variables
            _documentViewModel = new DocumentViewModel(viewModel.DocumentController) // create a new documentViewModel because  the view in the editor is different from the view in the workspace
            {
                IsDetailedUserInterfaceVisible = false,
                IsMoveable = false
            };
            _documentController = viewModel.DocumentController;

            _keyValuePairs = new ObservableCollection<KeyValuePair<Key, string>>();
            foreach (KeyValuePair<Key, FieldModelController> pair in _documentController.EnumFields())
            {
                _keyValuePairs.Add(new KeyValuePair<Key, string>(pair.Key, pair.Value.ToString()));
            }
            xKeyValueListView.ItemsSource = _keyValuePairs;


            // get the layout field on the document being displayed
            var layoutField = viewModel.DocumentController.GetField(DashConstants.KeyStore.LayoutKey) as DocumentFieldModelController;
            // get the layout document controller from the layout field
            var layoutDocumentController = layoutField?.Data;
            // get the documentCollectionFieldModelController from the layout document controller
            _layoutDocumentCollection = layoutDocumentController?.GetField(DashConstants.KeyStore.DataKey) as DocumentCollectionFieldModelController;
            if (_layoutDocumentCollection == null)
            {
                throw new NotImplementedException("we can't edit views without a layout yet");
            }


            _documentView = new DocumentView(_documentViewModel);
            xDocumentHolder.Children.Add(_documentView);

            _documentView.DragOver += DocumentViewOnDragOver;
            _documentView.Drop += DocumentViewOnDrop;
            _documentView.AllowDrop = true;


            ApplyEditable();
        }




        private void ApplyEditable()
        {
            List<FrameworkElement> editableElements = new List<FrameworkElement>();

            // iterate over all the documents which define views
            foreach (var layoutDocument in _layoutDocumentCollection.GetDocuments())
            {
                // get the controller for the data field that the layout document is parameterizing a view for
                var referenceToData = layoutDocument.GetField(DashConstants.KeyStore.DataKey) as ReferenceFieldModelController;
                Debug.Assert(referenceToData != null, "The layout document customarily defines the data key as a reference to the data field that it is defining a layout for");

                // use the layout document to generate a UI
                var fieldView = layoutDocument.MakeViewUI();

                var editableBorder = new EditableFieldFrame(layoutDocument.GetId())
                {
                    EditableContent = fieldView.FirstOrDefault(),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top
                };

                editableBorder.FieldSizeChanged += EditableBorderOnFieldSizeChanged;
                editableBorder.FieldPositionChanged += EditableBorderOnFieldPositionChanged;
                editableElements.Add(editableBorder);
            }

            _documentView.SetUIElements(editableElements);
        }


        private void EditableBorderOnFieldPositionChanged(object sender, double deltaX, double deltaY)
        {
            var editableFieldFrame = sender as EditableFieldFrame;
            Debug.Assert(editableFieldFrame != null);

            var layoutDocumentId = editableFieldFrame.DocumentId;

            var editedLayoutDocument = _layoutDocumentCollection.GetDocuments().FirstOrDefault(doc => doc.GetId() == layoutDocumentId);
            Debug.Assert(editedLayoutDocument != null);
            var translateController =
                editedLayoutDocument.GetField(DashConstants.KeyStore.PositionFieldKey) as PointFieldModelController;
            Debug.Assert(translateController != null);

            var p = translateController.Data;
            var newTranslation = new Point(p.X + deltaX, p.Y + deltaY);
            translateController.Data = newTranslation;

            editableFieldFrame.ApplyContentTranslationToFrame(newTranslation);
        }

        private void EditableBorderOnFieldSizeChanged(object sender, double newWidth, double newHeight)
        {
            var editableFieldFrame = sender as EditableFieldFrame;
            Debug.Assert(editableFieldFrame != null);

            var layoutDocumentId = editableFieldFrame.DocumentId;

            var editedLayoutDocument = _layoutDocumentCollection.GetDocuments().FirstOrDefault(doc => doc.GetId() == layoutDocumentId);
            Debug.Assert(editedLayoutDocument != null);
            var widthFieldController =
                editedLayoutDocument.GetField(DashConstants.KeyStore.WidthFieldKey) as NumberFieldModelController;
            var heightFieldController =
                editedLayoutDocument.GetField(DashConstants.KeyStore.HeightFieldKey) as NumberFieldModelController;
            Debug.Assert(widthFieldController != null && heightFieldController != null);

            heightFieldController.Data = newHeight;
            widthFieldController.Data = newWidth;
        }

        private void ApplyEditableOnTapped(object sender, TappedRoutedEventArgs e)
        {
            ApplyEditable();
        }

        private void XKeyValueListView_OnDragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            Debug.WriteLine(e.Items.Count);
            var pair = e.Items[0] is KeyValuePair<Key, string> ? (KeyValuePair<Key, string>)e.Items[0] : new KeyValuePair<Key, string>();
            Debug.WriteLine(pair.Key.Name);
            e.Data.RequestedOperation = DataPackageOperation.Move;
            Debug.WriteLine(_documentController.Fields[pair.Key].GetType());
            e.Data.Properties.Add("key", pair.Key);
            //e.Items.Insert(0, );
        }

        private void AddField(Key key)
        {
            //ReferenceFieldModel reference = _documentController.Fields[key].InputReference;
            //var box = new CourtesyDocuments.TextingBox(reference);
            //var doc = box.Document;

            ////var imModel = new ImageFieldModel(new Uri("ms-appx://Dash/Assets/cat.jpg"));
            ////var imModel2 = new ImageFieldModel(new Uri("ms-appx://Dash/Assets/cat2.jpeg"));
            ////var tModel = new TextFieldModel("Hello World!");
            ////var fields = new Dictionary<Key, FieldModel>
            ////{
            ////    [TextFieldKey] = tModel,
            ////    [Image1FieldKey] = imModel,
            ////    [Image2FieldKey] = imModel2
            ////};


            //var imBox1 = new CourtesyDocuments.ImageBox(new ReferenceFieldModel(_documentController.GetId(), Image1FieldKey)).Document;
            //var imBox2 =
            //    new CourtesyDocuments.ImageBox(new ReferenceFieldModel(_documentController.GetId(), Image2FieldKey))
            //        .Document;
            //var tBox =
            //    new CourtesyDocuments.TextingBox(new ReferenceFieldModel(_documentController.GetId(), TextFieldKey))
            //        .Document;


            //var documentFieldModel =
            //    new DocumentCollectionFieldModel(new DocumentModel[]
            //        {tBox.DocumentModel, imBox1.DocumentModel, imBox2.DocumentModel});
            //var documentFieldModelController = new DocumentCollectionFieldModelController(documentFieldModel);
            //ContentController.AddModel(documentFieldModel);
            //ContentController.AddController(documentFieldModelController);
            //_documentController.SetField(DashConstants.KeyStore.DataKey, documentFieldModelController, true);

            //var genericCollection = new CourtesyDocuments.GenericCollection(documentFieldModel).Document;
            //genericCollection.SetField(DashConstants.KeyStore.WidthFieldKey,
            //    new NumberFieldModelController(new NumberFieldModel(800)), true);

            //var layoutDoc = genericCollection.DocumentModel;
            ////SetLayoutForDocument(genericCollection.DocumentModel);

            //var documentFieldModel2 = new DocumentModelFieldModel(layoutDoc);
            //var layoutController = new DocumentFieldModelController(documentFieldModel2);
            //ContentController.AddModel(documentFieldModel2);
            //ContentController.AddController(layoutController);
            //_documentController.SetField(DashConstants.KeyStore.LayoutKey, layoutController, false);



            ////_layoutDocumentCollection.AddDocument(_documentController.Fields[key].);
        }

        private void DocumentViewOnDragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Move;
        }

        private void DocumentViewOnDrop(object sender, DragEventArgs e)
        {
            Key key = e.Data.Properties["key"] as Key;
            var fieldModel = _documentController.GetField(key).FieldModel;
            if (fieldModel is TextFieldModel)
            {
                CourtesyDocuments.TextingBox box = new CourtesyDocuments.TextingBox(new ReferenceFieldModel(_documentController.GetId(), key));
                _layoutDocumentCollection.AddDocument(box.Document);
            } else if (fieldModel is ImageFieldModel)
            {
                CourtesyDocuments.ImageBox box = new CourtesyDocuments.ImageBox(new ReferenceFieldModel(_documentController.GetId(), key));
                _layoutDocumentCollection.AddDocument(box.Document);
            }
            ApplyEditable();
        }

        private void XKeyValueListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
