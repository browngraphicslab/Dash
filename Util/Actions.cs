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

            var searchView = new DocumentView
            {
                Width = 200,
                Height = 200
            };
            var where = Util.GetCollectionDropPoint(
                MainPage.Instance.MainDocView.GetFirstDescendantOfType<CollectionView>(),
                e.GetPosition(MainPage.Instance));
            var pos = new Point(where.X - 30, where.Y -30);
            MainPage.Instance.DisplayDocument(opModel, where);
            MainPage.Instance.AddGenericFilter(o, e);
        }



        public static void ChangeInkColor(Color color, RadialMenu menu=null)
        {
            InkSource.Color = color;
            InkSource.SetAttributes();
            if (menu != null) menu.CenterButtonBackgroundFill = new SolidColorBrush(InkSource.Attributes.Color);
        }

        public static void ChoosePen(object o)
        {
            InkSource.StrokeType = InkSource.StrokeTypes.Pen;
            InkSource.SetAttributes();
        }

        public static void ChoosePencil(object o)
        {
            InkSource.StrokeType = InkSource.StrokeTypes.Pencil;
            InkSource.SetAttributes();
        }

        public static void SetOpacity(double opacity)
        {
            InkSource.Opacity = opacity;
            InkSource.SetAttributes();
        }

        public static void SetSize(double size)
        {
            InkSource.Size = size;
            InkSource.SetAttributes();
        }


        public static void DisplayBrightnessSlider(RadialMenuView obj)
        {
            Action<double, RadialMenu> setBrightness = SetBrightness;
            obj.OpenSlider("Brightness ", setBrightness);
        }

        public static void CloseSliderPanel(RadialMenuView obj)
        {
            obj.CloseSlider();
        }

        public static void SetBrightness(double brightness, RadialMenu menu)
        {
            InkSource.BrightnessFactor = brightness;
            InkSource.SetAttributes();
            if (menu != null) menu.CenterButtonBackgroundFill = new SolidColorBrush(InkSource.Attributes.Color);
        }


        public static void OnOperatorAdd(object o, DragEventArgs e)
        {
            //MainPage.Instance.AddOperator();
            MainPage.Instance.AddOperatorsFilter(o, e);
        }

        public static void AddOperator(object obj)
        {
            var freeForm = OperatorSearchView.AddsToThisCollection.CurrentView as CollectionFreeformView;

            if (freeForm == null)
            {
                return;
            }
            
            var searchView = OperatorSearchView.Instance.SearchView;
            var transform = searchView.TransformToVisual(freeForm.xItemsControl.ItemsPanelRoot);
            Debug.Assert(transform != null);
            var translate = transform.TransformPoint(new Point(searchView.ActualWidth, 0));

            var opCreator = obj as KeyValuePair<string, object>? ?? new KeyValuePair<string, object>();
            var opController = (opCreator.Value as Func<DocumentController>)?.Invoke();

            // using this as a setter for the transform massive hack - LM
            var opvm = new DocumentViewModel(opController)
            {
                GroupTransform = new TransformGroupData(translate, new Point(), new Point(1, 1))
            };

            if (opController != null)
            {
                OperatorSearchView.AddsToThisCollection.ViewModel.CollectionFieldModelController.AddDocument(opController);
            }
        }
        
        public static void AddCollection(CollectionView collection, DragEventArgs e)
        {
            //Get transformed position of drop event
            var where = Util.GetCollectionDropPoint(collection, e.GetPosition(MainPage.Instance));

            //Make first collection
            var numbers = new Numbers().Document;
            var fields = new Dictionary<Key, FieldModelController>
            {
                {
                    DocumentCollectionFieldModelController.CollectionKey,
                    new DocumentCollectionFieldModelController(new[] {numbers})
                }
            };
            var col = new DocumentController(fields, new DocumentType("collection", "collection"));
            var layoutDoc =
                new CollectionBox(new ReferenceFieldModelController(col.GetId(),
                    DocumentCollectionFieldModelController.CollectionKey)).Document;
            var layoutController = new DocumentFieldModelController(layoutDoc);
            col.SetField(DashConstants.KeyStore.ActiveLayoutKey, layoutController, true);
            col.SetField(DashConstants.KeyStore.LayoutListKey,
                new DocumentCollectionFieldModelController(new List<DocumentController> {layoutDoc}), true);

            //Make second collection
            var numbers2 = new Numbers().Document;
            var twoImages2 = new TwoImages(false).Document;
            var fields2 = new Dictionary<Key, FieldModelController>
            {
                [DocumentCollectionFieldModelController.CollectionKey] =
                new DocumentCollectionFieldModelController(new[]
                    {numbers2, twoImages2})
            };
            var col2 = new DocumentController(fields2, new DocumentType("collection", "collection"));
            var layoutDoc2 =
                new CollectionBox(new ReferenceFieldModelController(col2.GetId(),
                    DocumentCollectionFieldModelController.CollectionKey)).Document;
            var layoutController2 = new DocumentFieldModelController(layoutDoc2);
            col2.SetField(DashConstants.KeyStore.ActiveLayoutKey, layoutController2, true);
            col2.SetField(DashConstants.KeyStore.LayoutListKey,
                new DocumentCollectionFieldModelController(new List<DocumentController> {layoutDoc2}), true);

            //Display collections
            DisplayDocument(collection, col2, where);
            DisplayDocument(collection, col, where);
        }

        /// <summary>
        ///     Adds new documents to the MainView document. New documents are added as children of the Main document.
        /// </summary>
        /// <param name="docModel"></param>
        /// <param name="where"></param>
        /// <param name="collection"></param>
        public static void DisplayDocument(CollectionView collection, DocumentController docModel, Point? where = null)
        {
            if (where != null)
            {
                docModel.GetPositionField().Data = (Point)where;
            }
            var children = collection.ViewModel.CollectionFieldModelController;
            children?.AddDocument(docModel);
        }

        public static void AddApiCreator(CollectionView collection, DragEventArgs e)
        {
            var where = Util.GetCollectionDropPoint(collection, e.GetPosition(MainPage.Instance));
            var a = new ApiDocumentModel().Document;
            DisplayDocument(collection, a, where);
        }

        public static void AddDocuments(CollectionView col, DragEventArgs e)
        {
            var where = Util.GetCollectionDropPoint(col, e.GetPosition(MainPage.Instance));
            foreach (var d in new DBTest().Documents)
                DisplayDocument(col, d, where);
        }

        public static void AddNotes(CollectionView col, DragEventArgs e)
        {
            var where = Util.GetCollectionDropPoint(col, e.GetPosition(MainPage.Instance));
            DocumentController postitNote = new NoteDocuments.PostitNote(NoteDocuments.PostitNote.DocumentType).Document;
            DisplayDocument(col, postitNote, where);
        }

        public static void SetTouchInput(object obj)
        {
            InkSource.InkInputType = CoreInputDeviceTypes.Touch;
        }

        public static void SetPenInput(object obj)
        {
            InkSource.InkInputType = CoreInputDeviceTypes.Pen;
        }

        public static void SetMouseInput(object obj)
        {
            InkSource.InkInputType = CoreInputDeviceTypes.Mouse;
        }

        public static void SetNoInput(object obj)
        {
            InkSource.InkInputType = CoreInputDeviceTypes.None;
        }
    }
}
