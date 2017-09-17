using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Dash.Models;
using DashShared;
using Microsoft.Extensions.DependencyInjection;
using RadialMenuControl.UserControl;
using Dash.Controllers.Operators;
using static Dash.Controllers.Operators.DBSearchOperatorFieldModelController;

namespace Dash
{
    public static class Actions
    {
        
       
        public static void AddSearch(object o, DragEventArgs e)
        {
            //if (!c.Children.Contains(_searchView))
            //{
            //    c.Children.Add(_searchView);
            //    _searchView.SetPosition(p);
            //    _searchView.IsDraggable = true;
            //}
            //else
            //{
            //    c.Children.Remove(_searchView);
            //}
            var opModel = DBSearchOperatorFieldModelController.CreateSearch(DBTest.DBNull, DBTest.DBDoc, "", "");

            var where = Util.GetCollectionFreeFormPoint(
                MainPage.Instance.MainDocView.GetFirstDescendantOfType<CollectionFreeformView>(),
                e.GetPosition(MainPage.Instance));
            var pos = new Point(where.X - 30, where.Y -30);
            MainPage.Instance.DisplayDocument(opModel, where);
            MainPage.Instance.AddGenericFilter(o, e);
        }

        public static void ChangeInkColor(Color color, RadialMenu menu = null)
        {
            GlobalInkSettings.Color = color;
            GlobalInkSettings.UpdateInkPresenters();
            if (menu != null) menu.CenterButtonBackgroundFill = new SolidColorBrush(GlobalInkSettings.Attributes.Color);
        }

        public static void ChoosePen(object o)
        {
            GlobalInkSettings.StrokeType = GlobalInkSettings.StrokeTypes.Pen;
            GlobalInkSettings.UpdateInkPresenters(false);
        }

        public static void ChoosePencil(object o)
        {
            GlobalInkSettings.StrokeType = GlobalInkSettings.StrokeTypes.Pencil;
            GlobalInkSettings.UpdateInkPresenters(false);
        }

        public static void ChooseEraser(object o)
        {
            GlobalInkSettings.StrokeType = GlobalInkSettings.StrokeTypes.Eraser;
            GlobalInkSettings.UpdateInkPresenters(false);
        }

        public static void SetOpacity(double opacity)
        {
            GlobalInkSettings.Opacity = opacity;
            GlobalInkSettings.UpdateInkPresenters();
        }

        public static void SetSize(double size)
        {
            GlobalInkSettings.Size = size;
            GlobalInkSettings.UpdateInkPresenters();
        }


        public static void DisplayBrightnessSlider(RadialMenuView obj)
        {
            obj.OpenSlider();
        }

        public static void CloseSliderPanel(RadialMenuView obj)
        {
            obj.CloseSlider();
        }

        public static void SetBrightness(double brightness, RadialMenu menu)
        {
            GlobalInkSettings.BrightnessFactor = brightness;
            GlobalInkSettings.UpdateInkPresenters();
            if (menu != null) menu.CenterButtonBackgroundFill = new SolidColorBrush(GlobalInkSettings.Attributes.Color);
        }


        public static void OnOperatorAdd(ICollectionView collection, DragEventArgs e)
        {
            MainPage.Instance.AddOperatorsFilter(collection, e);
        }

        public static void AddOperator(Func<DocumentController> documentCreationFunc)
        {
            var freeForm = OperatorSearchView.AddsToThisCollection;

            if (freeForm == null)
            {
                return;
            }
            
            var searchView = OperatorSearchView.Instance.SearchView;
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
                OperatorSearchView.AddsToThisCollection.ViewModel.AddDocument(opController, null);
            }
        }

        public static void AddDocument(ICollectionView collection, DragEventArgs e)
        {
            var where = Util.GetCollectionFreeFormPoint(collection as CollectionFreeformView,
                e.GetPosition(MainPage.Instance));

            var fields = new Dictionary<KeyController, FieldModelController>()
            {
                [KeyStore.ActiveLayoutKey] = new DocumentFieldModelController(new FreeFormDocument(new List<DocumentController>(), where, new Size(400, 400)).Document)
            };

            collection.ViewModel.AddDocument(new DocumentController(fields, DocumentType.DefaultType), null);
        }

