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
	public partial class EditableImage : INotifyPropertyChanged, IAnnotationEnabled
	{
		private readonly Context _context;
		private readonly DocumentController _docCtrl;
		private StateCropControl _cropControl;
		private ImageController _imgctrl;
		public bool IsCropping;
		private DocumentView _docview;
		private Point _anchorPoint;
		private bool _isDragging;
		private DocumentView _lastNearest;
		public ImageRegionBox _selectedRegion = null;
		private List<ImageRegionBox> _visualRegions;
		private ListController<DocumentController> _dataRegions;
		private RegionVisibilityState _regionState;
		public static KeyController RegionDefinitionKey = new KeyController("6EEDCB86-76F4-4937-AE0D-9C4BC6744310", "Region Definition");
		private bool _isLinkMenuOpen = false;
		private AnnotationManager _annotationManager;

		private bool _isPreviousRegionSelected = false;

		private Dictionary<TextBlock, DocumentController> _linkDict = null;
		//public static KeyController RegionDefinitionKey = new KeyController

		public Image Image => xImage;

		public event PropertyChangedEventHandler PropertyChanged;

		private enum RegionVisibilityState
		{
			Visible,
			Hidden
		}

		public EditableImage(DocumentController docCtrl, Context context)
		{
			InitializeComponent();
			_docCtrl = docCtrl;
			_context = context;
			Image.Loaded += Image_Loaded;
			// gets datakey value (which holds an imagecontroller) and cast it as imagecontroller
			_imgctrl = docCtrl.GetDereferencedField(KeyStore.DataKey, context) as ImageController;
			xRegionPostManipulationPreview._image = this;
			

			//load existing annotated regions
			_visualRegions = new List<ImageRegionBox>();
			_regionState = RegionVisibilityState.Hidden;
			
			_dataRegions = _docCtrl.GetDataDocument().GetField<ListController<DocumentController>>(KeyStore.RegionsKey);
			if (_dataRegions != null)
			{
				foreach (var region in _dataRegions.TypedData)
				{
					var pos = region.GetPositionField().Data;
					var width = region.GetWidthField().Data;
					var height = region.GetHeightField().Data;
					var imageSize = _docCtrl.GetField<PointController>(KeyStore.ActualSizeKey).Data;

					var newBox = new ImageRegionBox {LinkTo = region};

					if (pos.X + width <= imageSize.X && pos.Y + height <= imageSize.Y)
					{
						newBox.SetPosition(
							pos,
							new Size(width, height),
							new Size(imageSize.X, imageSize.Y));
						xRegionsGrid.Children.Add(newBox);
						newBox.PointerPressed += xRegion_OnPointerPressed;
						newBox.PointerEntered += Region_OnPointerEntered;
						newBox.PointerExited += Region_OnPointerExited;
						newBox._image = this;
						_visualRegions.Add(newBox);
						newBox.Hide();
					}
					
				}
			}

			//this.FormatLinkMenu();
			
		}

		public async Task ReplaceImage()
		{
			_imgctrl = _docCtrl.GetDereferencedField<ImageController>(KeyStore.DataKey, new Context());

			var file = await GetImageFile();

			var fileProperties = await file.Properties.GetImagePropertiesAsync();

			Image.Width = fileProperties.Width;
			Image.Source = new BitmapImage(new Uri(file.Path));

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
			if (_docCtrl.GetField<ImageController>(KeyStore.OriginalImageKey) != null)
			{
				var file = await GetImageFile(true);
				var fileProperties = await file.Properties.GetImagePropertiesAsync();
				Image.Width = fileProperties.Width;

				_docCtrl.SetField<ImageController>(KeyStore.DataKey,
					_docCtrl.GetField<ImageController>(KeyStore.OriginalImageKey).ImageSource, true);
				_imgctrl = _docCtrl.GetDereferencedField<ImageController>(KeyStore.DataKey, new Context());
			}
		}

		private void Image_Loaded(object sender, RoutedEventArgs e)
		{
			// initialize values that rely on the image
			_docview = this.GetFirstAncestorOfType<DocumentView>();
			Focus(FocusState.Keyboard);
			_cropControl = new StateCropControl(_docCtrl, this);
			_annotationManager = new AnnotationManager(this, _docview);
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
			_docview.hideControls();
			IsCropping = true;
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
			_docCtrl.SetField(KeyStore.DataKey, new ImageController(uri), true);

			// update the image source, width, and positions
			Image.Source = cropBmp;
			Image.Width = width;

			// store new image information so that multiple crops can be made
			_imgctrl = _docCtrl.GetDereferencedField(KeyStore.DataKey, _context) as ImageController;

			var oldpoint = _docCtrl.GetField<PointController>(KeyStore.PositionFieldKey).Data;
			var scale = _docCtrl.GetField<PointController>(KeyStore.ScaleAmountFieldKey).Data;
			Point point = new Point(oldpoint.X + _cropControl.GetBounds().X * scale.X,
				oldpoint.Y + _cropControl.GetBounds().Y * scale.Y);

			_docCtrl.SetField<PointController>(KeyStore.PositionFieldKey, point, true);
			_cropControl = new StateCropControl(_docCtrl, this);

			// TODO: Test that replace button works with cropping when merged with master
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
						_docview.showControls();
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
			xRegionDuringManipulationPreview.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
			xRegionPostManipulationPreview.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
		}


		private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
		{
			if (IsCropping) e.Handled = true;
			var properties = e.GetCurrentPoint(this).Properties;

			if (_isDragging && properties.IsRightButtonPressed == false)
			{
				//update size of preview region box according to mouse movement

				var pos = e.GetCurrentPoint(xImage).Position;

				var x = Math.Min(pos.X, _anchorPoint.X);
				var y = Math.Min(pos.Y, _anchorPoint.Y);
				xRegionDuringManipulationPreview.Margin = new Thickness(x, y, 0, 0);

				xRegionDuringManipulationPreview.Width = Math.Abs(pos.X - _anchorPoint.X);
				xRegionDuringManipulationPreview.Height = Math.Abs(pos.Y - _anchorPoint.Y);
			}
		}

		private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
		{
			if (IsCropping) e.Handled = true;
			xRegionDuringManipulationPreview.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

			if (MainPage.Instance.IsCtrlPressed())
			{
				return;
			}

			_isDragging = false;

			if (xRegionDuringManipulationPreview.Width < 50 && xRegionDuringManipulationPreview.Height < 50) return;

			// the box only sticks around if it's of a large enough size
			xRegionPostManipulationPreview.SetPosition(
				new Point(xRegionDuringManipulationPreview.Margin.Left, xRegionDuringManipulationPreview.Margin.Top),
				new Size(xRegionDuringManipulationPreview.ActualWidth, xRegionDuringManipulationPreview.ActualHeight),
				new Size(xImage.ActualWidth, xImage.ActualHeight)
			);
			xRegionPostManipulationPreview.Visibility = Windows.UI.Xaml.Visibility.Visible;
		}

		private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (IsCropping) e.Handled = true;
			var properties = e.GetCurrentPoint(this).Properties;

			if (!IsCropping && properties.IsRightButtonPressed == false)
			{

				var pos = e.GetCurrentPoint(xImage).Position;
				_anchorPoint = pos;
				_isDragging = true;

				//reset and get rid of the region preview
				xRegionDuringManipulationPreview.Width = 0;
				xRegionDuringManipulationPreview.Height = 0;
				xRegionDuringManipulationPreview.Visibility = Visibility.Collapsed;
				xRegionDuringManipulationPreview.Visibility = Windows.UI.Xaml.Visibility.Visible;

				
				//if not selecting an already selected region, collapse preview boxes
				if (!(xRegionPostManipulationPreview.Column1.ActualWidth < pos.X) ||
				    !(pos.X < xRegionPostManipulationPreview.Column1.ActualWidth +
				      xRegionPostManipulationPreview.Column2.ActualWidth) ||
				    !(xRegionPostManipulationPreview.Row1.ActualHeight < pos.Y) ||
				    !(pos.Y < xRegionPostManipulationPreview.Row1.ActualHeight +
				      xRegionPostManipulationPreview.Row2.ActualHeight))
				{
					this.DeselectRegions();
				}
				else
				{
					//delete if control is pressed
					if (MainPage.Instance.IsAltPressed())
					{
						this.DeleteRegion(_selectedRegion);
						_isPreviousRegionSelected = false;
						return;
					}  
					//select otherwise
					//if (xLinkStack.Visibility == Visibility.Collapsed)
					if (!_isLinkMenuOpen) this.RegionSelected(_selectedRegion, e.GetCurrentPoint(MainPage.Instance).Position);
				}
			}
		}


		public bool IsSomethingSelected()
		{
			return xRegionPostManipulationPreview.Visibility == Windows.UI.Xaml.Visibility.Visible;
		}


		public DocumentController GetRegionDocument()
		{
			if (!this.IsSomethingSelected()) return _docCtrl;

			DocumentController imNote = null;

			//if region is selected, access the selected region's doc controller
			if (_isPreviousRegionSelected && _selectedRegion != null)
			{
				//add this link to list of links
				imNote = _selectedRegion.LinkTo;
			}
			else
			{
				// the bitmap streaming to crop doesn't work yet
				imNote = new ImageNote(_imgctrl.ImageSource,
						new Point(xRegionDuringManipulationPreview.Margin.Left, xRegionDuringManipulationPreview.Margin.Top),
						new Size(xRegionDuringManipulationPreview.ActualWidth, xRegionDuringManipulationPreview.ActualHeight))
					.Document;
				imNote.SetField(KeyStore.RegionDefinitionKey, _docCtrl, true);

				//add to regions list
				var regions = _docCtrl.GetDataDocument()
					.GetDereferencedField<ListController<DocumentController>>(KeyStore.RegionsKey, null);
				if (regions == null)
				{
					var dregions = new List<DocumentController>();
					dregions.Add(imNote);
					_docCtrl.GetDataDocument()
						.SetField<ListController<DocumentController>>(KeyStore.RegionsKey, dregions, true);
				}
				else
				{
					regions.Add(imNote);
				}

				var newBox = new ImageRegionBox { LinkTo = imNote };

				// use During Preview here because it's the one with actual pixel measurements
				newBox.SetPosition(
					new Point(xRegionDuringManipulationPreview.Margin.Left, xRegionDuringManipulationPreview.Margin.Top),
					new Size(xRegionDuringManipulationPreview.ActualWidth, xRegionDuringManipulationPreview.ActualHeight),
					new Size(xImage.ActualWidth, xImage.ActualHeight));
				xRegionsGrid.Children.Add(newBox);
				newBox.PointerPressed += xRegion_OnPointerPressed;
				newBox._image = this;
				_visualRegions.Add(newBox);
				newBox.PointerEntered += Region_OnPointerEntered;
				newBox.PointerExited += Region_OnPointerExited;
				xRegionPostManipulationPreview.Visibility = Visibility.Collapsed;
			}
			return imNote;
		}

		
		private void xRegion_OnPointerPressed(object sender, PointerRoutedEventArgs e)
		{
			e.Handled = false;
			this.RegionSelected((ImageRegionBox) sender, e.GetCurrentPoint(MainPage.Instance).Position);
			e.Handled = true;
		}

		public void RegionSelected(object region, Point pos, DocumentController chosenDC = null)
		{
			if (region == null) return;

			
			DocumentController theDoc = null;

			if (region is ImageRegionBox)
			{
				//get the linked doc of the selected region
				var imageRegion = (ImageRegionBox) region;
				theDoc = imageRegion.LinkTo;
				if (theDoc == null) return;

				this.SelectRegion(imageRegion);
			}
			else
			{
				 theDoc = _docCtrl;
			}

			//delete if control is pressed
			if (MainPage.Instance.IsAltPressed() && region is ImageRegionBox)
			{
				this.DeleteRegion((ImageRegionBox) region);
				_isPreviousRegionSelected = false;
				return;
			}

			if (pos.X == 0 && pos.Y == 0) pos = _docCtrl.GetField<PointController>(KeyStore.PositionFieldKey).Data;

			_annotationManager.RegionPressed(theDoc, pos, chosenDC);

		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			//UI for preview boxes
			xRegionPostManipulationPreview.xRegionBox.Fill = new SolidColorBrush(Colors.AntiqueWhite);
			xRegionPostManipulationPreview.xRegionBox.Stroke = new SolidColorBrush(Colors.SaddleBrown);
			xRegionPostManipulationPreview.xRegionBox.StrokeDashArray = new DoubleCollection() { 4 };

			xRegionPostManipulationPreview.xRegionBox.StrokeThickness = 1.5;
			xRegionPostManipulationPreview.xRegionBox.Opacity = 0.5;
		}

		//delete passed-in region
		public void DeleteRegion(ImageRegionBox region)
		{
			//collapse any open selection box & links
			xRegionPostManipulationPreview.Visibility = Visibility.Collapsed;
			
			//remove actual region
			if (region != null)
			{
				xRegionsGrid.Children.Remove(region);
				_visualRegions?.Remove(region);
				_dataRegions?.Remove(region.LinkTo);
			}
			
			//if region is selected, unhighlight the linked doc
			if (region == _selectedRegion && _lastNearest?.ViewModel?.DocumentController != null)
			{
				MainPage.Instance.HighlightDoc(_lastNearest.ViewModel.DocumentController, false, 2);
				_lastNearest = null;
				//TODO: Remove annotaion from workspace?
			}

			this.DeselectRegions();
		}

		private void SelectRegion(ImageRegionBox region)
		{
			_selectedRegion = region;
			_isPreviousRegionSelected = true;
			//create a preview region to show that this region is selected
			xRegionDuringManipulationPreview.Width = 0;
			xRegionDuringManipulationPreview.Height = 0;
			xRegionPostManipulationPreview.Column1.Width = region.Column1.Width;
			xRegionPostManipulationPreview.Column2.Width = region.Column2.Width;
			xRegionPostManipulationPreview.Column3.Width = region.Column3.Width;
			xRegionPostManipulationPreview.Row1.Height = region.Row1.Height;
			xRegionPostManipulationPreview.Row2.Height = region.Row2.Height;
			xRegionPostManipulationPreview.Row3.Height = region.Row3.Height;
			xRegionPostManipulationPreview.Visibility = Windows.UI.Xaml.Visibility.Visible;
			//xRegionPostManipulationPreview.xCloseRegionButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
		}

		//deselects all regions on an image
		private void DeselectRegions()
		{
			_isPreviousRegionSelected = false;
			xRegionPostManipulationPreview.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
			//xRegionPostManipulationPreview.xCloseRegionButton.Visibility = Visibility.Collapsed;
			_linkDict?.Clear();

			//unhighlight last selected regions' link
			if (_lastNearest?.ViewModel?.DocumentController != null)
			{
				MainPage.Instance.HighlightDoc(_lastNearest.ViewModel.DocumentController, false, 2);
			}
		}

		//ensures all regions are visible
		public void ShowRegions()
		{
			_regionState = RegionVisibilityState.Visible;

			if (_visualRegions != null && _visualRegions.Any())
			{
				foreach (ImageRegionBox region in _visualRegions)
				{
					region.Show();
				}
			}
		}

		//hides all visible regions 
		public void HideRegions()
		{
			_regionState = RegionVisibilityState.Hidden;

			if (_visualRegions != null && _visualRegions.Any())
			{
				//first, deselect any selected regions
				this.DeselectRegions();

				foreach (ImageRegionBox region in _visualRegions)
				{
					//region.Visibility = Visibility.Collapsed;
					region.Hide();
				}
			}
		}

		//shows region when user hovers over it
		private void Region_OnPointerEntered(object sender, RoutedEventArgs e)
		{
			if (sender is ImageRegionBox)
			{
				ImageRegionBox region = (ImageRegionBox) sender;
				region.Show();
			}
		}

		//hides region when user's cursor leaves it if region visibiliyt mode is hidden
		private void Region_OnPointerExited(object sender, RoutedEventArgs e)
		{
			if (sender is ImageRegionBox)
			{
				ImageRegionBox region = (ImageRegionBox)sender;
				if (_regionState == RegionVisibilityState.Hidden) region.Hide(); 
			}
		}

		public void UpdateHighlight(DocumentView nearestOnCollection)
		{
			//unhighlight last doc
			if (_lastNearest?.ViewModel != null)
			{
				MainPage.Instance.HighlightDoc(_lastNearest.ViewModel.DocumentController, false, 2);
			}

			//highlight this linked doc
			_lastNearest = nearestOnCollection;
			MainPage.Instance.HighlightDoc(nearestOnCollection.ViewModel.DocumentController, false, 1);
		}

		/*
		//opens a menu in which the user can select a link to pursue 
		private void OpenLinkMenu(List<DocumentController> linksList, KeyController directionKey)
		{
			if (_selectedRegion == null) return;
			
			//create preview for menu based on contents of linkedDoc
			
			double stackX = _selectedRegion.Column1.ActualWidth;
			double stackY = _selectedRegion.Row1.ActualHeight + 10;
			xLinkStack.Margin = new Thickness(stackX, stackY, 0, 0);
			xLinkStack.MaxWidth = _selectedRegion.ActualWidth;
			xLinkStack.MaxHeight = _selectedRegion.ActualHeight;
			xLinkStack.Visibility = Visibility.Visible;
			
			_linkDict = new Dictionary<TextBlock, DocumentController>();

			//add contents of list as previews
			foreach (DocumentController linkedDoc in linksList)
			{
				//format
				TextBlock linkPreview = new TextBlock();
				linkPreview.Foreground = new SolidColorBrush(Colors.Black);
				linkPreview.TextDecorations = TextDecorations.Underline;
				linkPreview.FontSize = 12;

				//data
				var linkedDC = linkedDoc.GetDataDocument()
					.GetDereferencedField<ListController<DocumentController>>(directionKey, null).TypedData.First();
				linkPreview.Text = linkedDC.LayoutName + " : " + linkedDC.Title;
				linkPreview.Width = _selectedRegion.Width - 10;
				xLinkStack.Children.Add(linkPreview);

				//add to dictionary & attach click handler
				_linkDict.Add(linkPreview, linkedDC);
				linkPreview.PointerPressed += Link_OnPointerPressed;
			}
		}

		private void Link_OnPointerPressed(object sender, PointerRoutedEventArgs e)
		{
			e.Handled = false;
			if (sender is TextBlock) this.Region_Pressed(_selectedRegion, e.GetCurrentPoint(MainPage.Instance).Position, _linkDict[(TextBlock)sender]);
			e.Handled = true;
		}

		*/

		/*
		private void FormatLinkMenu()
		{
			xLinkFlyout.Closed += (s, e) =>
			{
				_isLinkMenuOpen = false;
				xLinkFlyout.Items.Clear();
			};

			xLinkFlyout.Opening += (s, e) =>
			{
				_isLinkMenuOpen = true;
			};
		}

		private void OpenLinkMenu(List<DocumentController> linksList, KeyController directionKey, Point point)
		{
			if (_isLinkMenuOpen == false)
			{
				//add all links as menu items
				foreach (DocumentController linkedDoc in linksList)
				{
					//format new item
					var linkItem = new MenuFlyoutItem();
					var dc = linkedDoc.GetDataDocument()
						.GetDereferencedField<ListController<DocumentController>>(directionKey, null).TypedData.First();
					linkItem.Text = dc.Title;
					linkItem.Click += (s, e) =>
					{
						this.Region_Pressed(_selectedRegion, point, dc);
					};

					// Add the item to the menu.
					xLinkFlyout.Items.Add(linkItem);

				}

				//var selectedText = (FrameworkElement) xRichEditBox.Document.Selection;
				xLinkFlyout.ShowAt(_selectedRegion);
			}

		}
		*/
	}

}