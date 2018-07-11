
using Dash.Controllers;
using Dash.Converters;
using Dash.FontIcons;
using Dash.Models.DragModels;
using Dash.Views.TemplateEditor;
using DashShared;
using Microsoft.Toolkit.Uwp.UI.Animations;
using Syncfusion.UI.Xaml.Controls.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Shapes;
using Line = Windows.UI.Xaml.Shapes.Line;
using Point = Windows.Foundation.Point;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class TemplateEditorView : UserControl
	{
		public DocumentController LayoutDocument { get; set; }
		public DocumentController DataDocument { get; set; }
		private DocumentController InitialDataDocument { get; set; }

		//initializing the list of layout documents contained within the template
		public ObservableCollection<DocumentController> DocumentControllers { get; set; }
		public ObservableCollection<DocumentController> InitialDocumentControllers { get; set; }

		//item source for xWorkspace
		public ObservableCollection<DocumentViewModel> DocumentViewModels { get; set; }

		public ObservableCollection<DocumentViewModel> ViewCopiesList { get;set;}
	    public Grid GridRoot => xItemsControlGrid.ItemsPanelRoot as Grid;

		public Collection<DocumentView> DocumentViews { get; set; }

        private KeyValueTemplatePane _keyValuePane;
		private DocumentView _selectedDocument;
		private Point _pasteWhereHack;
		private bool _isDataDocKvp = true;
		DataPackage dataPackage = new DataPackage();
		private FrameworkElement TemplateLayout = null;
	    
	    
		public TemplateEditorView()
		{
			this.InitializeComponent();

			xOuterPanel.BorderThickness = new Thickness(2, 8, 2, 2);

			DocumentControllers = new ObservableCollection<DocumentController>();
			DocumentViewModels = new ObservableCollection<DocumentViewModel>();
			ViewCopiesList = new ObservableCollection<DocumentViewModel>();
			DocumentViews = new Collection<DocumentView>();
			InitialDocumentControllers = new ObservableCollection<DocumentController>();
        }

	    private void WorkingDocumentView_DocumentDeleted()
	    {
	        var docView = this.GetFirstAncestorOfType<DocumentView>();
	        Actions.HideDocument(docView?.ParentCollection.ViewModel, LayoutDocument);
        }

        private void TemplateEditorView_DocumentDeleted(DocumentView sender,
			DocumentView.DocumentViewDeletedEventArgs args)
		{
		    Clear();
			if (LayoutDocument.GetField<DocumentController>(KeyStore.DataKey).GetDataDocument()
					.GetField(KeyStore.TemplateEditorKey) != null)
				LayoutDocument.GetField<DocumentController>(KeyStore.DataKey).GetDataDocument()
					.RemoveField(KeyStore.TemplateEditorKey);
		}

		private void DocumentControllers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					AddDocs(e.NewItems.Cast<DocumentController>());
					break;
				case NotifyCollectionChangedAction.Move:
					break;
				case NotifyCollectionChangedAction.Remove:
					RemoveDocs(e.OldItems.Cast<DocumentController>());
					break;
				case NotifyCollectionChangedAction.Replace:
					break;
				case NotifyCollectionChangedAction.Reset:
                    ClearDocs();
					break;
			}

			
		}

		private void RemoveDocs(IEnumerable<DocumentController> oldDocs)
		{
            // find a matching docment view model and remove it
		    DocumentViewModel rightDoc = null;
			foreach (var doc in oldDocs)
			{
				//ViewCopiesList.Removedoc.GetDelegates().Remove();
				rightDoc = DocumentViewModels.First(i => i.DocumentController.Equals(doc));
				if (rightDoc != null)
				{
					DocumentViewModels.Remove(rightDoc);
                    DataDocument.RemoveFromListField(KeyStore.DataKey, rightDoc.DocumentController);

					//delete corresponding copy displayed in ListView
					ViewCopiesList.Remove(this.FindListViewCopy(rightDoc));

					break;
				}
				
			}

			xItemsControlCanvas.ItemsSource = DocumentViewModels;
		}

		//finds the copy that has the same data doc as the removed doc & removes it
		private DocumentViewModel FindListViewCopy(DocumentViewModel origDoc)
		{
			foreach (var copy in ViewCopiesList)
			{
				if (copy.DataDocument.Equals(origDoc.DataDocument)) return copy;
			}
			return null;
		}

	    private void ClearDocs()
	    {
	        foreach (var documentViewModel in DocumentViewModels)
	        {
	            DataDocument.RemoveFromListField(KeyStore.DataKey, documentViewModel.DocumentController);
	        }
            DocumentViewModels.Clear();
	    }

		private void AddDocs(IEnumerable<DocumentController> newDocs)
		{
			foreach (var doc in newDocs)
			{
				AddDoc(doc);
			}
		}

		private void AddDoc(DocumentController addedDoc)
		{
            var workingDoc = LayoutDocument.GetField<DocumentController>(KeyStore.DataKey);
            // if either is true, then the layout doc needs to be abstracted
		    var doc = addedDoc;
            // if the document is related to the working document
		    if (doc.GetDataDocument().Equals(workingDoc.GetDataDocument()) || doc.GetDataDocument().Equals(workingDoc))
		    {
		        // set the layout document's context to a reference of the data document's context
		        doc.SetField(KeyStore.DocumentContextKey,
		            new DocumentReferenceController(DataDocument,
		                KeyStore.DocumentContextKey),
		            true);
                
		        var specificKey = doc.GetField<ReferenceController>(KeyStore.DataKey).FieldKey;
		        if (specificKey != null)
		        {
		            // set the field of the document's data key to a pointer reference to this documents' docContext's specific key
		            doc.SetField(KeyStore.DataKey,
		                new PointerReferenceController(
		                    doc.GetField<DocumentReferenceController>(KeyStore.DocumentContextKey), specificKey), true);
		        }
		    }

		    // create new viewmodel with a copy of document, set editor to this
            var dvm =
		        new DocumentViewModel(doc, new Context(doc));
		    DocumentViewModels.Add(dvm);

		    // set the row and column fields appropriately
		    dvm.DocumentController.SetField(KeyStore.RowKey, new NumberController(FindRow(dvm.YPos)), true);
		    dvm.DocumentController.SetField(KeyStore.ColumnKey, new NumberController(FindColumn(dvm.XPos)), true);
		    dvm.DocumentController.SetPosition(new Point(0, 0));

            // adds layout doc to list of layout docs
            DataDocument.AddToListField(KeyStore.DataKey, doc);
		    //var datakey = DataDocument.GetField<ListController<DocumentController>>(KeyStore.DataKey);
		    //datakey.Add(doc);

			//add copy for list view
			var copy = doc.GetViewCopy();
			copy.SetPosition(new Point(0, 0));
			ViewCopiesList.Add(new DocumentViewModel(copy, new Context(copy)));
		}

		private void XSwitchButton_Tapped(object sender, TappedRoutedEventArgs e)
		{
			_isDataDocKvp = !_isDataDocKvp;
		}

	    public void FormatPanes()
	    {
	        //make key value pane
	        if (xDataPanel.Children.Count == 0)
	        {
	            _keyValuePane = new KeyValueTemplatePane(this);
	            _keyValuePane.KVP.xSwitchButton.Tapped += XSwitchButton_Tapped;
	            xDataPanel.Children.Add(_keyValuePane);
	        }

	        //make central collection/canvas
	        DocumentControllers =
	            new ObservableCollection<DocumentController>(
	                DataDocument.GetField<ListController<DocumentController>>(KeyStore.DataKey)?.TypedData ??
	                new List<DocumentController>());
	    }

	    private void XWorkspace_OnLoaded(object sender, RoutedEventArgs e)
		{
			xFreeFormButton.Background = new SolidColorBrush(Colors.White);

			var workingDoc = LayoutDocument.GetField<DocumentController>(KeyStore.DataKey);
            //workingDoc.SetField(KeyStore.TemplateEditorKey, DataDocument);
            // if the working document is already a template box, initialize with that template
            if (workingDoc.GetField<DocumentController>(KeyStore.ActiveLayoutKey)?.DocumentType
		            .Equals(TemplateBox.DocumentType) ?? false)
		    {
		        DataDocument.SetField(KeyStore.DataKey,
		            workingDoc.GetField<DocumentController>(KeyStore.ActiveLayoutKey)
		                .GetField<ListController<DocumentController>>(KeyStore.DataKey), true);
				//check template style, override current template format if necessary
			    if (workingDoc.GetField<DocumentController>(KeyStore.ActiveLayoutKey)
				        .GetField<NumberController>(KeyStore.TemplateStyleKey)?.Data == TemplateConstants.ListView)
			    {
					this.FormatTemplateIntoList();
			    }
			    else
			    {
				    this.FormatTemplateIntoFreeform();
			    }
		    }

			//initialize UI of workspace
			this.FormatPanes();
			this.FormatUploadTemplateFlyout();
            
		    var rect = new Rect(0, 0, xWorkspace.Width, xWorkspace.Height);
			var rectGeo = new RectangleGeometry { Rect = rect };
			xWorkspace.Clip = rectGeo;

			// sets the minimum bounds and adds the resizing tool
			Bounds = new Rect(0, 0, 70, 70);
			var resizer = new ResizingControls(this);
			xOuterWorkspace.Children.Add(resizer);
			RelativePanel.SetAlignHorizontalCenterWithPanel(resizer, true);
			RelativePanel.SetAlignVerticalCenterWithPanel(resizer, true);

			//hide resize and ellipse controls for template editor
			var docView = this.GetFirstAncestorOfType<DocumentView>();
			docView.ViewModel.DisableDecorations = true;
			docView.hideControls();
            
            // determine if the active layout exists and has information about rows and columns
		    var activeLayout = workingDoc.GetField<DocumentController>(KeyStore.ActiveLayoutKey);
		    if (activeLayout?.GetField(KeyStore.RowInfoKey) != null || activeLayout?.GetField(KeyStore.ColumnInfoKey) != null)
		    {
                // change the template editor into a grid view
		        xItemsControlList.Visibility = Visibility.Collapsed;
		        xItemsControlCanvas.Visibility = Visibility.Collapsed;

		        xItemsControlGrid.Visibility = Visibility.Visible;
		        xGridLeftDragger.Visibility = Visibility.Visible;
		        xGridTopDragger.Visibility = Visibility.Visible;
		    }

            //MAKE TEMPLATE VIEW
            TemplateLayout = DataDocument.MakeViewUI(new Context());
			TemplateLayout.Width = xWorkspace.Width;
			TemplateLayout.Height = xWorkspace.Height;
			TemplateLayout.Drop += XWorkspace_OnDrop;

		    var workingDocView =
		        docView.ParentCollection.ViewModel.DocumentViewModels.FirstOrDefault(
		            i => i.DocumentController.Equals(workingDoc))?.Content.GetFirstAncestorOfType<DocumentView>();
            workingDocView.FadeOutBegin += WorkingDocumentView_DocumentDeleted;
            //xWorkspace.Children.Add(TemplateLayout);

            //initialize layout documents on workspace
            DocumentViewModels.Clear();
            // loop through each layout document in the data document's data key
			var layoutDocsList = DataDocument
				.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null).TypedData;
			foreach (var layoutDoc in layoutDocsList)
			{
                // add them logically to the document controllers list
				DocumentControllers.Add(layoutDoc);
                // add them graphically to the document view models list
                // we must do both here because as of yet, there are no collection changed handles
                DocumentViewModels.Add(new DocumentViewModel(layoutDoc));
				InitialDocumentControllers.Add(layoutDoc);
				//add copy for list view
				var copy = layoutDoc.GetViewCopy();
				copy.SetPosition(new Point(0, 0));
				ViewCopiesList.Add(new DocumentViewModel(copy, new Context(copy)));
			}

			// update item source
			xItemsControlCanvas.ItemsSource = DocumentViewModels;
			xItemsControlGrid.ItemsSource = DocumentViewModels;

			// set the document context of the data doc (template) to the working document's data doc
			DataDocument.SetField(KeyStore.DocumentContextKey,
				LayoutDocument.GetField<DocumentController>(KeyStore.DataKey).GetDataDocument(), true);

			//set background color
			var colorString = DataDocument.GetField<TextController>(KeyStore.BackgroundColorKey, true)?.Data ?? "#FFFFFF";
			var backgroundColor = new StringToBrushConverter().ConvertDataToXaml(colorString);
			xWorkspace.Background = backgroundColor;
			xBackgroundColorPreviewBox.Fill = xWorkspace.Background;
			xDesignGridSizeComboBox.SelectedIndex = 0;
			xDesignGridVisibilityButton.IsChecked = false;

            // TODO: Add number indicating which template perhoops -sy
            // if the title key doesn't exist or is empty
            if (DataDocument.GetField<TextController>(KeyStore.TitleKey) == null ||
				!DataDocument.GetField<TextController>(KeyStore.TitleKey).Data.Any())
			{
				// use a default title
				var title = "MyTemplate";
				xTitleBlock.Text = title;
				DataDocument.SetField(KeyStore.TitleKey, new TextController(title), true);
			}

			// create a new field binding to link the title with the editable text block
			var templateEditorBinding = new FieldBinding<TextController>
			{
				Document = DataDocument,
				Key = KeyStore.TitleKey,
				Mode = BindingMode.TwoWay
			};
			xTitleBlock.AddFieldBinding(EditableTextBlock.TextProperty, templateEditorBinding);

            // add event handlers
            DocumentControllers.CollectionChanged += DocumentControllers_CollectionChanged;
			xKeyBox.PropertyChanged += XKeyBox_PropertyChanged;
			docView.DocumentDeleted += TemplateEditorView_DocumentDeleted;

            // initialize the size of the workspace based on the working doc's width and height fields
            // determine if the active layout exists and if it is a template box
		    if (workingDoc.GetField<DocumentController>(KeyStore.ActiveLayoutKey)?.DocumentType
		            .Equals(TemplateBox.DocumentType) ?? false)
		    {
                // set the width to the working doc's width field if the width field is less than 500, otherwise 500
		        xWorkspace.Width = workingDoc.GetWidthField().Data < 500 ? workingDoc.GetWidthField().Data : 500;
		        xWorkspace.Height = workingDoc.GetHeightField().Data < 500 ? workingDoc.GetHeightField().Data : 500;
            }
		    else
		    {
                // otherwise, set the width and height to an arbitrary default
		        xWorkspace.Width = 300;
		        xWorkspace.Height = 400;
		    }
        }

        private void StyleWorkspace(int style)
		{
			switch (style)
			{
				case TemplateConstants.FreeformView:
					break;
				case TemplateConstants.ListView:
					break;
				case TemplateConstants.GridView:
					break;
			}
		}

		private void XWorkspace_OnUnloaded(object sender, RoutedEventArgs e)
		{
            // unload all event handlers
			DocumentControllers.CollectionChanged -= DocumentControllers_CollectionChanged;
		    xKeyBox.PropertyChanged -= XKeyBox_PropertyChanged;
            //TODO:FIX THIS LINE, DASH CRASHES
            //DataDocument.SetField<DocumentController>(KeyStore.TemplateEditorKey, this, true);
            //this.GetFirstAncestorOfType<DocumentView>().DocumentDeleted -= TemplateEditorView_DocumentDeleted;
        }

		private void FormatUploadTemplateFlyout()
		{
		    xUploadTemplateFlyout.Content = new TemplateApplier(LayoutDocument.GetField<DocumentController>(KeyStore.DataKey),
		        this.GetFirstAncestorOfType<DocumentView>().ParentCollection.ViewModel.DocumentViewModels);
		}
        
        /// <summary>
        ///     adds a rich text note to the template preview
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void TextButton_OnClick(object sender, RoutedEventArgs e)
		{
            DocumentControllers.Add(new RichTextNote("New text box").Document);
		}

        /// <summary>
        ///     opens a file picker for images and adds an image to the template preview
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private async void ImageButton_OnClick(object sender, RoutedEventArgs e)
		{
			//opens file picker and limits search by listed image extensions
			var imagePicker = new FileOpenPicker
			{
				ViewMode = PickerViewMode.Thumbnail,
				SuggestedStartLocation = PickerLocationId.PicturesLibrary
			};
			imagePicker.FileTypeFilter.Add(".jpg");
			imagePicker.FileTypeFilter.Add(".jpeg");
			imagePicker.FileTypeFilter.Add(".bmp");
			imagePicker.FileTypeFilter.Add(".png");
			imagePicker.FileTypeFilter.Add(".svg");

			//adds each image selected to Dash
			var imagesToAdd = await imagePicker.PickMultipleFilesAsync();

			if (imagesToAdd != null)
			{
				foreach (var thisImage in imagesToAdd)
				{
					var parser = new ImageToDashUtil();
					var docController = await parser.ParseFileAsync(thisImage);
					if (docController != null)
					{
					    var fileProperties = await thisImage.Properties.GetImagePropertiesAsync();
                        var originalHeight = (double) fileProperties.Height;
					    var originalWidth = (double) fileProperties.Width;
					    var calculatedHeight = originalHeight;
                        if (docController.GetWidthField().Data > xWorkspace.Width)
                        {
                            docController.SetWidth(xWorkspace.Width * 0.8);
                            var ratio = originalHeight / originalWidth;
                            calculatedHeight = ratio * xWorkspace.Width;
                        }

                        if (calculatedHeight > xWorkspace.Height)
                        {
                            var scale = xWorkspace.Height / originalHeight;
                            var calculatedWidth = originalWidth * scale;
                            docController.SetWidth(calculatedWidth * 0.8);
                        }
                        
                        DocumentControllers.Add(docController.GetViewCopy(new Point(0, 0)));
					}
				}
			}
		}

        /// <summary>
        ///     opens a file picker for videos and adds a new video to the template preview
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private async void VideoButton_OnClick(object sender, RoutedEventArgs e)
		{
			//instantiates a file picker, set to open in user's video library
			var picker = new FileOpenPicker
			{
				ViewMode = PickerViewMode.Thumbnail,
				SuggestedStartLocation = PickerLocationId.VideosLibrary
			};

			picker.FileTypeFilter.Add(".avi");
			picker.FileTypeFilter.Add(".mp4");
			picker.FileTypeFilter.Add(".wmv");

			//awaits user upload of video 
			var files = await picker.PickMultipleFilesAsync();
            
			if (files != null)
			{
				foreach (var file in files)
				{
					//create a doc controller for the video, set position, and add to canvas
					var docController = await new VideoToDashUtil().ParseFileAsync(file);
					if (docController != null)
					{
						if (docController.GetWidthField().Data >= xWorkspace.Width)
							docController.SetWidth(xWorkspace.Width - 20);
						// TODO: Check for if height is too large (may be difficult bcs height = nan? -sy
						DocumentControllers.Add(docController.GetViewCopy(new Point(0, 0)));
					}
				}

				//add error message for null file?
			}
		}

        /// <summary>
        ///     opens a file picker for audio and adds the audio to the template preview
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private async void AudioButton_OnClick(object sender, RoutedEventArgs e)
		{
			//instantiates a file picker, set to open in user's audio library
			var picker = new FileOpenPicker
			{
				ViewMode = PickerViewMode.Thumbnail,
				SuggestedStartLocation = PickerLocationId.MusicLibrary
			};

			picker.FileTypeFilter.Add(".mp3");


			//awaits user upload of audio 
			var files = await picker.PickMultipleFilesAsync();
            
			if (files != null)
			{
				foreach (var file in files)
				{
					//create a doc controller for the audio, set position, and add to canvas
					var docController = await new AudioToDashUtil().ParseFileAsync(file);
					if (docController != null)
					{
						if (docController.GetWidthField().Data >= xWorkspace.Width)
							docController.SetWidth(xWorkspace.Width - 20);
						DocumentControllers.Add(docController.GetViewCopy(new Point(0, 0)));
					}
				}

				//add error message for null file?
			}
		}

		private void TemplateAlignmentButton_OnChecked(object sender, RoutedEventArgs e)
		{
			var button = sender as AppBarButton;
			var alignment = this.ButtonNameToAlignment(button?.Name); 

			//for each document, align according to what button was pressed
			foreach (var dvm in DocumentViewModels)
			{
				AlignItem(alignment, dvm);
			}

		}
        
		private void ItemAlignmentButton_OnChecked(object sender, TappedRoutedEventArgs e)
		{
		    e.Handled = true;
			var button = sender as AppBarButton;
			var alignment = this.ButtonNameToAlignment(button?.Name);

			if (_selectedDocument != null) AlignItem(alignment, _selectedDocument?.ViewModel);
		}

        /// <summary>
        ///     determines which horizontal alignment enum to return
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
		private HorizontalAlignment ButtonNameToAlignment(string name)
		{
			if (name == "xAlignLeft" || name == "xAlignItemLeft")
			{
				return HorizontalAlignment.Left;
			}
			else if (name == "xAlignCenter" || name == "xAlignItemCenter")
			{
				return HorizontalAlignment.Center;
			}
			else if (name == "xAlignRight" || name == "xAlignItemRight")
			{
				return HorizontalAlignment.Right;
			}
			else
			{
				return HorizontalAlignment.Stretch;
			}
		}

		private void AlignItem(HorizontalAlignment alignment, DocumentViewModel dvm)
		{
            // aligns the item to the appropriate side and sets the position value of that item appropriately
			var point = dvm.LayoutDocument.GetField<PointController>(KeyStore.PositionFieldKey);
			switch (alignment)
			{
				case HorizontalAlignment.Left:
					point = new PointController(0, point.Data.Y);
					dvm.LayoutDocument.SetField(KeyStore.HorizontalAlignmentKey,
						new TextController(alignment.ToString()), true);
					dvm.LayoutDocument.SetField(KeyStore.UseHorizontalAlignmentKey, new BoolController(true), true);
					break;

				case HorizontalAlignment.Center:
					var centerX = (xWorkspace.Width - dvm.LayoutDocument.GetActualSize().Value.X) / 2;
					point = new PointController(centerX, point.Data.Y);
					dvm.LayoutDocument.SetField(KeyStore.HorizontalAlignmentKey,
						new TextController(alignment.ToString()), true);
					dvm.LayoutDocument.SetField(KeyStore.UseHorizontalAlignmentKey, new BoolController(true), true);
					break;

				case HorizontalAlignment.Right:
					var rightX = xWorkspace.Width - dvm.LayoutDocument.GetActualSize().Value.X;
					point = new PointController(rightX, point.Data.Y);
					dvm.LayoutDocument.SetField(KeyStore.HorizontalAlignmentKey,
						new TextController(alignment.ToString()), true);
					dvm.LayoutDocument.SetField(KeyStore.UseHorizontalAlignmentKey, new BoolController(true), true);
					break;
			}

			dvm.LayoutDocument.SetField(KeyStore.PositionFieldKey, point, true);

		}



        #region Borders

		private void BorderOption_OnChanged(object sender, RoutedEventArgs e)
		{
			// TODO: Consider if we really need this and want to put in the work to save borders for documents -sy
			//double left = 0;
			//if (xLeftBorderChecker.IsChecked.GetValueOrDefault(false))
			//{
			//    left = _thickness;
			//}

			//double top = 0;
			//if (xTopBorderChecker.IsChecked.GetValueOrDefault(false))
			//{
			//    top = _thickness;
			//}

			//double right = 0;
			//if (xRightBorderChecker.IsChecked.GetValueOrDefault(false))
			//{
			//    right = _thickness;
			//}

			//double bottom = 0;
			//if (xBottomBorderChecker.IsChecked.GetValueOrDefault(false))
			//{
			//    bottom = _thickness;
			//}

			//if (_selectedDocument != null)
			//{
			//    _selectedDocument.TemplateBorder.BorderBrush = new SolidColorBrush(_color);
			//    _selectedDocument.TemplateBorder.BorderThickness = new Thickness(left, top, right, bottom);
			//}
		}