        public static void AddCollection(ICollectionView collection, DragEventArgs e)
        {
            var where = Util.GetCollectionFreeFormPoint(collection as CollectionFreeformView,
                e.GetPosition(MainPage.Instance));

            var fields = new Dictionary<KeyController, FieldModelController>()
            {
                [DocumentCollectionFieldModelController.CollectionKey] = new DocumentCollectionFieldModelController(),
            };

            var documentController = new DocumentController(fields, DocumentType.DefaultType);
            documentController.SetActiveLayout(
                new CollectionBox(
                        new ReferenceFieldModelController(documentController.GetId(),
                            DocumentCollectionFieldModelController.CollectionKey), where.X, where.Y, 400, 400)
                    .Document, true, true);

            collection.ViewModel.AddDocument(documentController, null);
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
            var fields = new Dictionary<KeyController, FieldModelController>
            {
                [DocumentCollectionFieldModelController.CollectionKey] = new DocumentCollectionFieldModelController(numbers)
            };
            var collectionDocument = new DocumentController(fields, DashConstants.DocumentTypeStore.CollectionDocument);
            var layoutDocument = new CollectionBox(new ReferenceFieldModelController(collectionDocument.GetId(),  
                DocumentCollectionFieldModelController.CollectionKey), 0, 0, 400, 400).Document;
            collectionDocument.SetField(KeyStore.ActiveLayoutKey, new DocumentFieldModelController(layoutDocument), true);
            collectionDocument.SetField(KeyStore.LayoutListKey,
                new DocumentCollectionFieldModelController(new List<DocumentController> { layoutDocument }), true);

            // Make second collection
            var numbers2 = new Numbers().Document;
            var twoImages2 = new TwoImages(false).Document;
            var fields2 = new Dictionary<KeyController, FieldModelController>
            {
                [DocumentCollectionFieldModelController.CollectionKey] =
                new DocumentCollectionFieldModelController(new[] {numbers2, twoImages2})
            };
            var col2 = new DocumentController(fields2, DashConstants.DocumentTypeStore.CollectionDocument);
            var layoutDoc2 =
                new CollectionBox(new ReferenceFieldModelController(col2.GetId(),
                    DocumentCollectionFieldModelController.CollectionKey), 0, 0, 400, 400).Document;
            var layoutController2 = new DocumentFieldModelController(layoutDoc2);
            col2.SetField(KeyStore.ActiveLayoutKey, layoutController2, true);
            col2.SetField(KeyStore.LayoutListKey,
                new DocumentCollectionFieldModelController(new List<DocumentController> { layoutDoc2 }), true);

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
            var fields2 = new Dictionary<KeyController, FieldModelController>
            {
                [DocumentCollectionFieldModelController.CollectionKey] =
                new DocumentCollectionFieldModelController(new[]
                    {numbers2})
            };
            var col2 = new DocumentController(fields2, DashConstants.DocumentTypeStore.CollectionDocument);
            var layoutDoc2 =
                new CollectionBox(new ReferenceFieldModelController(col2.GetId(),
                    DocumentCollectionFieldModelController.CollectionKey)).Document;
            var layoutController2 = new DocumentFieldModelController(layoutDoc2);
            col2.SetField(KeyStore.ActiveLayoutKey, layoutController2, true);
            col2.SetField(KeyStore.LayoutListKey,
                new DocumentCollectionFieldModelController(new List<DocumentController> { layoutDoc2 }), true);

            //Display collections
            DisplayDocument(collectionView, col2, where);

            DisplayDocument(collectionView, new InkDoc().Document, where);
            DisplayDocument(collectionView, new Numbers().Document, where);

            DisplayDocument(collectionView, new XampleText().Document, where);

            var ndb = new DBTest();
            for (int i = 0; i < ndb.Documents.Count; i++)
            {
                DisplayDocument(collectionView, ndb.Documents[i], where);
                MainPage.Instance.UpdateLayout();
            }
        }

        public static void AddNotes(ICollectionView collectionView, DragEventArgs e)
        {
            var where = Util.GetCollectionFreeFormPoint(collectionView as CollectionFreeformView, e.GetPosition(MainPage.Instance));
            DocumentController postitNote = new NoteDocuments.RichTextNote(NoteDocuments.PostitNote.DocumentType).Document;
            DisplayDocument(collectionView, postitNote, where);
        }

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
            GlobalInkSettings.IsSelectionEnabled = true;
        }

        public static void ToggleInkRecognition(object o)
        {
            GlobalInkSettings.IsRecognitionEnabled = !GlobalInkSettings.IsRecognitionEnabled;
        }

    }
}
