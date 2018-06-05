﻿
using Windows.UI.Xaml.Controls;
using Windows.UI.Input;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using Windows.Foundation;
using System.Diagnostics;
// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Dash
{

    public partial class EditableImage
    {

        //public Image Image => xImage;
        private PointerPoint p1;
        private PointerPoint p2;
        private bool isLeft;
        private bool hasDragged;

        public EditableImage()
        {
            InitializeComponent();

        }

        private void Grid_PointerPressed(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {

            if (e.GetCurrentPoint(xImage).Properties.IsLeftButtonPressed)
            {
                p1 = e.GetCurrentPoint(xImage);
                isLeft = true;
            }

        }

        private void Grid_PointerMoved(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(xImage).Properties.IsLeftButtonPressed)
            {
                p2 = e.GetCurrentPoint(xImage);
                hasDragged = true;

            }

        }

        private async void Grid_PointerReleased(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {

            if (isLeft && hasDragged && !e.GetCurrentPoint(xImage).Properties.IsLeftButtonPressed)
            {
                p2 = e.GetCurrentPoint(xImage);


                xRect.Visibility = Windows.UI.Xaml.Visibility.Visible;
                xRect.Width = (int)Math.Abs(p2.Position.X - p1.Position.X);
                xRect.Height = (int)Math.Abs(p2.Position.Y - p1.Position.Y);
                xRect.SetValue(Canvas.LeftProperty, (p1.Position.X < p2.Position.X) ? p1.Position.X : p2.Position.X);
                xRect.SetValue(Canvas.TopProperty, (p1.Position.Y < p2.Position.Y) ? p1.Position.Y : p2.Position.Y);
                await Task.Delay(100);
                RectangleGeometry geometry = new RectangleGeometry();
                geometry.Rect = new Rect(p1.Position, p2.Position);
                xImage.Clip = geometry;
                var docView = this.GetFirstAncestorOfType<DocumentView>();
                Point point = new Point(geometry.Rect.X, geometry.Rect.Y);
                docView.ViewModel.Position = point;
                docView.ViewModel.Width = geometry.Rect.Width;
                docView.ViewModel.Height = geometry.Rect.Height;
                xRect.Visibility = Windows.UI.Xaml.Visibility.Visible;
                isLeft = false;
                hasDragged = false;

            }

        }


    }
}
