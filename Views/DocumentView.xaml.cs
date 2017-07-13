using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using DashShared;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using DocumentMenu;
using Visibility = Windows.UI.Xaml.Visibility;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using Windows.Storage;
using Windows.Storage.Pickers;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236
namespace Dash
{

    public sealed partial class DocumentView : UserControl
    {
        public string DebugName = "";
        public CollectionView ParentCollection;
        public bool HasCollection { get; set; }
        /// <summary>
        /// Contains methods which allow the document to be moved around a free form canvas
        /// </summary>
        private ManipulationControls manipulator;

        private OverlayMenu _docMenu;
        public DocumentViewModel ViewModel { get; set; }


        public bool ProportionalScaling;
        public ManipulationControls Manipulator { get { return manipulator; } }

        public event OperatorView.IODragEventHandler IODragStarted;
        public event OperatorView.IODragEventHandler IODragEnded;

        public void setBG(SolidColorBrush s) { XGrid.Background = s; }

        public ICollectionView View { get; set; }
        private double startWidth, startHeight; // used for restoring on double click in icon view

        public DocumentView()
        {
            this.InitializeComponent();
            DataContextChanged += DocumentView_DataContextChanged;

            // add manipulation code
            this.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            manipulator = new ManipulationControls(this);
            manipulator.OnManipulatorTranslated += ManipulatorOnOnManipulatorTranslated;

            // set bounds
            MinWidth = 64;
            MinHeight = 64;

            startWidth = Width;
            startHeight = Height;

            //xContextMenu.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            DraggerButton.Holding += DraggerButtonHolding;
            DraggerButton.ManipulationDelta += Dragger_OnManipulationDelta;
            DraggerButton.ManipulationCompleted += Dragger_ManipulationCompleted;

            Loaded += (s, e) => ParentCollection = this.GetFirstAncestorOfType<CollectionView>();
            Tapped += OnTapped;
            DoubleTapped += OnDoubleTapped;
        }

        private void SetUpMenu()
        {
            var layout = new Action(OpenLayout);
            var copy = new Action(CopyDocument);
            var delete = new Action(DeleteDocument);
            var documentButtons = new List<MenuButton>()
            {
                new MenuButton(Symbol.Pictures, "Layout", Colors.LightBlue,layout),
                new MenuButton(Symbol.Copy, "Copy", Colors.LightBlue,copy),
                new MenuButton(Symbol.Delete, "Delete", Colors.LightBlue,delete)
            };
            _docMenu = new OverlayMenu(null, documentButtons);
            Binding visibilityBinding = new Binding()
            {
                Source = ViewModel,
                Path = new PropertyPath(nameof(ViewModel.DocMenuVisibility)),
                Mode = BindingMode.OneWay
            };
            _docMenu.SetBinding(OverlayMenu.VisibilityProperty, visibilityBinding);
            xMenuCanvas.Children.Add(_docMenu);
        }

        /// <summary>
        /// Update viewmodel when manipulator moves document
        /// </summary>
        /// <param name="translationDelta"></param>
        private void ManipulatorOnOnManipulatorTranslated(Point translationDelta)
        {
            var documentViewModel = this.DataContext as DocumentViewModel;
            documentViewModel.Position = new Point(documentViewModel.Position.X + translationDelta.X, documentViewModel.Position.Y + translationDelta.Y);
        }

        public DocumentView(DocumentViewModel documentViewModel) : this()
        {
            DataContext = documentViewModel;

        }



        /// <summary>
        /// Resizes the CollectionView according to the increments in width and height. 
        /// The CollectionListView vertically resizes corresponding to the change in the size of its cells, so if ProportionalScaling is true and the ListView is being displayed, 
        /// the Grid must change size to accomodate the height of the ListView.
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        public void Resize(double dx = 0, double dy = 0)
        {
            var dvm = DataContext as DocumentViewModel;
            dvm.Width = ActualWidth + dx;
            dvm.Height = ActualHeight + dy;


            // todo: remove this and replace with binding // debug why x:Bind fails
            Width = ActualWidth + dx;
            Height = ActualHeight + dy;

        }

        /// <summary>
        /// Called when the user holds the dragger button, or finishes holding it; 
        /// if the button is held down, initiates the proportional resizing mode.
        /// </summary>
        /// <param name="sender">DraggerButton in the DocumentView class</param>
        /// <param name="e"></param>
        public void DraggerButtonHolding(object sender, HoldingRoutedEventArgs e)
        {
            if (e.HoldingState == HoldingState.Started)
            {
                ProportionalScaling = true;
            }
            else if (e.HoldingState == HoldingState.Completed)
            {
                ProportionalScaling = false;
            }
        }

        /// <summary>
        /// Resizes the control based on the user's dragging the DraggerButton.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Dragger_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            Point p = Util.DeltaTransformFromVisual(e.Delta.Translation, sender as FrameworkElement);
            Resize(p.X, p.Y);
            e.Handled = true;
        }

