using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;
using Dash.Controllers;
using Dash.Models;
using DashShared;
using Microsoft.Extensions.DependencyInjection;
using RadialMenuControl.UserControl;
using Dash.Controllers.Operators;
using static Dash.Controllers.Operators.DBSearchOperatorFieldModelController;
using static Dash.NoteDocuments;

namespace Dash
{
    public static class Actions
    {
        

        public static void AddSearch(object o, DragEventArgs e)
        {

            var where = Util.GetCollectionFreeFormPoint(
                MainPage.Instance.xMainDocView.GetFirstDescendantOfType<CollectionFreeformView>(),

                e.GetPosition(MainPage.Instance));
            MainPage.Instance.AddGenericFilter(o, e);
        }


        public static void OnOperatorAdd(ICollectionView collection, DragEventArgs e)
        {
            MainPage.Instance.AddOperatorsFilter(collection, e);
        }

        public static void AddDocFromFunction(Func<DocumentController> documentCreationFunc)
        {
            var freeForm = TabMenu.AddsToThisCollection;

            if (freeForm == null)
            {
                return;
            }
            
            var searchView = TabMenu.Instance.SearchView;
            var transform = searchView.TransformToVisual(freeForm.xItemsControl.ItemsPanelRoot);
            Debug.Assert(transform != null);
            var translate = transform.TransformPoint(new Point());

            var opController = documentCreationFunc?.Invoke();

            // using this as a setter for the transform massive hack - LM
            var _ = new DocumentViewModel(opController)
            {
                GroupTransform = new TransformGroupData(translate, new Point(), new Point(1, 1))
            };

            if (opController != null)
            {
                freeForm.ViewModel.AddDocument(opController, null);
            }
        }

        public static void AddDocument(ICollectionView collection, DragEventArgs e)
        {
            var where = Util.GetCollectionFreeFormPoint(collection as CollectionFreeformView,
                e.GetPosition(MainPage.Instance));


            var newDocProto = new DocumentController(new Dictionary<KeyController, FieldControllerBase>(), DocumentType.DefaultType);
            newDocProto.SetField(KeyStore.AbstractInterfaceKey, new TextFieldModelController("Dynamic Doc API"), true);
            var newDoc = newDocProto.MakeDelegate();
            newDoc.SetActiveLayout(new FreeFormDocument(new List<DocumentController>(), where, new Size(400, 400)).Document, true, true);

            collection.ViewModel.AddDocument(newDoc, null);

            //DBTest.DBDoc.AddChild(newDoc);
        }

        public static void AddCollection(ICollectionView collection, DragEventArgs e)
        {
            var where = Util.GetCollectionFreeFormPoint(collection as CollectionFreeformView,
                e.GetPosition(MainPage.Instance));

            var cnote = new CollectionNote(where, CollectionView.CollectionViewType.Freeform);
            cnote.Document.SetField(CollectionNote.CollectedDocsKey, new DocumentCollectionFieldModelController(), true);
            var newDoc = cnote.Document;
            
            collection.ViewModel.AddDocument(newDoc, null);
            //DBTest.DBDoc.AddChild(newDoc);
        }

        public static async void ImportFields(ICollectionView collection, DragEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.List;
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            StorageFile storageFile = await openPicker.PickSingleFileAsync();
            if (storageFile != null)
            {
                var where = Util.GetCollectionFreeFormPoint(collection as CollectionFreeformView,
                    e.GetPosition(MainPage.Instance));
                var fields = new Dictionary<KeyController, FieldControllerBase>()
                {
                    [KeyStore.ActiveLayoutKey] =
                    new DocumentFieldModelController(
                        new FreeFormDocument(new List<DocumentController>(), where, new Size(100, 100)).Document)
                };
                var doc = new DocumentController(fields, DocumentType.DefaultType);
                var key = new KeyController(Guid.NewGuid().ToString(), storageFile.DisplayName);
                var layout = doc.GetActiveLayout();
                var data =
                    layout.Data.GetDereferencedField(KeyStore.DataKey, null) as DocumentCollectionFieldModelController;
                if (storageFile.IsOfType(StorageItemTypes.Folder))
                {
                    //Add collection of new documents?
                }
                else if (storageFile.IsOfType(StorageItemTypes.File))
                {
                    switch (storageFile.FileType)
                    {
                        case ".jpg":
                        case ".png":
                            var imgController = new ImageFieldModelController();
                            var btmp = new BitmapImage(new Uri(storageFile.Path, UriKind.Absolute));
                            imgController.Data = btmp;
                            doc.SetField(key, imgController, true);
                            var imgBox = new ImageBox(new DocumentReferenceFieldController(doc.GetId(), key), 0, 0, btmp.PixelWidth, btmp.PixelHeight);
                            data?.AddDocument(imgBox.Document);
                            break;
                        case ".txt":
                            var text = await FileIO.ReadTextAsync(storageFile);
                            var txtController = new TextFieldModelController(text);
                            doc.SetField(key, txtController, true);
                            var txtBox = new TextingBox(new DocumentReferenceFieldController(doc.GetId(), key), 0, 0, 200, 200);
                            data?.AddDocument(txtBox.Document);
                            break;
                        case ".doc":
                            // TODO implement for more file types
                            break;
                        default:
                            break;
                    }
                }
                collection.ViewModel.AddDocument(doc, null);
            }
            
        }
        
