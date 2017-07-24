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
using Dash.Models;
using Dash.Views;
using DashShared;
using Microsoft.Extensions.DependencyInjection;
using RadialMenuControl.UserControl;

namespace Dash
{
    public static class Actions
    {
       
        public static void AddSearch(Canvas c, Point p)
        {
            MainPage.Instance.AddGenericFilter();
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


        public static void OnOperatorAdd(object obj)
        {
            //MainPage.Instance.AddOperator();
            MainPage.Instance.AddOperatorsFilter();
        }

        public static void AddOperator(object obj)
        {
            DocumentController opModel = null;
            var type = obj as string;
            if (type == null) return;
            if (type == "Divide")
            {
                opModel =
                OperatorDocumentModel.CreateOperatorDocumentModel(
                    new DivideOperatorFieldModelController(new OperatorFieldModel(type)));
                var view = new DocumentView
                {
                    Width = 200,
                    Height = 200
                };
                var opvm = new DocumentViewModel(opModel);
                //OperatorDocumentViewModel opvm = new OperatorDocumentViewModel(opModel);
                view.DataContext = opvm;
            }
            else if (type == "Union")
            {
                opModel =
                    OperatorDocumentModel.CreateOperatorDocumentModel(
                        new UnionOperatorFieldModelController(new OperatorFieldModel(type)));
                var unionView = new DocumentView
                {
                    Width = 200,
                    Height = 200
                };
                var unionOpvm = new DocumentViewModel(opModel);
                unionView.DataContext = unionOpvm;
            }
            else if (type == "Intersection")
            {
                // add union operator for testing 
                opModel =
                    OperatorDocumentModel.CreateOperatorDocumentModel(
                        new IntersectionOperatorModelController(new OperatorFieldModel(type)));
                var intersectView = new DocumentView
                {
                    Width = 200,
                    Height = 200
                };
                var intersectOpvm = new DocumentViewModel(opModel);
                intersectView.DataContext = intersectOpvm;
            }
            else if (type == "ImageToUri")
            {
                // add image url -> image operator for testing
                opModel =
                    OperatorDocumentModel.CreateOperatorDocumentModel(
                        new ImageOperatorFieldModelController(new OperatorFieldModel(type)));
                var imgOpView = new DocumentView
                {
                    Width = 200,
                    Height = 200
                };
                var imgOpvm = new DocumentViewModel(opModel);
                imgOpView.DataContext = imgOpvm;
            }
            if (opModel != null)
                MainPage.Instance.DisplayDocument(opModel);
        }
        
        public static void AddCollection(object obj)
        {
            MainPage.Instance.AddCollection(null, null);
        }

        public static void AddApiCreator(object obj)
        {
            MainPage.Instance.AddApiCreator(obj, new TappedRoutedEventArgs());
        }

        public static void AddImage(object obj)
        {
            // xFreeformView.Canvas.Children.Add(new Sources.FilePicker.FilePickerDisplay());
            // xFreeformView.Canvas.Children.Add(new Sources.FilePicker.PDFFilePicker());
        }

        public static void AddDocuments(object obj)
        {
            MainPage.Instance.AddDocuments(null, null);
        }

        public static void AddNotes(object obj)
        {
            MainPage.Instance.AddNotes(); 
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
