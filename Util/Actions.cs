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
using static Dash.NoteDocuments;
using Dash.Views.Document_Menu;

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
        public static void AddDocument(ICollectionView collection, DragEventArgs e)
        {
            var where = Util.GetCollectionFreeFormPoint(collection as CollectionFreeformView,
                e.GetPosition(MainPage.Instance));


            //var newDocProto = new DocumentController(new Dictionary<KeyController, FieldControllerBase>(), DocumentType.DefaultType);
            //newDocProto.SetField(KeyStore.AbstractInterfaceKey, new TextFieldModelController("Dynamic Doc API"), true);
            //var newDoc = newDocProto.MakeDelegate();
            //newDoc.SetActiveLayout(new FreeFormDocument(new List<DocumentController>(), where, new Size(400, 400)).Document, true, true);
            //newDoc.SetActiveLayout(new FreeFormDocument(new List<DocumentController>(), where, new Size(400, 400)).Document, true, true);

            //collection.ViewModel.AddDocument(newDoc, null);
            collection.ViewModel.AddDocument(Util.BlankDocWithPosition(where), null);

            //DBTest.DBDoc.AddChild(newDoc);
        }

        public static void AddCollection(ICollectionView collection, DragEventArgs e)
        {
            var where = Util.GetCollectionFreeFormPoint(collection as CollectionFreeformView,
                e.GetPosition(MainPage.Instance));

            var cnote = new CollectionNote(where, CollectionView.CollectionViewType.Freeform);
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
                        new FreeFormDocument(new List<DocumentController>(), where, new Size(100, 100)).Document
                };
                var doc = new DocumentController(fields, DocumentType.DefaultType);
                var key = new KeyController(Guid.NewGuid().ToString(), storageFile.DisplayName);
                var layout = doc.GetActiveLayout();
                var data =
                    layout.GetDereferencedField(KeyStore.DataKey, null) as ListController<DocumentController>;
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
                            var imgController = new ImageController();
                            imgController.Data = new Uri(storageFile.Path, UriKind.Absolute);
                            doc.SetField(key, imgController, true);
                            var imgBox = new ImageBox(new DocumentReferenceController(doc.GetId(), key));
                            data?.Add(imgBox.Document);
                            break;
                        case ".txt":
                            var text = await FileIO.ReadTextAsync(storageFile);
                            var txtController = new TextController(text);
                            doc.SetField(key, txtController, true);
                            var txtBox = new TextingBox(new DocumentReferenceController(doc.GetId(), key), 0, 0, 200, 200);
                            data?.Add(txtBox.Document);
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

        public static void DisplayDocument(BaseCollectionViewModel collectionViewModel, DocumentController docController, Point? where = null)
        {
            if (where != null)
            {
                var pos = (Point)where;
                docController.GetPositionField().Data = pos;

                // TODO this is arbitrary should not be getting set here
                //var h = docController.GetHeightField().Data;
                //var w = docController.GetWidthField().Data;
                //docController.GetPositionField().Data = double.IsNaN(h) || double.IsNaN(w) ? pos : new Point(pos.X - w / 2, pos.Y - h / 2);
            }
            collectionViewModel.AddDocument(docController, null);
            collectionViewModel.DocumentViewModels.Last().GroupOnCreate = true;
        }


        /// <summary>
        /// Given a function that produces a document controller, visually displays the documents
        /// on the selected FreeFormView, defaulting to the main canvas.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="opController"></param>
        public static void AddDocFromFunction(UserControl sender, DocumentController opController)
        {
            // default to MainPage collection view
            CollectionFreeformView freeForm = MainPage.Instance.GetMainCollectionView().CurrentView as CollectionFreeformView;

            // fetch the coordinates of the caller on canvas
            var searchView = sender;
            var transform = searchView.TransformToVisual(freeForm.xItemsControl.ItemsPanelRoot);
            Debug.Assert(transform != null);
            var translate = transform.TransformPoint(new Point());
            translate = new Point(translate.X + 300, translate.Y + 100);

            // using this as a setter for the transform massive hack - LM
            var _ = new DocumentViewModel(opController)
            {
                Position = translate
            };

            if (opController != null)
            {
                freeForm.ViewModel.AddDocument(opController, null);
            }

        }


        public static void AddNote(ICollectionView collectionView, DragEventArgs e)
        {
            var where = Util.GetCollectionFreeFormPoint(collectionView as CollectionFreeformView, e.GetPosition(MainPage.Instance));
            AddNote(collectionView, where);
        }

        public static void AddNote(ICollectionView collectionView, TappedRoutedEventArgs e)
        {
            var where = Util.GetCollectionFreeFormPoint(collectionView as CollectionFreeformView, e.GetPosition(MainPage.Instance));
            AddNote(collectionView, where);
        }

        public static void AddNote(ICollectionView collectionView, Point mainPageCoord)
        {
            DocumentController postitNote = new RichTextNote(PostitNote.DocumentType).Document;
            DisplayDocument(collectionView.ViewModel, postitNote, mainPageCoord);
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
            // TODO someone finish this method, sorry I had to remove deprecated code that added this to collections - LSM
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

        public static void ChangeTheme(ICollectionView collectionView, DragEventArgs e)
        {
            MainPage.Instance.ThemeChange();
        }
    }
}