        public static void AddCollectionTEST(ICollectionView collection, DragEventArgs e)
        {
            //Get transformed position of drop event
            var where = Util.GetCollectionFreeFormPoint(collection as CollectionFreeformView, e.GetPosition(MainPage.Instance));

            //Make first collection
            List<DocumentController> numbers = new List<DocumentController>();
            for (int i = 0; i < 6; ++i)
            {
                numbers.Add(new Numbers().Document);
            }
            var fields = new Dictionary<KeyController, FieldControllerBase>
            {
                [KeyStore.CollectionKey] = new DocumentCollectionFieldModelController(numbers)
            };

            var collectionDocument = new DocumentController(fields, DashConstants.TypeStore.CollectionDocument);
            var layoutDocument = new CollectionBox(new DocumentReferenceFieldController(collectionDocument.GetId(),
                    KeyStore.CollectionKey), 0, 0, 400, 400).Document;
            collectionDocument.SetActiveLayout(layoutDocument, true, true); 


            // Make second collection
            var numbers2 = new Numbers().Document;
            var twoImages2 = new TwoImages(false).Document;
            var fields2 = new Dictionary<KeyController, FieldControllerBase>
            {
                [KeyStore.CollectionKey] =
                new DocumentCollectionFieldModelController(new[] {numbers2, twoImages2})
            };
            var col2 = new DocumentController(fields2, DashConstants.TypeStore.CollectionDocument);
            var layoutDoc2 =
                new CollectionBox(new DocumentReferenceFieldController(col2.GetId(),
                        KeyStore.CollectionKey), 0, 0, 400, 400).Document;
            col2.SetActiveLayout(layoutDoc2, true, true); 


            //Display collections
            DisplayDocument(collection, col2, where);
            DisplayDocument(collection, collectionDocument, where);
        }

        public static void DisplayDocument(ICollectionView collectionView, DocumentController docController, Point? where = null)
        {
            if (where != null)
            {
                var h = docController.GetHeightField().Data; 
                var w = docController.GetWidthField().Data;

                var pos = (Point)where;
                docController.GetPositionField().Data = double.IsNaN(h) || double.IsNaN(w) ? pos : new Point(pos.X - w / 2, pos.Y - h / 2); 
            }
            collectionView.ViewModel.AddDocument(docController, null); 
            //DBTest.DBDoc.AddChild(docController);
        }

        public static void AddDocuments(ICollectionView collectionView, DragEventArgs e)
        {
            var where = Util.GetCollectionFreeFormPoint(collectionView as CollectionFreeformView, e.GetPosition(MainPage.Instance));

            //Make second collection
            var numbers2 = new Numbers().Document;
            var fields2 = new Dictionary<KeyController, FieldControllerBase>
            {
                [KeyStore.CollectionKey] =
                new DocumentCollectionFieldModelController(new[]
                    {numbers2})
            };
            var col2 = new DocumentController(fields2, DashConstants.TypeStore.CollectionDocument);
            var layoutDoc2 =
                new CollectionBox(new DocumentReferenceFieldController(col2.GetId(),
                        KeyStore.CollectionKey)).Document;
            var layoutController2 = new DocumentFieldModelController(layoutDoc2);
            col2.SetField(KeyStore.ActiveLayoutKey, layoutController2, true);
            col2.SetField(KeyStore.LayoutListKey,
                new DocumentCollectionFieldModelController(new List<DocumentController> { layoutDoc2 }), true);

            //Display collections
            DisplayDocument(collectionView, col2, where);

            DisplayDocument(collectionView, new InkDoc().Document, where);
            DisplayDocument(collectionView, new Numbers().Document, where);

            DisplayDocument(collectionView, new XampleText().Document, where);

            /*
            var ndb = new DBTest();
            for (int i = 0; i < ndb.Documents.Count; i++)
            {
                DisplayDocument(collectionView, ndb.Documents[i], where);
                MainPage.Instance.UpdateLayout();
            }*/
        }

        public static void AddNotes(ICollectionView collectionView, DragEventArgs e)
        {
            var where = Util.GetCollectionFreeFormPoint(collectionView as CollectionFreeformView, e.GetPosition(MainPage.Instance));
            DocumentController postitNote = new NoteDocuments.RichTextNote(NoteDocuments.PostitNote.DocumentType).Document;
            DisplayDocument(collectionView, postitNote, where);
        }

        public static async void OpenFilePickerForImport(ICollectionView collectionView, DragEventArgs e)
        {
            var where = Util.GetCollectionFreeFormPoint(collectionView as CollectionFreeformView, e.GetPosition(MainPage.Instance));
            FileOpenPicker picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            var results = await picker.PickMultipleFilesAsync();
            FileDropHelper.HandleDropOnCollection(results, collectionView, where);
        }

        #region Ink Commands

        public static void SetTouchInput(object obj)
        {
            GlobalInkSettings.InkInputType = CoreInputDeviceTypes.Touch;
        }

        public static void SetPenInput(object obj)
        {
            GlobalInkSettings.InkInputType = CoreInputDeviceTypes.Pen;
        }

        public static void SetMouseInput(object obj)
        {
            GlobalInkSettings.InkInputType = CoreInputDeviceTypes.Mouse;
        }

        public static void SetNoInput(object obj)
        {
            GlobalInkSettings.InkInputType = CoreInputDeviceTypes.None;
        }


        public static void ToggleSelectionMode(object o)
        {
            GlobalInkSettings.StrokeType = GlobalInkSettings.StrokeTypes.Selection;
        }

        public static void ChoosePen(object o)
        {
            GlobalInkSettings.StrokeType = GlobalInkSettings.StrokeTypes.Pen;
        }

        public static void ChoosePencil(object o)
        {
            GlobalInkSettings.StrokeType = GlobalInkSettings.StrokeTypes.Pencil;
        }

        public static void ChooseEraser(object o)
        {
            GlobalInkSettings.StrokeType = GlobalInkSettings.StrokeTypes.Eraser;
        }
        
        #endregion
    }
}