        /// <summary>
        /// If the user was resizing proportionally, ends the proportional resizing and 
        /// changes the DraggerButton back to its normal appearance.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Dragger_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (ProportionalScaling)
            {
                ProportionalScaling = false;
            }
        }

        /// <summary>
        /// Called whenever a field is changed on the document
        /// </summary>
        /// <param name="fieldReference"></param>
        private void DocumentModel_DocumentFieldUpdated(ReferenceFieldModel fieldReference)
        {
            // ResetFields(_vm);
            // Debug.WriteLine("DocumentView.DocumentModel_DocumentFieldUpdated COMMENTED OUT LINE");
        }

        private void updateIcon()
        {
            // when you want a new icon, you have to add a check for it here!
            if (ViewModel.IconType == IconTypeEnum.Document)
            {
                xIconImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/doc-icon.png"));
            }
            else if (ViewModel.IconType == IconTypeEnum.Collection)
            {
                xIconImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/col-icon.png"));
            }
            else if (ViewModel.IconType == IconTypeEnum.Api)
            {
                xIconImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/api-icon.png"));
            }
        }

        /// <summary>
        /// The first time the local DocumentViewModel _vm can be set to the new datacontext
        /// this resets the fields otherwise does nothing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void DocumentView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            // if _vm has already been set return
            if (ViewModel != null)
            {
                return;
            }
            ViewModel = DataContext as DocumentViewModel;
            // if new _vm is not correct return
            if (ViewModel == null)
                return;

            if (ViewModel.DocumentController.DocumentModel.DocumentType.Type.Equals("operator"))
            {
                XGrid.Background = new SolidColorBrush(Colors.Transparent);
                xBorder.Opacity = 0;
            }
            Debug.WriteLine(ViewModel.DocumentController.DocumentModel.DocumentType.Type);
            if (ViewModel.DocumentController.DocumentModel.DocumentType.Type.Equals("collection"))
            {
                xBorder.Opacity = 0;
            }

            SetUpMenu();
            ViewModel.CloseMenu();

            #region LUKE HACKED THIS TOGETHER MAKE HIM FIX IT

            ViewModel.PropertyChanged += (o, eventArgs) =>
            {
                if (eventArgs.PropertyName == "IsMoveable")
                {
                    if (ViewModel.IsMoveable)
                    {
                        manipulator.AddAllAndHandle();
                    }
                    else
                    {
                        manipulator.RemoveAllButHandle();
                    }
                }
            };

            if (ViewModel.IsMoveable) manipulator.AddAllAndHandle();
            else manipulator.RemoveAllButHandle();

            #endregion
        }

        private void OuterGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ClipRect.Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height);

            // update collapse info
            // collapse to icon view on resize
            int pad = 32;
            if (Width < MinWidth + pad && Height < MinHeight + pad)
            {
                updateIcon();
                XGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                xIcon.Visibility = Windows.UI.Xaml.Visibility.Visible;
                xBorder.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                Tapped -= OnTapped;
                if (_docMenu != null) ViewModel.CloseMenu();
            }
            else
            {
                XGrid.Visibility = Windows.UI.Xaml.Visibility.Visible;
                xIcon.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                xBorder.Visibility = Windows.UI.Xaml.Visibility.Visible;
                Tapped += OnTapped;
            }
        }

        private void XEditButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var position = e.GetPosition(OverlayCanvas.Instance);
            OverlayCanvas.Instance.OpenInterfaceBuilder(ViewModel, position);
        }

        bool singleTap = false;

        ///// <summary>
        ///// Shows context menu on doubletap. Some fancy recognition: hides on either double tap or
        ///// on signle tap to prevent flickering.
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private void XGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) {


        //        if (xContextMenu.Visibility == Windows.UI.Xaml.Visibility.Visible)
        //            xContextMenu.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        //        else
        //            xContextMenu.Visibility = Windows.UI.Xaml.Visibility.Visible;


        //    singleTap = false;
        //    e.Handled = true;
        //}

        private void ExpandContract_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            // if in icon view expand to default size
            if (xIcon.Visibility == Windows.UI.Xaml.Visibility.Visible)
            {
                Height = 300;
                Width = 300;

                var dvm = DataContext as DocumentViewModel;
                dvm.Width = 300;
                dvm.Height = 300;

                // if in default view, show context menu
            }
            else
            {
                Height = MinWidth;
                Width = MinHeight;

                var dvm = DataContext as DocumentViewModel;
                dvm.Width = MinWidth;
                dvm.Height = MinHeight;
            }

            e.Handled = true; // prevent propagating
        }


        //// hides context menu on single tap
        //private async void XGrid_Tapped(object sender, TappedRoutedEventArgs e) {
        //    singleTap = true;
        //    await Task.Delay(150);
        //    if (singleTap)
        //        xContextMenu.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

        //}

        #region Menu

        public void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (_docMenu.Visibility == Visibility.Collapsed && !HasCollection)
                ViewModel.OpenMenu();
            else
                ViewModel.CloseMenu();
            e.Handled = true;
        }

        private void DeleteDocument()
        {
            throw new NotImplementedException();
        }

        private void CopyDocument()
        {
            throw new NotImplementedException();
        }

        private void UserControl_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (ParentCollection == null) return;
            ParentCollection.MaxZ += 1;
            Canvas.SetZIndex(this.GetFirstAncestorOfType<ContentPresenter>(), ParentCollection.MaxZ);
        }

        private void XGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {

        }

        private void OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            // TODO KB made to test doccontextlist, delete later 
            /* 
            ObservableCollection<DocumentController> docList = ViewModel.DocumentController.DocContextList; 
            Debug.WriteLine("count in this list is " + ViewModel.DocumentController.DocContextList.Count); 
            */

            // test exporting 
            ExportAsJson(); 

            //send email 


            e.Handled = true;
        }

        private async Task SendEmail(Windows.ApplicationModel.Contacts.Contact recipient, string message, StorageFile attachmentFile)
        {
            var emailMessage = new Windows.ApplicationModel.Email.EmailMessage();
            emailMessage.Body = message; 

            if (attachmentFile != null)
            {
                var stream = Windows.Storage.Streams.RandomAccessStreamReference.CreateFromFile(attachmentFile); 
                var attachment = new Windows.ApplicationModel.Email.EmailAttachment(attachmentFile.Name, stream);
                emailMessage.Attachments.Add(attachment); 
            }

            //var email = recipient.Emails.FirstOrDefault<Windows.ApplicationModel.Contacts.ContactEmail>();
            //if (email != null)
            //{
                //var emailRecipient = new Windows.ApplicationModel.Email.EmailRecipient(email.Address);
                //emailMessage.To.Add(emailRecipient);
            //}

            await Windows.ApplicationModel.Email.EmailManager.ShowComposeNewEmailAsync(emailMessage);  
        }

        private async void ExportAsImage()
        {
            FolderPicker picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeFilter.Add("*");
            StorageFolder folder = null;
            folder = await picker.PickSingleFolderAsync();

            if (folder != null)
            {
                StorageFile file = await folder.CreateFileAsync("img.jpg", CreationCollisionOption.ReplaceExisting);
                //await FileIO.WriteTextAsync(file, json);
            }
        }

        private string JsonSerializeHelper(IEnumerable<KeyValuePair<Key, FieldModelController>> fields)
        {
            Dictionary<string, string> jsonDict = new Dictionary<string, string>();
            foreach (KeyValuePair<Key, FieldModelController> pair in fields)
            {
                string data = "";
                if (pair.Value is TextFieldModelController)
                {
                    TextFieldModelController cont = pair.Value as TextFieldModelController;
                    data = cont.Data;
                }
                else if (pair.Value is NumberFieldModelController)
                {
                    NumberFieldModelController cont = pair.Value as NumberFieldModelController;
                    data = cont.Data.ToString();
                }
                else if (pair.Value is ImageFieldModelController)
                {
                    ImageFieldModelController cont = pair.Value as ImageFieldModelController;
                    data = cont.Data.ToString();
                }
                else if (pair.Value is PointFieldModelController)
                {
                    PointFieldModelController cont = pair.Value as PointFieldModelController;
                    data = cont.Data.ToString();
                } 
                // TODO refactor the CollectionKey here into DashConstants
                else if (pair.Key == DocumentCollectionFieldModelController.CollectionKey)
                {
                    data = "["; 
                    DocumentCollectionFieldModelController collectionCont = pair.Value as DocumentCollectionFieldModelController;
                    foreach(DocumentController cont in collectionCont.GetDocuments())
                    {
                        data += JsonSerializeHelper(cont.EnumFields());
                        data += ", "; 
                    }
                    data += "]";
                    Debug.WriteLine(data);  
                }
                else
                {
                    // TODO throw this at some point 
                    //throw new NotImplementedException(); 
                }
                jsonDict[pair.Key.Name] = data;
                Debug.Write(""); 
            }
            return JsonConvert.SerializeObject(jsonDict);
        }

        private async void ExportAsJson()
        {
            string json = JsonSerializeHelper(ViewModel.DocumentController.EnumFields()); 
            Debug.WriteLine(json);

            FolderPicker picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeFilter.Add("*");
            StorageFolder folder = null;
            folder = await picker.PickSingleFolderAsync();

            if (folder != null)
            {
            StorageFile file = await folder.CreateFileAsync("sample.json", CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(file, json);
            }
        }

        private void OpenLayout()
        {
            throw new NotImplementedException();
        }

        #endregion

    }
}