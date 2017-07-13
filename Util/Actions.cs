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
       
        private static SearchView _searchView = new SearchView();

        public static void AddSearch(Canvas c, Point p)
        {
            if (!c.Children.Contains(_searchView))
            {
                c.Children.Add(_searchView);
                _searchView.SetPosition(p);
                _searchView.IsDraggable = true;
            }
            else
            {
                c.Children.Remove(_searchView);
            }
        }



        public static void ChangeInkColor(Color color, RadialMenu menu=null)
        {
            InkSettings.Color = color;
            InkSettings.SetAttributes();
            if (menu != null) menu.CenterButtonBackgroundFill = new SolidColorBrush(InkSettings.Attributes.Color);
        }

        public static void ChoosePen(object o)
        {
            InkSettings.StrokeType = InkSettings.StrokeTypes.Pen;
            InkSettings.SetAttributes();
        }

        public static void ChoosePencil(object o)
        {
            InkSettings.StrokeType = InkSettings.StrokeTypes.Pencil;
            InkSettings.SetAttributes();
        }

        public static void ChooseEraser(object o)
        {
            InkSettings.StrokeType = InkSettings.StrokeTypes.Eraser;
            InkSettings.SetAttributes();
        }

        public static void SetOpacity(double opacity)
        {
            InkSettings.Opacity = opacity;
            InkSettings.SetAttributes();
        }

        public static void SetSize(double size)
        {
            InkSettings.Size = size;
            InkSettings.SetAttributes();
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
            InkSettings.BrightnessFactor = brightness;
            InkSettings.SetAttributes();
            if (menu != null) menu.CenterButtonBackgroundFill = new SolidColorBrush(InkSettings.Attributes.Color);
        }


        public static void OnOperatorAdd(object obj)
        {
            MainPage.Instance.AddOperator();
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

        public static void SetTouchInput(object obj)
        {
            InkSettings.InkInputType = CoreInputDeviceTypes.Touch;
        }

        public static void SetPenInput(object obj)
        {
            InkSettings.InkInputType = CoreInputDeviceTypes.Pen;
        }

        public static void SetMouseInput(object obj)
        {
            InkSettings.InkInputType = CoreInputDeviceTypes.Mouse;
        }

        public static void SetNoInput(object obj)
        {
            InkSettings.InkInputType = CoreInputDeviceTypes.None;
        }
    }
}