#endregion


		// called when apply changes button is clicked
		private void ApplyChanges_OnClicked(object sender, RoutedEventArgs e)
		{
		    if (sender != null)
		    {
		        var toggle = sender as ToggleButton;
            }
		    if ((bool) xActivate.IsChecked)
		    {

				//update revert checkpoint
			    InitialDocumentControllers = new ObservableCollection<DocumentController>();
			    foreach (var doc in DocumentControllers)
			    {
					InitialDocumentControllers.Add(doc);
			    }
				
		        // layout document's data key holds the document that we are currently working on
		        var workingDoc = LayoutDocument.GetField<DocumentController>(KeyStore.DataKey);
		        // make a copy of the data document
		        var dataDocCopy = DataDocument.GetDataInstance();
		        // loop through each layout document and try to abstract it out when necessary

		        // set the dataDocCopy's document context key to the working document's data document
		        dataDocCopy.SetField(KeyStore.DocumentContextKey, workingDoc.GetDataDocument(), true);
		        // set the position of the data copy to the working document's position
		        dataDocCopy.SetField(KeyStore.PositionFieldKey,
		            workingDoc.GetField<PointController>(KeyStore.PositionFieldKey), true);
				
				//dataDocCopy.SetField(KeyStore.TemplateStyleKey,
				//    style, true);

		        if (xItemsControlGrid.Visibility == Visibility.Visible)
		        {
		            var rowInfo = new ListController<NumberController>(
		                GridRoot.RowDefinitions.Select(i =>
		                    new NumberController(i.ActualHeight)));
		            dataDocCopy.SetField(KeyStore.RowInfoKey, rowInfo, true);
		            var colInfo = new ListController<NumberController>(
		                GridRoot.ColumnDefinitions.Select(i =>
		                    new NumberController(i.ActualWidth)));
		            dataDocCopy.SetField(KeyStore.ColumnInfoKey, colInfo, true);
		        }  
                
		        // set the active layout of the working document to the dataDocCopy (which is the template)
		        workingDoc.SetField(KeyStore.ActiveLayoutKey, dataDocCopy, true); // changes workingDoc to template box
		        workingDoc.GetDataDocument().SetField(KeyStore.TemplateEditorKey,
		            this.GetFirstAncestorOfType<DocumentView>().ViewModel.DocumentController, true);
		        // let the working doc's title be the template's title
		        workingDoc.SetField(KeyStore.TitleKey, new DocumentReferenceController(DataDocument, KeyStore.TitleKey),
		            true);
				
			    //update template style
			    if (DataDocument.GetField<NumberController>(KeyStore.TemplateStyleKey)?.Data ==
			        TemplateConstants.ListView)
			    {
					this.FormatTemplateIntoList();
			    }
			}
        }

		private void DocumentView_OnLoaded(object sender, RoutedEventArgs e)
		{

		    
			var docView = sender as DocumentView;
			if (!DocumentViews.Contains(docView))
			{
				//adds any children in the template canvas, and hides the template canvas' ellipse functionality
				DocumentViews.Add(docView);
				docView.hideEllipses();
		    }

            // hacky way of resizing bounds, bob and tyler are working on improving resizing in general
		    var currPos = docView.ViewModel.DocumentController
		        .GetField<PointController>(KeyStore.PositionFieldKey).Data;
		    var calculatedHeight = docView.ActualHeight;
		    if (docView.ActualWidth > xWorkspace.Width)
		    {
		        docView.ViewModel.DocumentController.SetWidth(xWorkspace.Width * 0.8);
		        docView.ViewModel.DocumentController.SetField(KeyStore.PositionFieldKey, new PointController(0, currPos.Y), true);
		        var ratio = docView.ActualHeight / docView.ActualWidth;
		        calculatedHeight = ratio * xWorkspace.Width;
		    }
            else if (currPos.X + docView.ActualWidth > xWorkspace.Width)
		    {
		        docView.ViewModel.DocumentController.SetField(KeyStore.PositionFieldKey,
		            new PointController(xWorkspace.Width - docView.ActualWidth - 1, currPos.Y), true);
		    }

		    currPos = docView.ViewModel.DocumentController.GetField<PointController>(KeyStore.PositionFieldKey).Data;
		    if (calculatedHeight > xWorkspace.Height)
		    {
		        var scale = xWorkspace.Height / docView.ActualHeight;
		        var calculatedWidth = docView.ActualWidth * scale;
                docView.ViewModel.DocumentController.SetWidth(calculatedWidth * 0.8);
                //docView.ViewModel.DocumentController.SetActualSize(new Point(xWorkspace.Width * scale, xWorkspace.Height));
                docView.ViewModel.DocumentController.SetField(KeyStore.PositionFieldKey, new PointController(currPos.X, 0), true);
		    }
		    else if (currPos.Y + calculatedHeight > xWorkspace.Height)
		    {
		        docView.ViewModel.DocumentController.SetField(KeyStore.PositionFieldKey,
		            new PointController(currPos.X, xWorkspace.Height - calculatedHeight - 1), true);
		    }
			
            //updates and generates bounds for the children inside the template canvas
            var bounds = new Rect(0, 0, xWorkspace.Width,
				xWorkspace.Height);
			docView.Bounds = new RectangleGeometry { Rect = bounds };
			docView.DocumentSelected += DocView_DocumentSelected;
           
			docView.DocumentDeleted += DocView_DocumentDeleted;
		    docView.SizeChanged += DocumentView_OnSizeChanged;
            docView.ViewModel.LayoutDocument.AddFieldUpdatedListener(KeyStore.PositionFieldKey, PositionFieldChanged);
            xWorkspace.SizeChanged += XWorkspace_SizeChanged;
		}

		private void DocumentView_OnLoaded_ListView(object sender, RoutedEventArgs e)
		{
			var docView = sender as DocumentView;
			if (!DocumentViews.Contains(docView))
			{
				//adds any children in the template canvas, and hides the template canvas' ellipse functionality
				DocumentViews.Add(docView);
				docView.hideEllipses();
			}
			
			docView.DocumentSelected += DocView_DocumentSelected;
			docView.DocumentDeleted += DocView_DocumentDeleted;
			docView.SizeChanged += DocumentView_OnSizeChanged;
			
		}

        private void XWorkspace_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //var grid = sender as Grid;
            
            //docController.SetField<NumberController>(KeyStore.WidthFieldKey, xWorkspace.ActualWidth, true);
            //docController.SetField<NumberController>(KeyStore.HeightFieldKey, xWorkspace.ActualHeight, true);
        }

        private void PositionFieldChanged(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args,
	        Context context)
	    {
            // determine if a horizontal alignment key exists
	        if (sender.GetField<BoolController>(KeyStore.UseHorizontalAlignmentKey)?.Data ?? false)
	        {
	            switch (sender.GetField<TextController>(KeyStore.HorizontalAlignmentKey)?.Data)
	            {
	                case nameof(HorizontalAlignment.Left):
                        // determine if the position field is appropriate for the alignment it uses
	                    if (sender.GetField<PointController>(KeyStore.PositionFieldKey).Data.X != 0)
	                    {
                            // if the position is invalid, then remove the alignment
	                        sender.SetField(KeyStore.UseHorizontalAlignmentKey, new BoolController(false), true);
	                    }
	                    break;
	                case nameof(HorizontalAlignment.Center):
	                    if (sender.GetField<PointController>(KeyStore.PositionFieldKey).Data.X !=
	                        (xWorkspace.Width - sender.GetActualSize().Value.X) / 2)
	                    {
	                        sender.SetField(KeyStore.UseHorizontalAlignmentKey, new BoolController(false), true);
                        }
	                    break;
	                case nameof(HorizontalAlignment.Right):
	                    if (sender.GetField<PointController>(KeyStore.PositionFieldKey).Data.X !=
	                        xWorkspace.Width - sender.GetActualSize().Value.X)
	                    {
	                        sender.SetField(KeyStore.UseHorizontalAlignmentKey, new BoolController(false), true);
                        }
	                    break;
	            }
            }
	    }

	    private void DocView_DocumentDeleted(DocumentView sender, DocumentView.DocumentViewDeletedEventArgs args)
		{
			DocumentControllers.Remove(sender.ViewModel.DocumentController);
		    DocumentViews.Remove(sender);
		}

		private void DocView_DocumentSelected(DocumentView sender, DocumentView.DocumentViewSelectedEventArgs args)
		{
		    sender.Bounds = new RectangleGeometry {Rect = xWorkspace.GetBoundingRect(xWorkspace)};

			xKeyBox.PropertyChanged -= XKeyBox_PropertyChanged;
			_selectedDocument = sender;
            // get the pointer reference of the selected document
			var pRef = _selectedDocument.ViewModel.DocumentController.GetField<ReferenceController>(KeyStore.DataKey);
            // use the pointer reference to determine what key it is pointed to
			var specificKey = pRef?.FieldKey;
			var text = "";
			var workingDoc = LayoutDocument.GetField<DocumentController>(KeyStore.DataKey);
			var doc = _selectedDocument.ViewModel.DocumentController;
            // if the document is related to the working document
			if (doc.GetDataDocument().Equals(workingDoc.GetDataDocument()) || doc.GetDataDocument().Equals(workingDoc))
			{
                // display a # before displaying what key it is
				text = "#";
				text += specificKey;
			}
			else
			{
                // otherwise, just display the title of the document
				text = _selectedDocument.ViewModel.DocumentController.Title;
			}

			xKeyBox.Text = text;
			xKeyBox.PropertyChanged += XKeyBox_PropertyChanged;

			if (xFormatItemsButtonStack.Visibility == Visibility.Collapsed) ExpandButtonOnClick(xFormatItemsHeader, new RoutedEventArgs());

		}

	    private void XKeyBox_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
	    {
	        // determines if the keybox is in a state where the user has submitted the inputted text
	        if (!xKeyBox.TextBoxLoaded && _selectedDocument != null)
	        {
	            var text = xKeyBox.Text;
	            // determine if the text says that it references some key
	            if (text.StartsWith("#"))
	            {
	                var possibleKeyString = text.Substring(1);
	                // loop through each key value pair and find a matching key
	                var keyValuePairs = LayoutDocument.GetField<DocumentController>(KeyStore.DataKey).GetDataDocument()
	                    .EnumFields();
	                var specificKey = keyValuePairs.FirstOrDefault(kvp => kvp.Key.ToString().Equals(possibleKeyString))
	                    .Key;
	                var workingDoc = LayoutDocument.GetField<DocumentController>(KeyStore.DataKey);
	                // determine if there is a matching key
	                if (specificKey != null)
	                {
	                    // if so, create a new reference to that matching key and re-apply it to the doc
	                    var newRef =
	                        _selectedDocument.ViewModel.DocumentController.GetField<ReferenceController>(
	                            KeyStore.DataKey);
	                    var selectedDoc = _selectedDocument.ViewModel.DocumentController;
	                    if (selectedDoc.GetDataDocument().Equals(workingDoc.GetDataDocument()) ||
	                        selectedDoc.GetDataDocument().Equals(workingDoc))
	                    {
	                        newRef.FieldKey = specificKey;
	                    }
	                    else
	                    {
	                        DocumentReferenceController docRef;
	                        if (workingDoc.GetField(specificKey) != null)
	                        {
	                            docRef = new DocumentReferenceController(LayoutDocument, KeyStore.DataKey);
	                            newRef = new PointerReferenceController(docRef, specificKey);
	                        }
	                        else if (workingDoc.GetDataDocument().GetField(specificKey) != null)
	                        {
	                            docRef = new DocumentReferenceController(
	                                LayoutDocument.GetField<DocumentController>(KeyStore.DataKey), KeyStore.DataKey);
	                            newRef = new PointerReferenceController(docRef, specificKey);
	                        }
	                    }

	                    var dvm = DocumentViewModels.First(vm => vm.Equals(_selectedDocument.ViewModel));
	                    DocumentViewModels.Remove(dvm);
	                    var newDoc = DocumentControllers.First(doc =>
	                        doc.Equals(_selectedDocument.ViewModel.DocumentController));
	                    newDoc.SetField(KeyStore.DataKey, newRef, true);
	                    var newDvm = new DocumentViewModel(newDoc);
	                    DocumentViewModels.Add(newDvm);
	                    _selectedDocument = null;
	                }
	            }
	        }
	    }

	    #region OnDrop Mechanics

		private async void XWorkspace_OnDrop(object sender, DragEventArgs e)
		{
			using (UndoManager.GetBatchHandle())
			{
				e.Handled = true;
				// accept move, then copy, and finally accept whatever they requested (for now)
				if (e.AllowedOperations.HasFlag(DataPackageOperation.Move))
					e.AcceptedOperation = DataPackageOperation.Move;
				else if (e.AllowedOperations.HasFlag(DataPackageOperation.Copy))
					e.AcceptedOperation = DataPackageOperation.Copy;
				else e.AcceptedOperation = e.DataView.RequestedOperation;

				//RemoveDragDropIndication(sender as UserControl);

				var where = e.GetPosition(sender as Grid);
				if (DocumentViewModels.Count > 0)
				{
					var lastPos = DocumentViewModels.Last().Position;
					where = e.GetPosition(xWorkspace);
				}

				if (xItemsControlList.Visibility == Visibility.Visible) where = new Point(0, 0);

				// if we drag from the file system
				if (e.DataView?.Contains(StandardDataFormats.StorageItems) == true)
				{
					try
					{
						var droppedDoc = await FileDropHelper.HandleDrop(where, e.DataView, null);
						if (droppedDoc != null)
							DocumentControllers.Add(droppedDoc.GetViewCopy(new Point(0, 0)));
						return;
					}
					catch (Exception exception)
					{
						Debug.WriteLine(exception);
					}
				}

				if (e.DataView?.Contains(StandardDataFormats.Html) == true)
				{
					_pasteWhereHack = where;
					var html = await e.DataView.GetHtmlFormatAsync();

					//Overrides problematic in-line styling pdf.js generates, such as transparent divs and translucent elements
					html = String.Concat(html,
						@"<style>
                      div
                      {
                        color: black !important;
                      }
                      html * {
                        opacity: 1.0 !important
                      }
                    </style>"
					);

					var splits = new Regex("<").Split(html);
					var imgs = splits.Where((s) => new Regex("img.*src=\"[^>\"]*").Match(s).Length > 0).ToList();
					var text = e.DataView.Contains(StandardDataFormats.Text)
						? (await e.DataView.GetTextAsync()).Trim()
						: "";
					if (string.IsNullOrEmpty(text) && imgs.Count == 1)
					{
						var srcMatch = new Regex("[^-]src=\"[^{>?}\"]*").Match(imgs.First().ToString()).Value;
						var src = srcMatch.Substring(6, srcMatch.Length - 6);
						var imgNote = new ImageNote(new Uri(src), where, new Size(), src.ToString());
						DocumentControllers.Add(imgNote.Document.GetViewCopy(new Point(0, 0)));
						return;
					}

					//copy html to clipboard
					dataPackage.RequestedOperation = DataPackageOperation.Copy;
					dataPackage.SetHtmlFormat(html);
					Clipboard.SetContent(dataPackage);

					//to import from html
					// create a ValueSet from the datacontext, used to create word doc to copy html to
					var table = new ValueSet { { "REQUEST", "HTML to RTF" } };

					await DotNetRPC.CallRPCAsync(table);

					var dataPackageView = Clipboard.GetContent();
					var richtext = await dataPackageView.GetRtfAsync();
					var htmlNote = new RichTextNote(richtext, _pasteWhereHack, new Size(300, 300)).Document;




					var strings = text.Split(new char[] { '\r' });
					foreach (var str in html.Split(new char[] { '\r' }))
					{
						var matches = new Regex("^SourceURL:.*").Matches(str.Trim());
						if (matches.Count != 0)
						{
							htmlNote.GetDataDocument().SetField<TextController>(KeyStore.SourecUriKey,
								matches[0].Value.Replace("SourceURL:", ""), true);
							break;
						}
					}

					if (imgs.Count() == 0)
					{
						var matches = new Regex(".{1,100}:.*").Matches(text.Trim());
						var title = (matches.Count == 1 && matches[0].Value == text)
							? new Regex(":").Split(matches[0].Value)[0]
							: "";
						htmlNote.GetDataDocument().SetField<TextController>(KeyStore.DocumentTextKey, text, true);
						if (title == "")
							foreach (var match in matches)
							{
								var pair = new Regex(":").Split(match.ToString());
								htmlNote.GetDataDocument().SetField<TextController>(new KeyController(pair[0], pair[0]),
									pair[1].Trim(), true);
							}
						else
							htmlNote.SetTitle(title);
					}
					else
					{
						var related = new List<DocumentController>();
						foreach (var img in imgs)
						{
							var srcMatch = new Regex("[^-]src=\"[^{>?}\"]*").Match(img.ToString()).Value;
							var src = srcMatch.Substring(6, srcMatch.Length - 6);
							var i = new ImageNote(new Uri(src), new Point(), new Size(), src.ToString());
							related.Add(i.Document);
						}

						htmlNote.GetDataDocument()
							.SetField<ListController<DocumentController>>(
								new KeyController("Html Images", "Html Images"), related, true); //
																								 //htmlNote.GetDataDocument(null).SetField(new KeyController("Html Images", "Html Images"), new ListController<DocumentController>(related), true);
						htmlNote.GetDataDocument().SetField<TextController>(KeyStore.DocumentTextKey, text, true);
						foreach (var str in strings)
						{
							var matches = new Regex("^.{1,100}:.*").Matches(str.Trim());
							if (matches.Count != 0)
							{
								foreach (var match in matches)
								{
									var pair = new Regex(":").Split(match.ToString());
									htmlNote.GetDataDocument()
										.SetField<TextController>(new KeyController(pair[0], pair[0]), pair[1].Trim(),
											true);
								}
							}
						}
					}

					DocumentControllers.Add(htmlNote.GetViewCopy(new Point(0, 0)));
				}
				else if (e.DataView?.Contains(StandardDataFormats.Rtf) == true)
				{
					var text = await e.DataView.GetRtfAsync();

					var t = new RichTextNote(text, where, new Size(300, double.NaN));
					DocumentControllers.Add(t.Document.GetViewCopy(new Point(0, 0)));
				}
				else if (e.DataView?.Contains(StandardDataFormats.Text) == true)
				{
					var text = await e.DataView.GetTextAsync();
					var t = new RichTextNote(text, where, new Size(300, double.NaN));
					var matches = new Regex(".*:.*").Matches(text);
					foreach (var match in matches)
					{
						var pair = new Regex(":").Split(match.ToString());
					    t.Document.GetDataDocument()
					        .SetField<TextController>(new KeyController(pair[0]), pair[1].Trim('\r'),
					            true);
                    }

					DocumentControllers.Add(t.Document.GetViewCopy(new Point(0, 0)));
				}
				else if (e.DataView?.Contains(StandardDataFormats.Bitmap) == true)
				{
					var bmp = await e.DataView.GetBitmapAsync();
					IRandomAccessStreamWithContentType streamWithContent = await bmp.OpenReadAsync();
					byte[] buffer = new byte[streamWithContent.Size];
					using (DataReader reader = new DataReader(streamWithContent))
					{
						await reader.LoadAsync((uint)streamWithContent.Size);
						reader.ReadBytes(buffer);
					}

					var localFolder = ApplicationData.Current.LocalFolder;
					var uniqueFilePath =
						UtilShared.GenerateNewId() + ".jpg"; // somehow this works for all images... who knew
					var localFile =
						await localFolder.CreateFileAsync(uniqueFilePath, CreationCollisionOption.ReplaceExisting);
					localFile.OpenStreamForWriteAsync().Result.Write(buffer, 0, buffer.Count());

					var img = await ImageToDashUtil.CreateImageBoxFromLocalFile(localFile, "dropped image");
					DocumentControllers.Add(img.GetViewCopy(new Point(0, 0)));
					var t = new ImageNote(new Uri(localFile.FolderRelativeId));
					// var t = new AnnotatedImage(null, Convert.ToBase64String(buffer), "", "");
					DocumentControllers.Add(t.Document.GetViewCopy(new Point(0, 0)));
				}
				else if (e.DataView?.Properties.ContainsKey(nameof(DragCollectionFieldModel)) == true)
				{
					var dragData = (DragCollectionFieldModel)e.DataView.Properties[nameof(DragCollectionFieldModel)];
					var showField = dragData.FieldKey;

					if (showField != null && dragData.CollectionReference != null)
					{
						var subDocs = new List<DocumentController>();
						if (dragData.DraggedItems?.Any() == true)
						{
							var firstDocValue = dragData.DraggedItems.First().GetDataDocument()
								.GetDereferencedField(showField, null);
							if (firstDocValue is ListController<DocumentController> ||
								firstDocValue?.GetValue(null) is List<FieldControllerBase>)
								showField = expandCollection(dragData.FieldKey, dragData.DraggedItems, subDocs,
									showField);
							else if (firstDocValue is DocumentController)
								subDocs = dragData.DraggedItems.Select((d) =>
										d.GetDataDocument().GetDereferencedField<DocumentController>(showField, null))
									.ToList();
							else subDocs = pivot(dragData.DraggedItems, showField);
						}

						var cnote = new CollectionNote(where, dragData.ViewType);
						if (subDocs != null)
							cnote.SetDocuments(new List<DocumentController>(subDocs));
						else
							cnote.Document.GetDataDocument()
								.SetField(KeyStore.DataKey, dragData.CollectionReference, true);
						cnote.Document.SetField(CollectionDBView.FilterFieldKey, showField, true);
						DocumentControllers.Add(cnote.Document.GetViewCopy(new Point(0, 0)));
					}
					else
					{
						var parentDocs = (sender as FrameworkElement)?.GetAncestorsOfType<CollectionView>()
							.Select((cv) => cv.ParentDocument?.ViewModel?.DataDocument);
						var filteredDocs = dragData.DraggedItems.Where((d) =>
							!parentDocs.Contains(d.GetDataDocument()) &&
							d?.DocumentType?.Equals(DashConstants.TypeStore.MainDocumentType) == false);

						var payloadLayoutDelegates = filteredDocs.Select((p) =>
						{
							if (p.GetActiveLayout() == null &&
								p.GetDereferencedField(KeyStore.DocumentContextKey, null) == null)
								p.SetActiveLayout(new DefaultLayout().Document, true, true);
							var newDoc = e.AcceptedOperation == DataPackageOperation.Move ? p.GetSameCopy(where) :
								e.AcceptedOperation == DataPackageOperation.Link ? p.GetKeyValueAlias(where) :
								p.GetCopy(where);
							if (double.IsNaN(newDoc.GetWidthField().Data))
								newDoc.SetWidth(dragData.Width ?? double.NaN);
							if (double.IsNaN(newDoc.GetHeightField().Data))
								newDoc.SetHeight(dragData.Height ?? double.NaN);
							return newDoc;
						});
						DocumentControllers.Add(new CollectionNote(where, dragData.ViewType, 500, 300,
							payloadLayoutDelegates.ToList()).Document);
					}
				}
				// if the user drags a data document
				else if (e.DataView?.Properties.ContainsKey(nameof(List<DragDocumentModel>)) == true)
				{
					var dragModel = (List<DragDocumentModel>)e.DataView.Properties[nameof(List<DragDocumentModel>)];
					foreach (var d in dragModel.Where((dm) => dm.CanDrop(sender as FrameworkElement)))
					{
						var start = dragModel.First().DraggedDocument.GetPositionField().Data;
						foreach (var doc in (dragModel.Where((dm) => dm.CanDrop(sender as FrameworkElement)).Select(
							(dm) => dm.GetDropDocument(new Point(
								dm.DraggedDocument.GetPositionField().Data.X - start.X + where.X,
								dm.DraggedDocument.GetPositionField().Data.Y - start.Y + where.Y), true)).ToList()))
						{
							DocumentControllers.Add(doc.GetViewCopy(new Point(0, 0)));
						}
					}
				}
				// if the user drags a data document
				else if (e.DataView?.Properties.ContainsKey(nameof(DragDocumentModel)) == true)
				{
					var dragModel = (DragDocumentModel)e.DataView.Properties[nameof(DragDocumentModel)];
					if (dragModel.LinkSourceView != null
					) // The LinkSourceView is non-null when we're dragging the green 'link' dot from a document
					{
						// bcz:  Needs to support LinksFrom as well as LinksTo...
						if (MainPage.Instance.IsShiftPressed()
						) // if shift is pressed during this drag, we want to see all the linked documents to this document as a collection
						{
							var regions = dragModel.DraggedDocument.GetDataDocument()
								.GetDereferencedField<ListController<DocumentController>>(KeyStore.RegionsKey, null)
								?.TypedData;
							if (regions != null)
							{
								var links = regions.SelectMany((r) =>
									r.GetDataDocument().GetLinks(KeyStore.LinkToKey).TypedData);
								var targets = links.SelectMany((l) =>
									l.GetDataDocument().GetLinks(KeyStore.LinkToKey).TypedData);
								var aliases = targets.Select((t) =>
								{
									var vc = t.GetViewCopy();
									vc.SetHidden(false);
									return vc;
								});
								var cnote = new CollectionNote(where, CollectionView.CollectionViewType.Grid, 500, 300,
									aliases.ToList());
								DocumentControllers.Add(cnote.Document.GetViewCopy(new Point(0, 0)));
							}
						}
						else if (MainPage.Instance.IsCtrlPressed()
						) // if control is pressed during this drag, we want to see a collection of the actual link documents
						{
							var regions = dragModel.DraggedDocument.GetDataDocument()
								.GetDereferencedField<ListController<DocumentController>>(KeyStore.RegionsKey, null)
								?.TypedData;
							var directlyLinkedTo = dragModel.DraggedDocument.GetDataDocument()
								.GetLinks(KeyStore.LinkToKey)?.TypedData;
							var regionLinkedTo = regions?.SelectMany((r) =>
								r.GetDataDocument().GetLinks(KeyStore.LinkToKey)?.TypedData);
							if (regionLinkedTo != null || directlyLinkedTo != null)
							{
								var links = regionLinkedTo != null
									? regionLinkedTo.ToList()
									: new List<DocumentController>();
								if (directlyLinkedTo != null)
									links.AddRange(directlyLinkedTo);
								var cnote = new CollectionNote(where, CollectionView.CollectionViewType.Grid, 500, 300,
									links.ToList());
								DocumentControllers.Add(cnote.Document.GetViewCopy(new Point(0, 0)));
							}
						}
						else // if no modifiers are pressed, we want to create a new annotation document and link it to the source document (region)
						{
							var dragDoc = dragModel.DraggedDocument;
							if (dragModel.LinkSourceView != null &&
								KeyStore.RegionCreator[dragDoc.DocumentType] != null)
								dragDoc = KeyStore.RegionCreator[dragDoc.DocumentType](dragModel.LinkSourceView);
							var note = new RichTextNote("<annotation>", where).Document;
							dragDoc.Link(note);
							DocumentControllers.Add(note.GetViewCopy(new Point(0, 0)));
						}
					}
					else if (dragModel.CanDrop(sender as FrameworkElement))
					{
					    DocumentController dropDoc;
                        var workingDoc = LayoutDocument.GetField<DocumentController>(KeyStore.DataKey);
					    var workingDataDoc = workingDoc.GetDataDocument();
					    if (dragModel.DraggedDocument.Equals(workingDoc) || dragModel.DraggedDocument.Equals(workingDataDoc))
					    {
					        dropDoc = new DataBox(null, where.X, where.Y).Document;
					        dropDoc.SetField(KeyStore.DataKey, new PointerReferenceController(
					            new DocumentReferenceController(dropDoc, KeyStore.DocumentContextKey), dragModel.DraggedKey), true);
                            dropDoc.Tag = "DraggedKey doc";
					        dropDoc.SetField(KeyStore.DocumentContextKey,
					            new DocumentReferenceController(DataDocument, KeyStore.DocumentContextKey), true);
                            //dbox.SetField(KeyStore.DataKey,
                            //    new PointerReferenceController(new DocumentReferenceController(dbox.Id, KeyStore.DocumentContextKey), DraggedKey), true);
					        dropDoc.SetTitle(dragModel.DraggedKey.Name);
                        }
					    else
					    {
					        dropDoc = dragModel.GetDropDocument(where);
					    }

					    DocumentControllers.Add(dropDoc);
						xItemsControlCanvas.ItemsSource = DocumentViewModels;
						xItemsControlGrid.ItemsSource = DocumentViewModels;
					}
				}
			}
		}

		KeyController expandCollection(KeyController fieldKey, List<DocumentController> getDocs,
			List<DocumentController> subDocs, KeyController showField)
		{
			foreach (var d in getDocs)
			{
				var fieldData = d.GetDataDocument().GetDereferencedField(fieldKey, null);
				if (fieldData is ListController<DocumentController>)
					foreach (var dd in (fieldData as ListController<DocumentController>).TypedData)
					{
						var dataDoc = dd.GetDataDocument();

						var expandedDoc = new DocumentController(new Dictionary<KeyController, FieldControllerBase>(),
							DocumentType.DefaultType);
						expandedDoc.SetField(KeyStore.HeaderKey, d.GetDataDocument(), true);
						expandedDoc.SetField(showField, dataDoc, true);
						subDocs.Add(expandedDoc);
					}
				else if (fieldData is ListController<TextController>)
					foreach (var dd in (fieldData as ListController<TextController>).Data)
					{
						var expandedDoc = new DocumentController(new Dictionary<KeyController, FieldControllerBase>(),
							DocumentType.DefaultType);
						expandedDoc.SetField(KeyStore.HeaderKey, d.GetDataDocument(), true);
						expandedDoc.SetField(showField, new TextController((dd as TextController).Data), true);
						subDocs.Add(expandedDoc);
					}
				else if (fieldData is ListController<NumberController>)
					foreach (var dd in (fieldData as ListController<NumberController>).Data)
					{
						var expandedDoc = new DocumentController(new Dictionary<KeyController, FieldControllerBase>(),
							DocumentType.DefaultType);
						expandedDoc.SetField(KeyStore.HeaderKey, d.GetDataDocument(), true);
						expandedDoc.SetField(showField, new NumberController((dd as NumberController).Data), true);
						subDocs.Add(expandedDoc);
					}
			}

			return showField;
		}

		List<DocumentController> pivot(List<DocumentController> docs, KeyController pivotKey)
		{
			var dictionary = new Dictionary<object, Dictionary<KeyController, List<object>>>();
			var pivotDictionary = new Dictionary<object, DocumentController>();

			foreach (var d in docs.Select((dd) => dd.GetDataDocument()))
			{
				var fieldDict = setupPivotDoc(pivotKey, dictionary, pivotDictionary, d);
				if (fieldDict == null)
					continue;
				foreach (var f in d.EnumDisplayableFields())
					if (!f.Key.Equals(pivotKey))
					{
						if (!fieldDict.ContainsKey(f.Key))
						{
							fieldDict.Add(f.Key, new List<object>());
						}

						fieldDict[f.Key].Add(f.Value.GetValue(new Context(d)));
					}
			}

			var pivoted = new List<DocumentController>();
			foreach (var d in dictionary)
			{
				var doc = pivotDictionary.ContainsKey(d.Key) ? pivotDictionary[d.Key] : null;
				if (doc == null)
					continue;
				foreach (var f in d.Value)
					if (doc.GetField(f.Key) == null)
					{
						var items = new List<FieldControllerBase>();
						foreach (var i in f.Value)
						{
							if (i is string)
								items.Add(new TextController(i as string));
							else if (i is double)
								items.Add(new NumberController((double)i));
							else if (i is DocumentController)
								items.Add((DocumentController)i);
						}

						if (items.Count > 0)
						{
							FieldControllerBase field = null;

							//TODO tfs: why are we making copies of all of these fields?
							if (items.First() is TextController)
								field = (items.Count == 1)
									? (FieldControllerBase)new TextController((items.First() as TextController).Data)
									: new ListController<TextController>(items.OfType<TextController>());
							else if (items.First() is NumberController)
								field = (items.Count == 1)
									? (FieldControllerBase)new NumberController((items.First() as NumberController)
										.Data)
									: new ListController<NumberController>(items.OfType<NumberController>());
							else if (items.First() is RichTextController)
								field = (items.Count == 1)
									? (FieldControllerBase)new RichTextController((items.First() as RichTextController)
										.Data)
									: new ListController<RichTextController>(items.OfType<RichTextController>());
							else if (items.First() is DocumentController)
								field = (items.Count == 1)
									? (FieldControllerBase)(items.First() as DocumentController)
									: new ListController<DocumentController>(items.OfType<DocumentController>());
							if (field != null)
								doc.SetField(f.Key, field, true);
						}
					}

				pivoted.Add(doc);
			}

			return pivoted;
		}

		Dictionary<KeyController, List<object>> setupPivotDoc(KeyController pivotKey,
			Dictionary<object, Dictionary<KeyController, List<object>>> dictionary,
			Dictionary<object, DocumentController> pivotDictionary, DocumentController d)
		{
			var obj = d.GetDataDocument().GetDereferencedField(pivotKey, null)?.GetValue(null);
			DocumentController pivotDoc = null;
			if (obj != null && !dictionary.ContainsKey(obj))
			{
				var pivotField = d.GetDataDocument().GetField(pivotKey);
				pivotDoc = (pivotField as ReferenceController)?.GetDocumentController(null);
				if (d.GetDataDocument().GetAllPrototypes().Contains(pivotDoc) || pivotDoc == null ||
					pivotDoc.DocumentType.Equals(DashConstants.TypeStore.OperatorType))
				{
					pivotDoc = new DocumentController(new Dictionary<KeyController, FieldControllerBase>()
					{
					}, DocumentType.DefaultType);
					if (obj is string)
					{
						pivotDoc.SetField(pivotKey, new TextController(obj as string), true);
					}
					else if (obj is RichTextModel.RTD)
					{
						pivotDoc.SetField(pivotKey, new RichTextController(obj as RichTextModel.RTD), true);
					}
					else if (obj is double)
					{
						pivotDoc.SetField(pivotKey, new NumberController((double)obj), true);
					}
					else if (obj is DocumentController)
					{
						pivotDoc = obj as DocumentController;
					}
					else if (obj is ListController<DocumentController>)
					{
						pivotDoc.SetField(pivotKey,
							new ListController<DocumentController>(obj as List<DocumentController>), true);
					}

					//DBTest.DBDoc.AddChild(pivotDoc);
					d.SetField(pivotKey, new DocumentReferenceController(pivotDoc, pivotKey), true);
				}

				pivotDictionary.Add(obj, pivotDoc);
				dictionary.Add(obj, new Dictionary<KeyController, List<object>>());
			}

			if (obj != null)
			{
				d.SetField(pivotKey, new DocumentReferenceController(pivotDictionary[obj], pivotKey), true);
				return dictionary[obj];
			}

			return null;
		}

		#endregion

	    private void ExpandButtonOnClick(object sender, RoutedEventArgs e)
	    {
	        var button = sender as StackPanel;
	        StackPanel stack = null;
	        FontAwesome arrow = null;
	        Storyboard animation = null;
	        //toggle visibility of sub-buttons according to what header button was pressed
	        switch (button?.Name)
	        {
	            case "xAddItemsHeader":
	                stack = xAddItemsButtonStack;
	                arrow = xAddItemsArrow;
	                animation = xFadeAnimation;
	                break;
	            case "xFormatItemsHeader":
	                stack = xFormatItemsButtonStack;
	                arrow = xFormatItemsArrow;
	                animation = xFadeAnimationFormat;
		            break;
	            case "xOptionsHeader":
	                stack = xOptionsButtonStack;
	                arrow = xOptionsArrow;
	                animation = xFadeAnimationOptions;
	                break;
	        }

	        if (stack != null && arrow != null) this.ToggleButtonState(stack, arrow, animation);
	    }

	    private void ToggleButtonState(StackPanel buttonStack, FontAwesome arrow, Storyboard fade)
		{
			var centX = (float)xAddItemsArrow.ActualWidth / 2;
			var centY = (float)xAddItemsArrow.ActualHeight / 2;

			if (buttonStack.Visibility == Visibility.Visible)
			{
				arrow.Rotate(value: 0.0f, centerX: centX, centerY: centY, duration: 300, delay: 0,
					easingType: EasingType.Default).Start();
				buttonStack.Visibility = Visibility.Collapsed;

			}
			else
			{
				arrow.Rotate(value: -90.0f, centerX: centX, centerY: centY, duration: 300, delay: 0,
					easingType: EasingType.Default).Start();
				buttonStack.Visibility = Visibility.Visible;
				fade?.Begin();
			}
		}

		private void XResetButton_OnClick(object sender, RoutedEventArgs e)
		{
			Clear();
            var workingDoc = LayoutDocument.GetField<DocumentController>(KeyStore.DataKey);
            // if we have an active layout
		    if (workingDoc.GetField(KeyStore.ActiveLayoutKey) is DocumentController activeLayout)
		    {
                // remove it
		        workingDoc.RemoveField(KeyStore.ActiveLayoutKey);
                // remove the active layout's document context
		        activeLayout.RemoveField(KeyStore.DocumentContextKey);
		    }
		}

		private void XClearButton_OnClick(object sender, RoutedEventArgs e)
		{
		    Clear();
		}

	    private void Clear()
	    {
	        DocumentControllers.Clear();
		    ViewCopiesList.Clear();
	    }

		private void XUploadTemplate_OnClick(object sender, RoutedEventArgs e)
		{
            xUploadTemplateFlyout.Content = new TemplateApplier(LayoutDocument.GetField<DocumentController>(KeyStore.DataKey),
                this.GetFirstAncestorOfType<DocumentView>().ParentCollection.ViewModel.DocumentViewModels);
            xUploadTemplateFlyout.ShowAt(xUploadTemplateButton);
		}

		//updates the bounding when an element changes size
		private void DocumentView_OnSizeChanged(object sender, SizeChangedEventArgs e)
		{
		    var docView = sender as DocumentView;
           
		   

            //      if (docView.Bounds.Rect.Width != null && docView.ActualWidth + docView.ViewModel.XPos > docView.Bounds.Rect.Width)
            //{
            //    docView.Width = docView.Bounds.Rect.Width;
            //}

            //if (docView.Bounds.Rect.Height != null && docView.ActualHeight + docView.ViewModel.YPos > docView.Bounds.Rect.Height)
            //{
            //    docView.Height = docView.Bounds.Rect.Height;
            //}

            //var bounds = new Rect(0, 0, xWorkspace.Width - docView.ActualWidth,
            //	xWorkspace.Height - docView.ActualHeight);

            //         docView.Bounds = new RectangleGeometry { Rect = bounds };

        }

		private void XItemsExpander_OnExpanded(object sender, EventArgs e)
		{
			//xItemsExpander.Background = new SolidColorBrush(Color.FromArgb(255, 85, 102, 102));
		}


		//if we want to change the buttons on hover
		private void XExpansionGrid_OnPointerEntered(object sender, PointerRoutedEventArgs e)
		{
			//XAddItemsGrid.Background = new SolidColorBrush(Color.FromArgb(255, 65, 104, 87));
		}


		private void XExpansionGrid_OnPointerExited(object sender, PointerRoutedEventArgs e)
		{
			if (xAddItemsButtonStack.Visibility == Visibility.Collapsed)
			{
				//XAddItemsGrid.Background = new SolidColorBrush(Colors.Transparent);
			}

		}

		private void xBackground_SelectedColorChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			var colorPicker = sender as SfColorPicker;
			if (colorPicker != null)
			{
				var color = colorPicker.SelectedColor;
				var brush = new SolidColorBrush(color);
				//set template background to this color
				xWorkspace.Background = brush;
				//update color preview box
				xBackgroundColorPreviewBox.Fill = brush;
				//update key
				DataDocument.SetField<TextController>(KeyStore.BackgroundColorKey, color.ToString(), true);
			}
		}

		private void XBackgroundColorPreviewBox_OnTapped(object sender, TappedRoutedEventArgs e)
		{
			FlyoutBase.ShowAttachedFlyout(xBackgroundColorPreviewBox);
		}
		
		//highlights ellipse on pointer entered
		private void Ellipse_OnPointerEntered(object sender, PointerRoutedEventArgs e)
		{
			var grid = sender as Grid;
			var ellipse = grid?.GetFirstDescendantOfType<Ellipse>();
			if (ellipse != null)
			{
				grid.Width = 29;
				grid.Height = 29;
				grid.Opacity = 1;
			}
		}

		//un-highlights ellipse on pointer exited
		private void Ellipse_OnPointerExited(object sender, PointerRoutedEventArgs e)
		{
			var grid = sender as Grid;
			var ellipse = grid?.GetFirstDescendantOfType<Ellipse>();
			if (ellipse != null)
			{
				grid.Width = 25;
				grid.Height = 25;
				grid.Opacity = .75;
			}

		}

	    public Rect Bounds;

	    public void ResizeCanvas(Size newSize)
	    {
            // determine if the workspace has a valid width and height
	        if (double.IsNaN(xWorkspace.Width) || double.IsNaN(xWorkspace.Height))
	        {
                // set the workspace clipping
	            xWorkspace.Clip = new RectangleGeometry { Rect = new Rect(0, 0, xWorkspace.Width, xWorkspace.Height) };
	            Bounds.Width = 70;
	            Bounds.Height = 70;
	            return;
	        }

            // save the old size as the workspace's width and height
	        var oldSize = new Size(xWorkspace.Width, xWorkspace.Height);
	        var topLeft = new Point(double.PositiveInfinity, double.PositiveInfinity);
	        var bottomRight = new Point(double.NegativeInfinity, double.NegativeInfinity);
            // loop through each document view and find the furthest top left and bottom right points
	        foreach (var docview in DocumentViews)
	        {
	            topLeft.X = Math.Min(topLeft.X, docview.ViewModel.XPos);
	            topLeft.Y = Math.Min(topLeft.Y, docview.ViewModel.YPos);
	            bottomRight.X = Math.Max(bottomRight.X, docview.ViewModel.XPos + docview.ViewModel.ActualSize.X);
	            bottomRight.Y = Math.Max(bottomRight.Y, docview.ViewModel.YPos + docview.ViewModel.ActualSize.Y);
            }
          
            // maintain their positions by changing their value's offsets
	        foreach (var docview in DocumentViews)
	        {
	            var point = docview.ViewModel.DocumentController.GetPosition();
	            var newPoint = point.Value;
	            newPoint.X += (newSize.Width - oldSize.Width) / 2;
	            newPoint.Y += (newSize.Height - oldSize.Height) / 2;
	            docview.ViewModel.DocumentController.SetPosition(newPoint);
	        }

            // set the workspace's size to the new size
            xWorkspace.Width = newSize.Width;
	        xWorkspace.Height = newSize.Height;
	        xWorkspace.Clip = new RectangleGeometry {Rect = xWorkspace.GetBoundingRect(xWorkspace)};

            // try to set the width and height of the active layout to the new width and height
	        var layout = LayoutDocument.GetField<DocumentController>(KeyStore.DataKey)
	            ?.GetField<DocumentController>(KeyStore.ActiveLayoutKey);
	        layout?.SetField(KeyStore.WidthFieldKey, new NumberController(newSize.Width), true);
	        layout?.SetField(KeyStore.HeightFieldKey, new NumberController(newSize.Height), true);

	        var bounds = new Rect(new Point(), newSize);
	        if (!(bounds.Contains(topLeft) && bounds.Contains(bottomRight)))
	        {
	            Bounds.Width = 70;
	            Bounds.Height = 70;
	            return;
	        }
	        else
	        {
	            Bounds.Width = 2 * Math.Max(Math.Abs(bottomRight.X - newSize.Width / 2), Math.Abs(topLeft.X - newSize.Width / 2));
	            Bounds.Height = 2 * Math.Max(Math.Abs(bottomRight.Y - newSize.Height / 2), Math.Abs(topLeft.Y - newSize.Height / 2));
            }
            
	        PositionEllipses();
	    }

	    public void PositionEllipses()
	    {
	        var width = xWorkspace.ActualWidth;
	        var height = xWorkspace.ActualHeight;
            // determine if the width is greater than some arbitrary constant
	        if (width > 420)
	        {
                // if so, move the ellipses to inside of the workspace
	            RelativePanel.SetAlignTopWithPanel(xEllipsePanel, true);
	            RelativePanel.SetAlignLeftWithPanel(xEllipsePanel, true);
	            var offsetY = (500 - height) / 2 + 4;
	            var offsetX = (500 - width) / 2 + width - xEllipseStack.ActualWidth - 12;
	            xEllipsePanel.Padding = new Thickness(offsetX, offsetY, 0, 0);

	        }
	        else
	        {
	            RelativePanel.SetAlignTopWithPanel(xEllipsePanel, false);
	            RelativePanel.SetAlignLeftWithPanel(xEllipsePanel, false);
	            xEllipsePanel.Padding = new Thickness(0, 0, 0, 0);
	        }
        }

        private void XBackgroundOpacitySlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
		{
			//update opacity of background
			xWorkspace.Background.Opacity = e.NewValue / 255;
			xBackgroundColorPreviewBox.Opacity = e.NewValue / 255;
		    DataDocument?.SetField(KeyStore.OpacitySliderValueKey, new NumberController(e.NewValue), true);
		}
   

		/*
		private void CustomizeDesignGrid(int selectedSizeIndex)
		{
			var numDiv = 0;
			switch (selectedSizeIndex)
			{
				case 0:
					numDiv = 3;
					break;
				case 1:
					numDiv = 9;
					break;
				case 2:
					numDiv = 16;
					break;

			}
			//add columns
			for (var i = 0; i < numDiv; i++)
			{
					ColumnDefinition col = new ColumnDefinition();
					xDesignGrid.ColumnDefinitions.Add(col);
					col.Width = new GridLength(1, GridUnitType.Star);

					RowDefinition row = new RowDefinition();
					xDesignGrid.RowDefinitions.Add(row);
					row.Height = new GridLength(1, GridUnitType.Star);

					//format border
					var border = new Windows.UI.Xaml.Controls.Border();
					border.BorderBrush = new SolidColorBrush(Colors.Gray);
					border.BorderThickness = new Thickness(3);

					Grid.SetColumn(border, i);
					Grid.SetRow(border, i);
				
			}
		}

*/

		//changes size of grid when user selects size from drop down
		private void XDesignGridSizeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var combo = sender as ComboBox;
			xDesignGridVisibilityButton.IsChecked = true;
			if (combo != null)
			{
				//if they select "SMALL"
				if (xDesignGridSizeComboBox.SelectedIndex == 0)
				{
					xDesignGridSmall.Visibility = Visibility.Visible;
					xDesignGridLarge.Visibility = Visibility.Collapsed;
				}
				//if they select "LARGE"
				else 
				{
					xDesignGridLarge.Visibility = Visibility.Visible;
					xDesignGridSmall.Visibility = Visibility.Collapsed;
				}
			}
		}

		//makes chosen grid visible
		private void XDesignGridVisibilityButton_OnChecked(object sender, RoutedEventArgs e)
		{
			//if small grid is chosen
			if (xDesignGridSizeComboBox.SelectedIndex == 0)
			{
				xDesignGridSmall.Visibility = Visibility.Visible;
			}
			//if large grid is chosen
			else
			{
				xDesignGridLarge.Visibility = Visibility.Visible;
			}

			xDesignGridSizeComboBox.Background = new SolidColorBrush(Colors.LightGray);
		}

		//collapses both design grids
		private void XDesignGridVisibilityButton_OnUnchecked(object sender, RoutedEventArgs e)
		{
			xDesignGridSmall.Visibility = Visibility.Collapsed;
			xDesignGridLarge.Visibility = Visibility.Collapsed;

			xDesignGridSizeComboBox.Background = new SolidColorBrush(Colors.Gray);
		}


		#region Template Style Handlers

		private void XFreeFormButton_OnChecked(object sender, RoutedEventArgs e)
		{
            //set TemplateStyle key to FreeForm
            //DataDocument?.SetField(KeyStore.TemplateStyleKey, new NumberController(TemplateConstants.FreeformView), true);

		    xGridLeftDragger.Visibility = Visibility.Collapsed;
		    xGridTopDragger.Visibility = Visibility.Collapsed;
			//xItemsControl.ItemsPanel = ItemsPanelTemplateType(typeof(Canvas));
			//update
			xItemsControlCanvas.ItemsSource = DocumentViewModels;

			this.FormatTemplateIntoFreeform();
		}

		private void FormatTemplateIntoFreeform()
		{
			xItemsControlCanvas.Visibility = Visibility.Visible;
			xItemsControlList.Visibility = Visibility.Collapsed;

			//button ui
			xListButton.Background = new SolidColorBrush(Colors.Transparent);
			xFreeFormButton.Background = new SolidColorBrush(Colors.White);

			//update key
			DataDocument.SetField(KeyStore.TemplateStyleKey, new NumberController(TemplateConstants.FreeformView), true);
		}

		private void XListButton_OnChecked(object sender, RoutedEventArgs e)
		{
			this.FormatTemplateIntoList();
            
		}

		private void FormatTemplateIntoList()
		{
			xGridLeftDragger.Visibility = Visibility.Collapsed;
			xGridTopDragger.Visibility = Visibility.Collapsed;
			
			xItemsControlCanvas.Visibility = Visibility.Collapsed;
			xItemsControlList.Visibility = Visibility.Visible;

			//update key
			DataDocument.SetField(KeyStore.TemplateStyleKey, new NumberController(TemplateConstants.ListView), true);
			
			xItemsControlList.ItemsSource = ViewCopiesList;
			
			//button ui
			xFreeFormButton.Background = new SolidColorBrush(Colors.Transparent);
			xListButton.Background = new SolidColorBrush(Colors.White);

		}

		//TODO:MAY HAVE TO CHANGE TO BE USER CONTROL INSTEAD OF GRID TO ACCOUNT FOR USER INTERACTION
		private void XGridButton_OnChecked(object sender, RoutedEventArgs e)
		{
			//set TemplateStyle key to Grid
			//DataDocument?.SetField(KeyStore.TemplateStyleKey, new NumberController(TemplateConstants.GridView), true);

            // make visible the dragging starters on the left and top of the outer workspace
		    xGridLeftDragger.Visibility = Visibility.Visible;
		    xGridTopDragger.Visibility = Visibility.Visible;

		    xItemsControlCanvas.Visibility = Visibility.Collapsed;
		    xItemsControlList.Visibility = Visibility.Collapsed;
		    xItemsControlGrid.Visibility = Visibility.Visible;

		    xItemsControlGrid.ItemsSource = DocumentViewModels;

			//update key
		    DataDocument.SetField<NumberController>(KeyStore.TemplateStyleKey,
		        new NumberController(TemplateConstants.GridView), true);
		}

		ItemsPanelTemplate ItemsPanelTemplateType(Type panelType)
		{
			var itemsPanelTemplateXaml =
				$@"<ItemsPanelTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                                  xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                   <{panelType.Name}/>
               </ItemsPanelTemplate>";

			return (ItemsPanelTemplate)XamlReader.Load(itemsPanelTemplateXaml);
		}


		#endregion


		private void CloseButton_OnClick(object sender, RoutedEventArgs e)
	    {
	        Clear();
	        this.LayoutDocument.SetHidden(true);
        }

	    private void MinusButton_OnClick(object sender, RoutedEventArgs e)
	    {
	        this.LayoutDocument.SetHidden(true);
	    }

		//adds a freeform collection to the template on click
		private void AddFreeform_OnClick(object sender, RoutedEventArgs e)
		{
			var freeform = new CollectionNote(new Point(0, 0), CollectionView.CollectionViewType.Freeform, 100, 100).Document;
			DocumentControllers.Add(freeform);
		}

		//adds a grid collection to the template on click
		private void AddGrid_OnClick(object sender, RoutedEventArgs e)
		{
			DocumentControllers.Add(new CollectionNote(new Point(0, 0), CollectionView.CollectionViewType.Schema, 100, 100).Document);
		}

		//adds a stackpanel to the template on click
		private void AddList_OnClick(object sender, RoutedEventArgs e)
		{
			// TODO: MAKE A DOCUMENT CONTROLLER FOR A STACK PANEL & FORMAT (OR MAKE USER CONTROL??)
			/*
			 var list = new StackPanel();
			 
			 list.PointerEntered
			 DocumentControllers.Add(new DocumentController(new ListModel() as DocumentModel));
			*/
			DocumentControllers.Add(new CollectionNote(new Point(0, 0), CollectionView.CollectionViewType.Grid, 100, 100).Document);
			//IList<DocumentController> layoutList = new ObservableCollection<DocumentController>();
			//DocumentControllers.Add(new ListViewLayout(layoutList, new Point(0,0), new Size(100,200)).Document);
		}

	    private void XGridTopDragger_OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
	    {
            // on started, make visible the fake guideline and let it move with the mouse
	        xHorizLine.Visibility = Visibility.Visible;
	        xHorizLine.Y1 = e.Position.Y;
	        xHorizLine.Y2 = e.Position.Y;
	        e.Handled = true;
	    }

	    private void XGridTopDragger_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
	    {
            // while dragging, keep the line on the same y-position as the mouse
	        var top = xHorizLine.Y1;
	        top += Util.DeltaTransformFromVisual(e.Delta.Translation, xOuterWorkspace).Y;
	        xHorizLine.Y1 = top;
	        xHorizLine.Y2 = top;
	        e.Handled = true;
	    }

	    private Line NewLine(double x1 = 0, double x2 = 0, double y1 = 0, double y2 = 0)
        {
            // creates a new line, useful for creating guidelines
	        var line = new Line
	        {
	            X1 = x1,
	            X2 = x2,
	            Y1 = y1,
	            Y2 = y2,
	            Stroke = new SolidColorBrush(Colors.Aqua),
	            StrokeThickness = 1
	        };
            line.PointerEntered += delegate
            {
                Window.Current.CoreWindow.PointerCursor =
                    new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Hand, 0);
            };
            line.Tapped += Line_Tapped;
            line.PointerExited += delegate
	        {
	            Window.Current.CoreWindow.PointerCursor =
	                new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 0);
	        };
            Canvas.SetZIndex(line, 100);
	        return line;
	    }

        private void Line_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var line = sender as Line;

            // case for vertical lines
            if (line.X1 == line.X2)
            {
                // we subtract ten from the "fixed" point to force it to retrieve the column left of the line
                var leftCol = FindColumn(line.X1 - 10 - (xOuterWorkspace.Width - xWorkspace.Width) / 2);
                var rightCol = leftCol + 1;
                // determine if the right column is a valid index
                if (rightCol <= GridRoot.ColumnDefinitions.Count - 1)
                {
                    // if so, make the left column's width encapsulate the right column's width
                    GridRoot.ColumnDefinitions[leftCol].Width =
                        new GridLength(GridRoot.ColumnDefinitions[leftCol].ActualWidth +
                                       GridRoot.ColumnDefinitions[rightCol].ActualWidth);
                    // fix the grid columns of everything that was in the right column and move it to the left
                    foreach (var docView in DocumentViews)
                    {
                        if (Grid.GetColumn(docView.GetFirstAncestorOfType<ContentPresenter>()) == rightCol)
                        {
                            Grid.SetColumn(docView.GetFirstAncestorOfType<ContentPresenter>(), leftCol);
                        }
                    }
                    // remove the right column
                    GridRoot.ColumnDefinitions.RemoveAt(rightCol);
                }
                // remove the line
                xOuterWorkspace.Children.Remove(line);
            }
            // case for horizontal lines
            else if (line.Y1 == line.Y2)
            {
                var topRow = FindRow(line.Y1 - 10 - (xOuterWorkspace.Height - xWorkspace.Height) / 2);
                var bottomRow = topRow + 1;
                if (bottomRow <= GridRoot.RowDefinitions.Count - 1)
                {
                    GridRoot.RowDefinitions[topRow].Height =
                        new GridLength(GridRoot.RowDefinitions[topRow].ActualHeight +
                                       GridRoot.RowDefinitions[bottomRow].ActualHeight);
                    foreach (var docView in DocumentViews)
                    {
                        if (Grid.GetRow(docView.GetFirstAncestorOfType<ContentPresenter>()) == bottomRow)
                        {
                            Grid.SetRow(docView.GetFirstAncestorOfType<ContentPresenter>(), topRow);
                        }
                    }
                    GridRoot.RowDefinitions.RemoveAt(bottomRow);
                }
                xOuterWorkspace.Children.Remove(line);
            }
            ApplyChanges_OnClicked(null, new RoutedEventArgs());
        }

        private void XGridTopDragger_OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
	    {
            // determine if the line exists inside of the workspace
	        if (0 < xHorizLine.Y1 - (xOuterWorkspace.Height - xWorkspace.Height) / 2 &&
	            xHorizLine.Y1 - (xOuterWorkspace.Height - xWorkspace.Height) / 2 < xWorkspace.Height)
	        {
                // create a copy of the line and add it to the outer workspace
	            var line = NewLine(0, 500, xHorizLine.Y1, xHorizLine.Y2);
                xOuterWorkspace.Children.Add(line);

	            // let the height start at the y position minus the offset created by the outer workspace
	            double height = line.Y1 - (xOuterWorkspace.Height - xWorkspace.Height) / 2;
                // find which row we should be inserting at
                var row = FindRow(height);
                // if we aren't in the first row
	            if (row > 0)
	            {
                    // find the height of all the rows leading before it
	                double calculatedHeight = 0;
	                for (var i = 0; i < row; i++)
	                {
	                    calculatedHeight += GridRoot.RowDefinitions[i].ActualHeight;
	                }

                    // subtract that sum from our current height
	                height -= calculatedHeight;
	            }
                // insert a new row at that spot with our calculated height
	            GridRoot.RowDefinitions.Insert(row, new RowDefinition
	            {
	                Height = new GridLength(height)
	            });
                // by adding a new row, we have cut a row in half, so the other half needs to have its height reset
	            GridRoot.RowDefinitions[row + 1].Height =
	                new GridLength(GridRoot.RowDefinitions[row + 1].ActualHeight - height);

	            //foreach (var doc in DocumentViews)
	            //{
	            //    if (doc.ViewModel.DocumentController.GetField<NumberController>(KeyStore.RowKey)?.Data == row || doc
	            //            .ViewModel.DocumentController.GetField<NumberController>(KeyStore.RowKey)?.Data == row + 1)
	            //    {
	            //        DocumentView_OnLoaded_GridView(doc, null);
	            //    }
	            //}
	        }

            // reset the line that we use as a visual cue of "adding" a new line
	        xHorizLine.Visibility = Visibility.Collapsed;
	        xHorizLine.Y1 = 0;
	        xHorizLine.Y2 = 0;
	        e.Handled = true;
	    }

        private void XGridLeftDragger_OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
	    {
	        xVertLine.Visibility = Visibility.Visible;
	        xVertLine.X1 = e.Position.X;
	        xVertLine.X2 = e.Position.X;
	        e.Handled = true;
        }

	    private void XGridLeftDragger_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
	    {
	        var left = xVertLine.X1;
	        left += Util.DeltaTransformFromVisual(e.Delta.Translation, xOuterWorkspace).X;
	        xVertLine.X1 = left;
	        xVertLine.X2 = left;
	        e.Handled = true;
	    }

	    private void XGridLeftDragger_OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
	    {
            // determine if the new line exists inside of the workspace
	        if (0 < xVertLine.X1 - (xOuterWorkspace.Width - xWorkspace.Width) / 2 &&
	            xVertLine.X1 - (xOuterWorkspace.Width - xWorkspace.Width) / 2 < xWorkspace.Width)
	        {
                // create a copy of the line and add it to the outer workspace
                var line = NewLine(xVertLine.X1, xVertLine.X2, 0, 500);
	            xOuterWorkspace.Children.Add(line);

                // determine the width from the left of the workspace to the line
	            double width = line.X1 - (xOuterWorkspace.Width - xWorkspace.Width) / 2;
                // find the index of the new column given the x offset
	            var col = FindColumn(width);
                // if it isn't the first index
	            if (col > 0)
	            {
                    // add up the widths of all the columns preceding
	                double calculatedWidth = 0;
	                for (var i = 0; i < col; i++)
	                {
	                    calculatedWidth += GridRoot.ColumnDefinitions[i].ActualWidth;
	                }
                    // subtract the sum from our current width
	                width -= calculatedWidth;
	            }
                // create a new column at that specific index with our new width
                GridRoot.ColumnDefinitions.Insert(col, new ColumnDefinition
	            {
	                Width = new GridLength(width)
	            });
                // by creating a new column, we have to cut a previously existing one, so we need to reset the width
	            GridRoot.ColumnDefinitions[col + 1].Width =
	                new GridLength(GridRoot.ColumnDefinitions[col + 1].ActualWidth - width);
	        }

            // reset the fake guiding line
            xVertLine.Visibility = Visibility.Collapsed;
	        xVertLine.X1 = 0;
	        xVertLine.X2 = 0;
	        e.Handled = true;
	    }

        /// <summary>
        ///     given a y-offset relative to the top of xWorkspace, finds the appropriate row
        ///     that that offset should be in
        /// </summary>
        /// <param name="offsetY">
        ///     double variable representing y-offset relative to the top edge of xWorkspace
        /// </param>
        /// <returns></returns>
	    private int FindRow(double offsetY)
	    {
	        double currOffset = 0;
            // loop through every row definition
	        for (var i = 0; i < GridRoot.RowDefinitions.Count; i++)
	        {
                // if the y-offset lands between these two numbers, the y-offset is in row i
	            if (currOffset < offsetY && offsetY <
	                currOffset + GridRoot.RowDefinitions[i].ActualHeight)
	            {
	                return i;
	            }

                // save the position of this row and go to the next row
	            currOffset += GridRoot.RowDefinitions[i].ActualHeight;
	        }

            // if we haven't returned anything, but the offset is between our end offset and the height
	        if (currOffset < offsetY && offsetY < xWorkspace.Height)
	        {
                // the y-offset is in the last row
	            return GridRoot.RowDefinitions.Count;
	        }

	        return 0;
	    }

	    private int FindColumn(double offsetX)
	    {
	        double currOffset = 0;
            // loop through every column definition
	        for (var i = 0; i < GridRoot.ColumnDefinitions.Count; i++)
	        {
                // if the x-offset lands between these two numbers, the x-offset is in column i
	            if (currOffset < offsetX && offsetX <
	                currOffset + GridRoot.ColumnDefinitions[i].ActualWidth)
	            {
	                return i;
	            }

                // save the position of this column and go to the next
	            currOffset += GridRoot.ColumnDefinitions[i].ActualWidth;
	        }

	        if (currOffset < offsetX && offsetX < xWorkspace.Width)
	        {
	            return GridRoot.ColumnDefinitions.Count;
	        }

	        return 0;
	    }

        private void TappedHandler(object sender, TappedRoutedEventArgs e)
	    {
            // necessary
	        e.Handled = true;
	    }

	    private void DocumentView_OnLoaded_GridView(object sender, RoutedEventArgs e)
	    {
	        // set up variables
	        var docView = sender as DocumentView;
	        if (!DocumentViews.Contains(docView))
	        {
	            DocumentViews.Add(docView);
	            // add event handlers for the document
	            docView.DocumentDeleted += DocView_DocumentDeleted;
	            docView.SizeChanged += DocumentView_OnSizeChanged;
	            docView.ViewModel.LayoutDocument.AddFieldUpdatedListener(KeyStore.PositionFieldKey, PositionFieldChanged);
            }
	        // since we are in a grid, we want to use the horizontal and vertical alignment keys
	        docView.ViewModel.DocumentController.SetField(KeyStore.UseHorizontalAlignmentKey, new BoolController(true),
	            true);
	        docView.ViewModel.DocumentController.SetField(KeyStore.UseVerticalAlignmentKey, new BoolController(true),
	            true);
            // stop the document from being able to be manipulated
	        docView.PreventManipulation = true;
	        // we also want to make sure the horizontal and vertical alignment are both stretched
	        docView.ViewModel.DocumentController.SetField(KeyStore.HorizontalAlignmentKey,
	            new TextController(HorizontalAlignment.Stretch.ToString()), true);
	        docView.ViewModel.DocumentController.SetField(KeyStore.VerticalAlignmentKey,
	            new TextController(VerticalAlignment.Stretch.ToString()), true);
	        docView.HorizontalAlignment = HorizontalAlignment.Stretch;
	        docView.VerticalAlignment = VerticalAlignment.Stretch;

	        // let the column/row index be the column/row key if it exists, otherwise use the
	        // viewmodel's x/y position to find the correct column/row
	        var col = docView.ViewModel.DocumentController.GetField<NumberController>(KeyStore.ColumnKey)?.Data ??
	                  FindColumn(docView.ViewModel.XPos);
	        var row = docView.ViewModel.DocumentController.GetField<NumberController>(KeyStore.RowKey)?.Data ??
	                  FindRow(docView.ViewModel.YPos);
	        // set the content presenter's grid row and column to that index
	        Grid.SetColumn(docView.GetFirstAncestorOfType<ContentPresenter>(), (int) col);
	        Grid.SetRow(docView.GetFirstAncestorOfType<ContentPresenter>(), (int) row);

	        // retrieve the template box layout information if possible
	        var layout = LayoutDocument.GetField<DocumentController>(KeyStore.DataKey)
	            .GetField<DocumentController>(KeyStore.ActiveLayoutKey);
	        // determine if it exists and a row information key exists with a different number of rows than currently
	        if (layout?.GetField(KeyStore.RowInfoKey) is ListController<NumberController> rowInfo &&
	            rowInfo.Data.Count != GridRoot.RowDefinitions.Count)
	        {
                // if so, clear the rows
	            GridRoot.RowDefinitions.Clear();
                // start counting the height at the offset determined by the top of xOuterWorkspace
	            var countingHeight = (xOuterWorkspace.Height - xWorkspace.Height) / 2;
	            foreach (NumberController heightInfo in rowInfo.Data)
	            {
	                GridRoot.RowDefinitions.Add(
	                    new RowDefinition
	                    {
	                        Height = new GridLength(heightInfo.Data)
	                    });
	                countingHeight += heightInfo.Data;
	                if (countingHeight < xOuterWorkspace.Height - (xOuterWorkspace.Height - xWorkspace.Height) / 2)
	                {
	                    var line = NewLine(0, 500, countingHeight, countingHeight);
	                    xOuterWorkspace.Children.Add(line);
	                }
	            }
	        }

	        if (layout?.GetField(KeyStore.ColumnInfoKey) is ListController<NumberController> colInfo &&
	            colInfo.Data.Count != GridRoot.ColumnDefinitions.Count)
	        {
	            GridRoot.ColumnDefinitions.Clear();

	            double countingWidth = (xOuterWorkspace.Width - xWorkspace.Width) / 2;
	            foreach (NumberController widthInfo in colInfo.Data)
	            {
	                GridRoot.ColumnDefinitions.Add(
	                    new ColumnDefinition
	                    {
	                        Width = new GridLength(widthInfo.Data)
	                    });
	                countingWidth += widthInfo.Data;
	                if (countingWidth < xOuterWorkspace.Width - (xOuterWorkspace.Width - xWorkspace.Width) / 2)
	                {
	                    var line = NewLine(countingWidth, countingWidth, 0, 500);
	                    xOuterWorkspace.Children.Add(line);
	                }
	            }
	        }

	        double width;
	        if (GridRoot.ColumnDefinitions[(int) col]?.Width.IsStar ?? false)
	        {
	            double calculatedWidth = 0;
	            for (var i = 0; i < col; i++)
	            {
	                calculatedWidth += GridRoot.ColumnDefinitions[i].Width.Value;
	            }

	            for (var j = (int) col + 1; j < GridRoot.ColumnDefinitions.Count; j++)
	            {
	                calculatedWidth += GridRoot.ColumnDefinitions[j].Width.Value;
	            }

	            width = xWorkspace.Width - calculatedWidth;
	        }
	        else
	        {
	            width = GridRoot.ColumnDefinitions[(int) col]?.Width.Value ?? xWorkspace.Width;
	        }

	        double height;
	        if (GridRoot.RowDefinitions[(int) row]?.Height.IsStar ?? false)
	        {
	            double calculatedHeight = 0;
	            for (var i = 1; i < GridRoot.RowDefinitions.Count; i++)
	            {
	                calculatedHeight += GridRoot.RowDefinitions[i].Height.Value;
	            }

	            height = xWorkspace.Height - calculatedHeight;
	        }
	        else
	        {
	            height = GridRoot.RowDefinitions[(int) row]?.Height.Value ?? xWorkspace.Height;
	        }

	        docView.ViewModel.DocumentController.SetWidth(width);
	        docView.ViewModel.DocumentController.SetHeight(height);
	    }

	    private void ScrollViewer_OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
	    {
	        XScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
	    }
		private void XItemsControlList_OnDragItemsStartingList_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
		{
			Debug.WriteLine("DRAG STARTED");
		}

		private void XItemsControlList_OnDragItemsCompletedList_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
		{
			Debug.WriteLine("DRAG COMPLETED");
		}

	  

	    private void ToggleControl_OnChecked(object sender, RoutedEventArgs e)
	    {
	        var toggle = sender as ToggleButton;
	        switch (toggle.Name)
	        {
                case "xActivate":
                    if ((bool) xPreview.IsChecked)
                    {
                        xPreview.IsChecked = false;
                    }

                    xActivate.IsChecked = true;
                    DataDocument.SetField<BoolController>(KeyStore.ActivationKey, true, true);
                    ApplyChanges_OnClicked(toggle, new RoutedEventArgs());
                    break;
	            case "xPreview":
	                if ((bool) xActivate.IsChecked)
	                {
	                    xActivate.IsChecked = false;
	                }

	                xPreview.IsChecked = true;
	                DataDocument.SetField<BoolController>(KeyStore.ActivationKey, false, true);
                    break;
	        }
        }

		//IF WE WANT TO CLOSE THE FORMAT ITEMS DROP DOWN WHEN THE ITEM LOSES FOCUS
		/*
		private void XWorkspace_OnPointerPressed_(object sender, PointerRoutedEventArgs e)
		{
			if (xFormatItemsButtonStack.Visibility == Visibility.Visible) ExpandButtonOnClick(xFormatItemsHeader, new RoutedEventArgs());
		}
		*/
	}

}