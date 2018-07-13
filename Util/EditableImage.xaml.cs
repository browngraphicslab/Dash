using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Dash.Annotations;
using Dash.Views;
using DashShared;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using TextBlock = Windows.UI.Xaml.Controls.TextBlock;
using Visibility = Windows.UI.Xaml.Visibility;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Dash
{

	public partial class EditableImage : INotifyPropertyChanged, IVisualAnnotatable
	{
		private readonly Context _context;
		private readonly DocumentController _docCtrl;
		private StateCropControl _cropControl;
		private ImageController _imgctrl;
		public bool IsCropping;
		private DocumentView _docview;
	    public VisualAnnotationManager AnnotationManager;
        
        // interface-required events to communicate with the AnnotationManager
        public event PointerEventHandler NewRegionStarted;
	    public event PointerEventHandler NewRegionMoved;
	    public event PointerEventHandler NewRegionEnded;

        public Image Image => xImage;

		public event PropertyChangedEventHandler PropertyChanged;

		public EditableImage(DocumentController docCtrl, Context context)
		{
			InitializeComponent();
			_docCtrl = docCtrl;
			_context = context;
			Image.Loaded += Image_Loaded;
			// gets datakey value (which holds an imagecontroller) and cast it as imagecontroller
			_imgctrl = docCtrl.GetDereferencedField(KeyStore.DataKey, context) as ImageController;

			// existing annotated regions are loaded with the VisualAnnotationManager
		}

	    public void RegionSelected(object region, Point pt, DocumentController chosenDoc = null)
	    {
	        AnnotationManager.RegionSelected(region, pt, chosenDoc);
	    }

	    public async Task ReplaceImage()
		{
			_imgctrl = _docCtrl.GetDereferencedField<ImageController>(KeyStore.DataKey, new Context());

            // get the file from the current image controller
            var file = await GetImageFile();
            var fileProperties = await file.Properties.GetImagePropertiesAsync();

            // set image source to the new file path and fix the width
            Image.Source = new BitmapImage(new Uri(file.Path));
            Image.Width = fileProperties.Width;

            // on replace image, change the original image value for revert
            var origImgCtrl = _docCtrl.GetDereferencedField<ImageController>(KeyStore.DataKey, new Context());
            _docCtrl.SetField(KeyStore.OriginalImageKey, origImgCtrl, true);
        }

		private async Task<StorageFile> GetImageFile(bool originalImage = false)
		{
			// finds local uri path of image controller's image source
			StorageFile file;
			Uri src;
			if (originalImage)
			{
				src = _docCtrl.GetField<ImageController>(KeyStore.OriginalImageKey).ImageSource;
			}
			else
			{
				src = _imgctrl.ImageSource;
			}

			/*
			 * TODO There has to be a better way to do this. Maybe ask Bob and see if he has any ideas?
			 * try catch is literally the only way we can deal with regular
			 * local uris, absolute uris, and website uris as the same time
			 */
			try
			{
				// method of getting file from local uri
				file = await StorageFile.GetFileFromPathAsync(src.LocalPath);
			}
			catch (Exception)
			{
				// method of getting file from absolute uri
				file = await StorageFile.GetFileFromApplicationUriAsync(src);
			}

			return file;
		}

        public async void Revert()
        {
            using (UndoManager.GetBatchHandle())
            {
                // make sure if we have an original image stored (which we always should)
                if (_docCtrl.GetField<ImageController>(KeyStore.OriginalImageKey) != null)
                {
                    // get the storagefile of the original image so we can revert
                    var file = await GetImageFile(true);
                    var fileProperties = await file.Properties.GetImagePropertiesAsync();
                    Image.Width = fileProperties.Width;

                    _docCtrl.SetField<ImageController>(KeyStore.DataKey,
                        _docCtrl.GetField<ImageController>(KeyStore.OriginalImageKey).ImageSource, true);
                    _imgctrl = _docCtrl.GetDereferencedField<ImageController>(KeyStore.DataKey, new Context());
                }
            }
        }

		private void Image_Loaded(object sender, RoutedEventArgs e)
		{
			// initialize values that rely on the image
			_docview = this.GetFirstAncestorOfType<DocumentView>();
			Focus(FocusState.Keyboard);
			_cropControl = new StateCropControl(_docCtrl, this);
			AnnotationManager = new VisualAnnotationManager(this, _docCtrl, xAnnotations);
		}

		public async Task Rotate()
		{
			Rect rect = new Rect
			{
				X = 0,
				Y = 0,
				Width = Math.Floor(Image.ActualHeight),
				Height = Math.Floor(Image.ActualWidth)
			};

			await Crop(rect, BitmapRotation.Clockwise90Degrees);
		}

		public async Task MirrorHorizontal()
		{
			await MirrorImage(BitmapFlip.Horizontal);
		}

		public async Task MirrorVertical()
		{
			await MirrorImage(BitmapFlip.Vertical);
		}

		private async Task MirrorImage(BitmapFlip flip)
		{
			Rect rect = new Rect
			{
				X = 0,
				Y = 0,
				Height = Math.Floor(Image.ActualHeight),
				Width = Math.Floor(Image.ActualWidth)
			};
			await Crop(rect, BitmapRotation.None, flip);
		}

        // called when the cropclick action is invoked in the image subtoolbar
        public void StartCrop()
        {
            // make sure that we aren't already cropping
            if (xGrid.Children.Contains(_cropControl)) return;
            Focus(FocusState.Programmatic);
            xGrid.Children.Add(_cropControl);
            _docview.ViewModel.DisableDecorations = true;
            _docview.hideControls();
            IsCropping = true;
        }

        private void StopImageFromMoving(object sender, PointerRoutedEventArgs e)
        {
            // prevent the image from being moved while being cropped
            if (IsCropping) e.Handled = true;
        }

        /// <summary>
        ///     crops the image with respect to the values of the rectangle passed in
        /// </summary>
        /// <param name="rectangleGeometry">
        ///     rectangle geometry that determines the size and starting point of the crop
        /// </param>
        public async Task Crop(Rect rectangleGeometry, BitmapRotation rot = BitmapRotation.None,
            BitmapFlip flip = BitmapFlip.None)
        {
            var file = await GetImageFile();

			var fileProperties = await file.Properties.GetImagePropertiesAsync();

			if (_docCtrl.GetField<ImageController>(KeyStore.OriginalImageKey) == null)
			{
				var origImgCtrl = _docCtrl.GetDereferencedField<ImageController>(KeyStore.DataKey, new Context());
				_docCtrl.SetField(KeyStore.OriginalImageKey, origImgCtrl, true);
			}

			//_originalWidth is original width of owl, not replaced image
			var scale = fileProperties.Width / Image.ActualWidth;

			// retrieves data from rectangle
			var startPointX = (uint) rectangleGeometry.X;
			var startPointY = (uint) rectangleGeometry.Y;
			var height = (uint) rectangleGeometry.Height;
			var width = (uint) rectangleGeometry.Width;

			Debug.Assert(file != null); // if neither works, something's hecked up
			WriteableBitmap cropBmp;

			// opens the uri path and reads it
			using (IRandomAccessStream stream = await file.OpenReadAsync())
			{
				var decoder = await BitmapDecoder.CreateAsync(stream);

				// finds scaled size of the new bitmap image
				var scaledWidth = (uint) Math.Ceiling(decoder.PixelWidth / scale);
				var scaledHeight = (uint) Math.Ceiling(decoder.PixelHeight / scale);

				if (flip != BitmapFlip.None && (height != scaledHeight || width != scaledWidth))
				{
					height = scaledHeight;
					width = scaledWidth;
					rectangleGeometry.Height = scaledHeight;
					rectangleGeometry.Width = scaledWidth;
				}

				if (rot == BitmapRotation.Clockwise90Degrees && (height != scaledWidth || width != scaledHeight))
				{
					height = scaledWidth;
					width = scaledHeight;
					rectangleGeometry.Height = scaledWidth;
					rectangleGeometry.Width = scaledHeight;
				}

				// sets the boundaries for how we are cropping the bitmap image
				var bitmapTransform = new BitmapTransform();
				var bounds = new BitmapBounds
				{
					X = startPointX,
					Y = startPointY,
					Width = width,
					Height = height
				};
				bitmapTransform.Rotation = rot;
				bitmapTransform.Flip = flip;
				bitmapTransform.Bounds = bounds;
				bitmapTransform.ScaledWidth = scaledWidth;
				bitmapTransform.ScaledHeight = scaledHeight;

				// creates a new bitmap image with those boundaries
				var pix = await decoder.GetPixelDataAsync(
					BitmapPixelFormat.Bgra8,
					BitmapAlphaMode.Straight,
					bitmapTransform,
					ExifOrientationMode.IgnoreExifOrientation,
					ColorManagementMode.ColorManageToSRgb
				);

				var pixels = pix.DetachPixelData();

				// dis is it, the new bitmap image
				cropBmp = new WriteableBitmap((int) width, (int) height);
				var pixStream = cropBmp.PixelBuffer.AsStream();
				pixStream.Write(pixels, 0, (int) (width * height * 4));

				SaveCroppedImageAsync(cropBmp, decoder, rectangleGeometry, pixels);
			}
		}

        private async void SaveCroppedImageAsync(WriteableBitmap cropBmp, BitmapDecoder decoder, Rect rectgeo,
            byte[] pixels)
        {
            using (UndoManager.GetBatchHandle())
            {

                var width = (uint) rectgeo.Width;
                var height = (uint) rectgeo.Height;

                // randomly generate a new guid for the filename
                var fileName = UtilShared.GenerateNewId() + ".jpg"; // .jpg works for all images
                var bitmapEncoderGuid = BitmapEncoder.JpegEncoderId;
                // create the file
                var newFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(fileName,
                    CreationCollisionOption.ReplaceExisting);

                // load the file with the iamge information
                using (var newStream = await newFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var encoder = await BitmapEncoder.CreateAsync(bitmapEncoderGuid, newStream);

                    encoder.SetPixelData(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Straight,
                        width,
                        height,
                        decoder.DpiX,
                        decoder.DpiY,
                        pixels);
                    await encoder.FlushAsync();
                }

                // retrieve the uri from the file to update the image controller
                var path = "ms-appdata:///local/" + newFile.Name;
                var uri = new Uri(path);
                _docCtrl.SetField<ImageController>(KeyStore.DataKey, uri, true);

                // update the image source, width, and positions
                Image.Source = cropBmp;
                Image.Width = width;

                // store new image information so that multiple crops can be made
                _imgctrl = _docCtrl.GetDereferencedField<ImageController>(KeyStore.DataKey, _context);

                var oldpoint = _docCtrl.GetPosition() ?? new Point();
                var scale = _docCtrl.GetField<PointController>(KeyStore.ScaleAmountFieldKey).Data;
                Point point = new Point(oldpoint.X + _cropControl.GetBounds().X * scale.X,
                    oldpoint.Y + _cropControl.GetBounds().Y * scale.Y);

                _docCtrl.SetPosition(point);
                _cropControl = new StateCropControl(_docCtrl, this);

                // TODO: Test that replace button works with cropping when merged with master
            }
        }

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

        // functionality for saving a crop and for moving the cropping boxes with directional keys
        private async void XGrid_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (IsCropping)
                switch (e.Key)
                {
                    case VirtualKey.Enter:
                        // crop the image!
                        IsCropping = false;
                        xGrid.Children.Remove(_cropControl);
                        await Crop(_cropControl.GetBounds());
                        _docview.ViewModel.DisableDecorations = false;
                        _docview.hideControls();

                        break;
                    case VirtualKey.Left:
                    case VirtualKey.Right:
                    case VirtualKey.Up:
                    case VirtualKey.Down:
                        // moves the bounding box in the key's direction
                        _cropControl.OnKeyDown(e);
                        break;
                }
            e.Handled = true;
        }

		// removes the cropping controls and allows image to be moved and used when focus is lost
		private void EditableImage_OnLostFocus(object sender, RoutedEventArgs e)
		{
			if (!IsCropping) return;
			IsCropping = false;
			_docview.showControls();
			xGrid.Children.Remove(_cropControl);
		    AnnotationManager.ToggleRegionPreviewVisibility(Visibility.Collapsed);
		}

		private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
		{
			if (IsCropping) e.Handled = true;
            NewRegionMoved?.Invoke(this, e);
		}

		private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
		{
			if (IsCropping) e.Handled = true;
			NewRegionEnded?.Invoke(this, e);
		}

		private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (IsCropping) e.Handled = true;
			var properties = e.GetCurrentPoint(this).Properties;

			if (!IsCropping && properties.IsRightButtonPressed == false)
			{
			    NewRegionStarted?.Invoke(this, e);
            }
		}

		public DocumentController GetRegionDocument()
		{
		    return AnnotationManager.GetRegionDocument();
		}

	    public DocumentController GetDocControllerFromSelectedRegion()
	    {
            // the bitmap streaming to crop doesn't work yet
	        var imNote = new ImageNote(_imgctrl.ImageSource, xAnnotations.GetTopLeftPoint(),
	                xAnnotations.GetDuringPreviewActualSize()).Document;
            imNote.SetRegionDefinition(_docCtrl, Dash.AnnotationManager.AnnotationType.RegionBox);

	        return imNote;
	    }

	    public FrameworkElement Self()
	    {
	        return this;
	    }

	    public Size GetTotalDocumentSize()
	    {
	        return new Size(xImage.ActualWidth, xImage.ActualHeight);
	    }

	    public FrameworkElement GetPositionReference()
	    {
	        return xImage;
	    }
	}
}