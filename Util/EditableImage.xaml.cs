﻿
using Windows.UI.Xaml.Controls;
using Windows.UI.Input;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using Windows.Foundation;
using System.Diagnostics;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.Storage;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml.Data;
// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Dash
{

    public partial class EditableImage
    {

        public Image Image => xImage;
       
        private PointerPoint p1;
        private PointerPoint p2;
        private bool isLeft;
        private bool hasDragged;
        public RectangleGeometry rectgeo;
        private ImageController _imgctrl;
        private DocumentController _docCtrl;
        private Context _context;
      
       

        public EditableImage(DocumentController docCtrl, Context context)
        {
            InitializeComponent();
            rectgeo = new RectangleGeometry();
            _docCtrl = docCtrl;
            _context = context;
            _imgctrl = docCtrl.GetDereferencedField(KeyStore.DataKey, context) as ImageController;

        }



        private void Grid_PointerPressed(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {

            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                p1 = e.GetCurrentPoint(xImage);
                isLeft = true;
                transform.X = p1.Position.X;
                transform.Y = p1.Position.Y;
            }

        }

        private void Grid_PointerMoved(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                p2 = e.GetCurrentPoint(xImage);
                hasDragged = true;
                xRect.Visibility = Windows.UI.Xaml.Visibility.Visible;
               

                xRect.Width = (int)Math.Abs(p2.Position.X - p1.Position.X);
                xRect.Height = (int)Math.Abs(p2.Position.Y - p1.Position.Y);

            }

        }

        

        private async void Grid_PointerReleased(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {

            if (isLeft && hasDragged && !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                
                p2 = e.GetCurrentPoint(xImage);

                xRect.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            
                await Task.Delay(100);
                
                rectgeo.Rect = new Rect(p1.Position.X, p1.Position.Y, xRect.Width, xRect.Height);


                //xImage.Clip = rectgeo;

                //var docView = this.GetFirstAncestorOfType<DocumentView>();

                //Point point = new Point(rectgeo.Rect.X, rectgeo.Rect.Y);


                //docView.RenderTransform = transform;

                //docView.ViewModel.Width = xRect.Width;
                //docView.ViewModel.Height = xRect.Height;

                OnCrop(rectgeo.Rect);

                isLeft = false;
                hasDragged = false;

            }

        }

        private async void OnCrop(Rect rectgeo)
        {
            
            var startx = (uint)rectgeo.X;
            var starty = (uint)rectgeo.Y;

            var height = (uint)rectgeo.Height;
            var width = (uint)rectgeo.Width;

            StorageFile file = await StorageFile.GetFileFromPathAsync(_imgctrl.ImageSource.LocalPath);

            WriteableBitmap cropBmp;
            using (IRandomAccessStream stream = await file.OpenReadAsync())
            {
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                BitmapTransform transform = new BitmapTransform();
                BitmapBounds bounds = new BitmapBounds()
                {
                    X = startx,
                    Y = starty,
                    Width = width,
                    Height = height

                };
                transform.Bounds = bounds;

                PixelDataProvider pix = await decoder.GetPixelDataAsync(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Straight,
                    transform,
                    ExifOrientationMode.IgnoreExifOrientation,
                    ColorManagementMode.ColorManageToSRgb
                );

                byte[] pixels = pix.DetachPixelData();

                cropBmp = new WriteableBitmap((int)width, (int)height);
                Stream pixStream = cropBmp.PixelBuffer.AsStream();
                pixStream.Write(pixels, 0, (int)(width * height * 4));
                

            }

            if (cropBmp != null)
            {
                //TODO: setup image binding with new controller
                Image.Source = cropBmp;
                //var keyCtrl = KeyStore.DataKey;
                //var docType =
                //new DocumentController();
                //SetupImageBinding(Image, _docCtrl, _context);
            }
        }
        private static void SetupImageBinding(Image image, DocumentController controller,
           Context context)
        {
            var data = controller.GetField(KeyStore.DataKey);
            if (data is ReferenceController)
            {
                var reference = data as ReferenceController;
                var dataDoc = reference.GetDocumentController(context);
                dataDoc.AddFieldUpdatedListener(reference.FieldKey,
                    delegate (FieldControllerBase sender, FieldUpdatedEventArgs args, Context c)
                    {
                        var doc = (DocumentController)sender;
                        var dargs =
                            (DocumentController.DocumentFieldUpdatedEventArgs)args;
                        if (args.Action == DocumentController.FieldUpdatedAction.Update || dargs.FromDelegate)
                            return;
                        BindImageSource(image, doc, c, reference.FieldKey);
                    });
            }
            BindImageSource(image, controller, context, KeyStore.DataKey);
        }
        protected static void BindImageSource(Image image, DocumentController docController, Context context,
           KeyController key)
        {
            var data = docController.GetDereferencedField(key, context) as ImageController;
            if (data == null)
                return;
            var binding = new FieldBinding<ImageController>
            {
                Document = docController,
                Key = KeyStore.DataKey,
                Mode = BindingMode.OneWay,
                Context = context,
                Converter = UriToBitmapImageConverter.Instance
            };
            image.AddFieldBinding(Image.SourceProperty, binding);
        }
    }
}
