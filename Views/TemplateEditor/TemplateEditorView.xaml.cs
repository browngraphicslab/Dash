
using Dash.Controllers;
using Dash.Converters;
using Dash.FontIcons;
using Dash.Models.DragModels;
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
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Shapes;
using Point = Windows.Foundation.Point;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class TemplateEditorView : UserControl
	{
		public DocumentController LayoutDocument { get; set; }
		public DocumentController DataDocument { get; set; }

		//initializing the list of layout documents contained within the template
		public ObservableCollection<DocumentController> DocumentControllers { get; set; }

		//item source for xWorkspace
		public ObservableCollection<DocumentViewModel> DocumentViewModels { get; set; }

		public Collection<DocumentView> DocumentViews { get; set; }

        private KeyValueTemplatePane _keyValuePane;
		private DocumentView _selectedDocument;
		private Point _pasteWhereHack;
		private double _thickness;
		private bool _isDataDocKVP = true;
		private Windows.UI.Color _color;
		DataPackage dataPackage = new DataPackage();


		public SolidColorBrush _backgroundColor = new SolidColorBrush(Colors.White);

		public TemplateEditorView()
		{
			this.InitializeComponent();

			xOuterPanel.BorderThickness = new Thickness(2, 8, 2, 2);

			DocumentControllers = new ObservableCollection<DocumentController>();
			DocumentViewModels = new ObservableCollection<DocumentViewModel>();
			DocumentViews = new Collection<DocumentView>();
		}

		private void TemplateEditorView_DocumentDeleted(DocumentView sender,
			DocumentView.DocumentViewDeletedEventArgs args)
		{
			if (LayoutDocument.GetField<DocumentController>(KeyStore.DataKey).GetDataDocument()
					.GetField(KeyStore.TemplateDocumentKey) != null)
				LayoutDocument.GetField<DocumentController>(KeyStore.DataKey).GetDataDocument()
					.RemoveField(KeyStore.TemplateDocumentKey);
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
					break;
			}
		}

		private void RemoveDocs(IEnumerable<DocumentController> oldDocs)
		{
			foreach (var doc in oldDocs)
			{
				DocumentViewModels.Remove(DocumentViewModels.First(i => i.DocumentController.Equals(doc)));
			}

			DataDocument.SetField(KeyStore.DataKey, new ListController<DocumentController>(DocumentControllers),
				true);
			xItemsControl.ItemsSource = DocumentViewModels;
		}

		private void AddDocs(IEnumerable<DocumentController> newDocs)
		{
			foreach (var doc in newDocs)
			{
				AddDoc(doc);
			}
		}

		private void AddDoc(DocumentController doc)
		{
			var workingDoc = LayoutDocument.GetField<DocumentController>(KeyStore.DataKey);
			// if either is true, then the layout doc needs to be abstracted
			if (doc.GetDataDocument().Equals(workingDoc.GetDataDocument()) || doc.GetDataDocument().Equals(workingDoc))
			{
				if (_isDataDocKVP)
				{
					// set the layout doc's context to a reference of the data doc's context
					doc.SetField(KeyStore.DocumentContextKey,
						new DocumentReferenceController(
							DataDocument.GetField<DocumentController>(KeyStore.DocumentContextKey).Id,
							KeyStore.DocumentContextKey),
						true);
				}
				else
				{
					// set the layout doc's context to a reference of the data doc's context
					doc.SetField(KeyStore.DocumentContextKey,
						new DocumentReferenceController(DataDocument.Id,
							KeyStore.DocumentContextKey),
						true);
				}

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
            // adds layout doc to list of layout docs
		    var datakey = DataDocument.GetField<ListController<DocumentController>>(KeyStore.DataKey);
		    datakey.Add(dvm.LayoutDocument);
		    DataDocument.SetField(KeyStore.DataKey, datakey, true);

            xItemsControl.ItemsSource = DocumentViewModels;
		}

		private void XSwitchButton_Tapped(object sender, TappedRoutedEventArgs e)
		{
			_isDataDocKVP = !_isDataDocKVP;
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
				new ObservableCollection<DocumentController>(DataDocument
					.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null).TypedData);

		}

		private void XWorkspace_OnLoaded(object sender, RoutedEventArgs e)
		{
			//initialize UI of workspace
			this.FormatPanes();
			var rect = new Rect(0, 0, 300, 400);
			var rectGeo = new RectangleGeometry { Rect = rect };
			xWorkspace.Clip = rectGeo;

			//hide resize and ellipse controls for template editor
			this.GetFirstAncestorOfType<DocumentView>().ViewModel.DisableDecorations = true;
			this.GetFirstAncestorOfType<DocumentView>().hideControls();
			DocumentViewModels.Clear();
			//initialize layout documents on workspace
			foreach (var layoutDoc in DataDocument
				.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null).TypedData)
			{
				DocumentViewModels.Add(new DocumentViewModel(layoutDoc));
			}

			//update item source
			xItemsControl.ItemsSource = DocumentViewModels;

			xOuterPanel.BorderThickness = new Thickness(0, 2, 0, 0);
		    Bounds = new Rect(0, 0, 70, 70);
		    var resizer = new ResizingControls(this);

            xOuterWorkspace.Children.Add(resizer);
		    RelativePanel.SetAlignHorizontalCenterWithPanel(resizer, true);
		    RelativePanel.SetAlignVerticalCenterWithPanel(resizer, true);
            DataDocument.SetField(KeyStore.DocumentContextKey,
				LayoutDocument.GetField<DocumentController>(KeyStore.DataKey), true);
			//listen for any changes to the collection
			DocumentControllers.CollectionChanged += DocumentControllers_CollectionChanged;
			xKeyBox.PropertyChanged += XKeyBox_PropertyChanged;
			this.GetFirstAncestorOfType<DocumentView>().DocumentDeleted += TemplateEditorView_DocumentDeleted;

            //set background color
            var colorString = DataDocument.GetField<TextController>(KeyStore.BackgroundColorKey, true)?.Data ?? "#FFFFFF";
			var backgroundColor = new StringToBrushConverter().ConvertDataToXaml(colorString);
			xWorkspace.Background = backgroundColor;
			xBackgroundColorPreviewBox.Fill = xWorkspace.Background;
		}
        
	    private void XWorkspace_OnUnloaded(object sender, RoutedEventArgs e)
		{
			DocumentControllers.CollectionChanged -= DocumentControllers_CollectionChanged;
		}

		// when the "Add Text" button is clicked, this adds a text box to the template preview
		private void TextButton_OnClick(object sender, RoutedEventArgs e)
		{
            DocumentControllers.Add(new RichTextNote("New text box").Document);
		}

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
						if (docController.GetWidthField().Data >= xWorkspace.Width)
							docController.SetWidth(xWorkspace.Width - 20);
						// TODO: Check for if height is too large (may be difficult bcs height = nan? -sy
						DocumentControllers.Add(docController.GetViewCopy(new Point(0, 0)));
					}
				}
			}
		}

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

			//TODO just add new images to docs list instead of going through mainPageCollectionView
			//var docs = MainPage.Instance.MainDocument.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);
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

			//TODO just add new images to docs list instead of going through mainPageCollectionView
			//var docs = MainPage.Instance.MainDocument.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);
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

		private void AlignmentButton_OnChecked(object sender, RoutedEventArgs e)
		{
			var button = sender as AppBarButton;

			//for each document, align according to what button was pressed
			foreach (var dvm in DocumentViewModels)
			{
				var point = dvm.LayoutDocument.GetField<PointController>(KeyStore.PositionFieldKey);
				switch (button.Name)
				{
					case "xAlignLeft":
						point = new PointController(0, point.Data.Y);
					    dvm.LayoutDocument.SetField(KeyStore.HorizontalAlignmentKey,
					        new TextController(HorizontalAlignment.Left.ToString()), true);
					    dvm.LayoutDocument.SetField(KeyStore.UseHorizontalAlignmentKey, new BoolController(true), true);
                        break;

					case "xAlignCenter":
						var centerX = (xWorkspace.Width - dvm.LayoutDocument.GetActualSize().Value.X) / 2;
                        point = new PointController(centerX, point.Data.Y);
					    dvm.LayoutDocument.SetField(KeyStore.HorizontalAlignmentKey,
					        new TextController(HorizontalAlignment.Center.ToString()), true);
					    dvm.LayoutDocument.SetField(KeyStore.UseHorizontalAlignmentKey, new BoolController(true), true);
                        break;

					case "xAlignRight":
						var rightX = xWorkspace.Width - dvm.LayoutDocument.GetActualSize().Value.X;
						point = new PointController(rightX, point.Data.Y);
					    dvm.LayoutDocument.SetField(KeyStore.HorizontalAlignmentKey,
					        new TextController(HorizontalAlignment.Right.ToString()), true);
					    dvm.LayoutDocument.SetField(KeyStore.UseHorizontalAlignmentKey, new BoolController(true), true);
                        break;
				}

				dvm.LayoutDocument.SetField(KeyStore.PositionFieldKey, point, true);
			}
		}

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

		// called when apply changes button is clicked
		private void ApplyChanges_OnClicked(object sender, RoutedEventArgs e)
		{
			// layout document's data key holds the document that we are currently working on
			var workingDoc = LayoutDocument.GetField<DocumentController>(KeyStore.DataKey);
			// TODO: working doc should be able to be null
			// make a copy of the data document
			var dataDocCopy = DataDocument.MakeCopy();
			// loop through each layout document and try to abstract it out when necessary

			// set the dataDocCopy's document context key to the working document's data document
			dataDocCopy.SetField(KeyStore.DocumentContextKey, workingDoc, true);
            // set the position of the data copy to the working document's position
            dataDocCopy.SetField(KeyStore.PositionFieldKey,
                workingDoc.GetField<PointController>(KeyStore.PositionFieldKey), true);

            // set width and height of the new document
		    dataDocCopy.SetField(KeyStore.WidthFieldKey, new NumberController(xWorkspace.Width), true);
		    dataDocCopy.SetField(KeyStore.HeightFieldKey, new NumberController(xWorkspace.Height), true);
			// set the active layout of the working document to the dataDocCopy (which is the template)
			workingDoc.SetField(KeyStore.ActiveLayoutKey, dataDocCopy, true);
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

		    var currPos = docView.ViewModel.DocumentController
		        .GetField<PointController>(KeyStore.PositionFieldKey).Data;
		    if (docView.ActualWidth > xWorkspace.Width)
		    {
		        docView.ViewModel.DocumentController.SetWidth(xWorkspace.Width);
		        docView.ViewModel.DocumentController.SetField(KeyStore.PositionFieldKey, new PointController(0, currPos.Y), true);
		    }
		    else if (currPos.X + docView.ActualWidth > xWorkspace.Width)
		    {
		        docView.ViewModel.DocumentController.SetField(KeyStore.PositionFieldKey,
		            new PointController(xWorkspace.Width - docView.ActualWidth - 1, currPos.Y), true);
		    }

		    currPos = docView.ViewModel.DocumentController.GetField<PointController>(KeyStore.PositionFieldKey).Data;
            if (currPos.Y + docView.ActualHeight > xWorkspace.Height)
		    {
		        docView.ViewModel.DocumentController.SetField(KeyStore.PositionFieldKey,
		            new PointController(currPos.X, xWorkspace.Height - docView.ActualHeight - 1), true);
		    }

            //updates and generates bounds for the children inside the template canvas
            var bounds = new Rect(0, 0, xWorkspace.Width - docView.ActualWidth,
				xWorkspace.Height - docView.ActualHeight);
			docView.Bounds = new RectangleGeometry { Rect = bounds };
			docView.DocumentSelected += DocView_DocumentSelected;
			docView.DocumentDeleted += DocView_DocumentDeleted;
            docView.ViewModel.LayoutDocument.AddFieldUpdatedListener(KeyStore.PositionFieldKey, PositionFieldChanged);
		}

	    private void PositionFieldChanged(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args,
	        Context context)
	    {
	        if (sender.GetField<BoolController>(KeyStore.UseHorizontalAlignmentKey)?.Data ?? false)
	        {
	            switch (sender.GetField<TextController>(KeyStore.HorizontalAlignmentKey)?.Data)
	            {
	                case nameof(HorizontalAlignment.Left):
	                    if (sender.GetField<PointController>(KeyStore.PositionFieldKey).Data.X != 0)
	                    {
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
			xKeyBox.PropertyChanged -= XKeyBox_PropertyChanged;
			_selectedDocument = sender;
			var pRef = _selectedDocument.ViewModel.DocumentController.GetField<ReferenceController>(KeyStore.DataKey);
			var specificKey = pRef.FieldKey;
			string text = "";
			var workingDoc = LayoutDocument.GetField<DocumentController>(KeyStore.DataKey);
			var doc = _selectedDocument.ViewModel.DocumentController;
			if (doc.GetDataDocument().Equals(workingDoc.GetDataDocument()) || doc.GetDataDocument().Equals(workingDoc))
			{
				text = "#";
				text += specificKey;
			}
			else
			{
				text = _selectedDocument.ViewModel.DocumentController.Title;
			}

			xKeyBox.Text = text;
			xKeyBox.PropertyChanged += XKeyBox_PropertyChanged;
		}

		private void XKeyBox_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (!xKeyBox.TextBoxLoaded && _selectedDocument != null)
			{
				var text = xKeyBox.Text;
				if (text.StartsWith("#"))
				{
					var possibleKeyString = text.Substring(1);
					var keyValuePairs = LayoutDocument.GetField<DocumentController>(KeyStore.DataKey).GetDataDocument()
						.EnumFields();
					var specificKey = keyValuePairs.FirstOrDefault(kvp => kvp.Key.ToString().Equals(possibleKeyString))
						.Key;
					var workingDoc = LayoutDocument.GetField<DocumentController>(KeyStore.DataKey);
					if (specificKey != null)
					{
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
								docRef = new DocumentReferenceController(LayoutDocument.Id, KeyStore.DataKey);
								newRef = new PointerReferenceController(docRef, specificKey);
							}
							else if (workingDoc.GetDataDocument().GetField(specificKey) != null)
							{
								docRef = new DocumentReferenceController(
									LayoutDocument.GetField<DocumentController>(KeyStore.DataKey).Id, KeyStore.DataKey);
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
							.SetField<TextController>(KeyController.LookupKeyByName(pair[0], true), pair[1].Trim('\r'),
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
						var dropDoc = dragModel.GetDropDocument(where);
						DocumentControllers.Add(dropDoc.GetViewCopy(where));
						//// kinda hacky lol -sy
						//DocumentViewModels.Last().DocumentController
						//	.SetField(KeyStore.PositionFieldKey, new PointController(where), true);
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
					d.SetField(pivotKey, new DocumentReferenceController(pivotDoc.Id, pivotKey), true);
				}

				pivotDictionary.Add(obj, pivotDoc);
				dictionary.Add(obj, new Dictionary<KeyController, List<object>>());
			}

			if (obj != null)
			{
				d.SetField(pivotKey, new DocumentReferenceController(pivotDictionary[obj].Id, pivotKey), true);
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
				case "xFormatTemplateHeader":
					stack = xFormatTemplateButtonStack;
					arrow = xFormatTemplateArrow;
					animation = xFadeAnimationFormatTemplate;
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

		//sets the thickness for the borders
		private void XThicknessSlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
		{
			if (sender is Slider slider)
			{
				_thickness = slider.Value;
			}
		}

		//sets the colors for the borders
		private void XColorPicker_OnPointerReleased(object sender, PointerRoutedEventArgs e)
		{
			//_color = xColorPicker.SelectedColor;    
		}

		private void XResetButton_OnClick(object sender, RoutedEventArgs e)
		{
			//TODO: reset to original state of template (clear if new, or revert to other if editing)
		}

		private void XUndoButton_OnClick(object sender, RoutedEventArgs e)
		{
			//TODO: implement undo
		}

		private void XRedoButton_OnClick(object sender, RoutedEventArgs e)
		{
			//TODO: implement redo
		}

		private void XClearButton_OnClick(object sender, RoutedEventArgs e)
		{
			//TODO: implement clear
		}

		private void XUploadTemplate_OnClick(object sender, RoutedEventArgs e)
		{
			//TODO: implement
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
           
			var bounds = new Rect(0, 0, xWorkspace.Width - docView.ActualWidth,
				xWorkspace.Height - docView.ActualHeight);
		   
            docView.Bounds = new RectangleGeometry { Rect = bounds };
		   
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

		private void BackgroundButton_OnTapped(object sender, TappedRoutedEventArgs e)
		{
			//xBackgroundColorFlyout.Open;
		}

		private void XBackgroundColorPreviewBox_OnTapped(object sender, TappedRoutedEventArgs e)
		{
			FlyoutBase.ShowAttachedFlyout(xBackgroundColorPreviewBox);
			//xBackgroundOpacitySlider.Width = xBackgroundColorPicker.ActualWidth;
			//xBackgroundOpacitySlider.Foreground = new LinearGradientBrush();


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
	        var oldSize = new Size(xWorkspace.Width, xWorkspace.Height);
	        xWorkspace.Width = newSize.Width;
	        xWorkspace.Height = newSize.Height;
	        var rect = new Rect(0, 0, newSize.Width, newSize.Height);
	        var rectGeo = new RectangleGeometry { Rect = rect };
	        xWorkspace.Clip = rectGeo;
	        double maxOffsetX = 70;
	        double maxOffsetY = 70;
	        foreach (var docview in DocumentViews)
	        {
	            var point = docview.ViewModel.DocumentController.GetPosition();
	            var newPoint = point.Value;
	            newPoint.X += (newSize.Width - oldSize.Width) / 2;
	            newPoint.Y += (newSize.Height - oldSize.Height) / 2;
                docview.ViewModel.DocumentController.SetPosition(newPoint);
	            if (newSize.Width - newPoint.X > maxOffsetX)
	            {
                    maxOffsetX = newSize.Width - newPoint.X;
	            } else if (-(newSize.Width - newPoint.X - docview.ActualWidth) > maxOffsetX)
	            {
	                maxOffsetX = -(newSize.Width - newPoint.X - docview.ActualWidth);
                }

	            if (newSize.Height - newPoint.Y > maxOffsetY)
	            {
	                maxOffsetY = newSize.Height - newPoint.Y;
	            } else if (-(newSize.Height - newPoint.Y - docview.ActualHeight) > maxOffsetY)
	            {
	                maxOffsetY = -(newSize.Height - newPoint.Y - docview.ActualHeight);
                }

                var bounds = new Rect(0, 0, xWorkspace.Width - docview.ActualWidth,
	                xWorkspace.Height - docview.ActualHeight);
	            docview.Bounds = new RectangleGeometry { Rect = bounds };
            }

	        Bounds.Width = maxOffsetX;
	        Bounds.Height = maxOffsetY;
	    }

		private void XBackgroundOpacitySlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
		{
			//update opacity of background
			xWorkspace.Background.Opacity = e.NewValue / 255;
			xBackgroundColorPreviewBox.Opacity = e.NewValue / 255;
		    DataDocument?.SetField(KeyStore.OpacitySliderValueKey, new NumberController(e.NewValue), true);
		}
	}
}