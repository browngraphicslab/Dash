using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Visibility = Windows.UI.Xaml.Visibility;
using Windows.UI.Xaml.Controls.Primitives;
using DashShared;
using Dash.Controllers.Operators;


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236
namespace Dash
{

    public sealed partial class DocumentView : SelectionElement
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

        public bool ProportionalScaling { get; set; }
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
            MinWidth = 120;
            MinHeight = 96;

            startWidth = Width;
            startHeight = Height;

            //xContextMenu.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            DraggerButton.Holding += DraggerButtonHolding;
            DraggerButton.ManipulationDelta += Dragger_OnManipulationDelta;
            DraggerButton.ManipulationCompleted += Dragger_ManipulationCompleted;
            Tapped += OnTapped;
            DoubleTapped += ExpandContract_DoubleTapped;
        }
        private void This_Loaded(object sender, RoutedEventArgs e)
        {
            ParentCollection = this.GetFirstAncestorOfType<CollectionView>();
        }

        private void SetUpMenu() {
            Color bgcolor = (Application.Current.Resources["WindowsBlue"] as SolidColorBrush).Color;

            var documentButtons = new List<MenuButton>()
            {
                new MenuButton(Symbol.Pictures, "Layout",bgcolor,OpenLayout),
                new MenuButton(Symbol.Copy, "Copy",bgcolor,CopyDocument),
                new MenuButton(Symbol.SetTile, "Delegate",bgcolor, MakeDelegate),
                new MenuButton(Symbol.Delete, "Delete",bgcolor,DeleteDocument),
                new MenuButton(Symbol.Camera, "ScrCap",bgcolor, ScreenCap),
                new MenuButton(Symbol.Placeholder, "Commands",bgcolor, CommandLine),
                new MenuButton(Symbol.Page, "Json",bgcolor, GetJson)
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
            ViewModel.OpenMenu();
        }


        /// <summary>
        /// Update viewmodel when manipulator moves document
        /// </summary>
        /// <param name="delta"></param>
        private void ManipulatorOnOnManipulatorTranslated(TransformGroupData delta)
        {
            var currentTranslate = ViewModel.GroupTransform.Translate;
            var currentScaleAmount = ViewModel.GroupTransform.ScaleAmount;

            var deltaTranslate = delta.Translate;
            var deltaScaleAmount = delta.ScaleAmount;

            var translate = new Point(currentTranslate.X + deltaTranslate.X, currentTranslate.Y + deltaTranslate.Y);
            //delta does contain information about scale center as is, but it looks much better if you just zoom from middle tbh.a
            var scaleCenter = new Point(ActualWidth / 2, ActualHeight / 2);
            var scaleAmount = new Point(currentScaleAmount.X * deltaScaleAmount.X, currentScaleAmount.Y * deltaScaleAmount.Y);

            ViewModel.GroupTransform = new TransformGroupData(translate, scaleCenter, scaleAmount);
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
        public Size Resize(double dx = 0, double dy = 0)
        {
            var dvm = DataContext as DocumentViewModel;
            dvm.Width = Math.Max(double.IsNaN(dvm.Width) ? ActualWidth + dx : dvm.Width + dx, 0);
            dvm.Height = Math.Max(double.IsNaN(dvm.Height) ? ActualHeight + dy : dvm.Height + dy, 0);
            return new Size(dvm.Width, dvm.Height);
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
            var s = Resize(p.X, p.Y);
            var position = ViewModel.GroupTransform.Translate;
            var dx = Math.Max(p.X, 0);
            var dy = Math.Max(p.Y, 0);
            //p = new Point(dx, dy);
            ViewModel.GroupTransform = new TransformGroupData(new Point(position.X - p.X / 2.0f, position.Y - p.Y / 2.0f),
                                                                new Point(s.Width / 2.0f, s.Height / 2.0f),
                                                                ViewModel.GroupTransform.ScaleAmount); 
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


        void initDocumentOnDataContext() {

            // document type specific styles >> use VERY sparringly
            var docType = ViewModel.DocumentController.DocumentModel.DocumentType;
            if (docType.Type != null) {
                // hide white background & drop shadow on operator views
                if (docType.Type.Equals("operator")) {
                    XGrid.Background = new SolidColorBrush(Colors.Transparent);
                    xBorder.Opacity = 0;
                }
            } else {

                ViewModel.DocumentController.DocumentModel.DocumentType.Type = docType.Id.Substring(0, 5);
            }

            // if there is a readable document type, use that as label
            var sourceBinding = new Binding {
                Source = ViewModel.DocumentController.DocumentModel.DocumentType,
                Path = new PropertyPath(nameof(ViewModel.DocumentController.DocumentModel.DocumentType.Type)),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            xIconLabel.SetBinding(TextBox.TextProperty, sourceBinding);

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
            if (ViewModel != null || DataContext == null)
                return;

            ViewModel = DataContext as DocumentViewModel;
            // if new _vm is not correct return
            if (ViewModel == null)
                return;

            initDocumentOnDataContext();
            SetUpMenu();
            ViewModel.CloseMenu();

            if (ViewModel.IsInInterfaceBuilder)
            {
                SetInterfaceBuilderSpecificSettings();
            }

        }

        private void SetInterfaceBuilderSpecificSettings()
        {
            RemoveScroll();
        }

        private void OuterGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ClipRect.Rect = ViewModel.MenuOpen ? new Rect(0, 0, e.NewSize.Width - 55, e.NewSize.Height) : new Rect(0, 0, e.NewSize.Width, e.NewSize.Height);
            ViewModel.UpdateGridViewIconGroupTransform(ActualWidth, ActualHeight);

            if (ViewModel != null)
                ViewModel.UpdateGridViewIconGroupTransform(ActualWidth, ActualHeight);
            // update collapse info
            // collapse to icon view on resize
            int pad = 1;
             if (Width < MinWidth + pad && Height < MinHeight + xIconLabel.ActualHeight) {
                updateIcon();
                XGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                xIcon.Visibility = Windows.UI.Xaml.Visibility.Visible;
                xBorder.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                xDragImage.Opacity = 0;
                Tapped -= OnTapped;
                if (_docMenu != null) ViewModel.CloseMenu();
            } else {
                XGrid.Visibility = Windows.UI.Xaml.Visibility.Visible;
                xIcon.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                xBorder.Visibility = Windows.UI.Xaml.Visibility.Visible;
                xDragImage.Opacity = 1;
                Tapped += OnTapped;
            }
        }

        private void ExpandContract_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) {
            // if in icon view expand to default size
            if (xIcon.Visibility == Visibility.Visible)
            {
                Resize(300, 300);
                
            }/*
            else
            {
                Height = MinWidth;
                Width = MinHeight;

                var dvm = DataContext as DocumentViewModel;
                dvm.Width = MinWidth;
                dvm.Height = MinHeight;
            }
            */
            e.Handled = true; // prevent propagating
        }

  #region Menu



        private void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (ViewModel.IsInInterfaceBuilder)
            {
                return;
            }

            OnSelected();
            e.Handled = true;
        }

        public void DeleteDocument()
        {
            FadeOut.Begin();
        }

        private void CopyDocument()
        {
            ParentCollection.ViewModel.CollectionFieldModelController.AddDocument(ViewModel.Copy());
        }

        private void MakeDelegate()
        {
            ParentCollection.ViewModel.CollectionFieldModelController.AddDocument(ViewModel.GetDelegate());
        }

        public void ScreenCap()
        {
            Util.ExportAsImage(OuterGrid);
        }

        public void CommandLine()
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)this.XGrid);
        }

        public void GetJson()
        {
            Util.ExportAsJson(ViewModel.DocumentController.EnumFields());
        }

        private void UserControl_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (ParentCollection == null) return;
            ParentCollection.MaxZ += 1;
            Canvas.SetZIndex(this.GetFirstAncestorOfType<ContentPresenter>(), ParentCollection.MaxZ);
        }

        private void FadeOut_Completed(object sender, object e)
        {
            ParentCollection.ViewModel.CollectionFieldModelController.RemoveDocument(ViewModel.DocumentController);
        }

        private void This_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
            var point = e.GetCurrentPoint(ParentCollection);
            var scaleSign = point.Properties.MouseWheelDelta / 120.0f;
            var scale = scaleSign > 0 ? 1.05 : 1.0 / 1.05;
            var newScale = new Point(ViewModel.GroupTransform.ScaleAmount.X * scale, ViewModel.GroupTransform.ScaleAmount.Y * scale);
            ViewModel.GroupTransform = new TransformGroupData(ViewModel.GroupTransform.Translate,
                                                              ViewModel.GroupTransform.ScaleCenter,
                                                              newScale);
        }

        private void OpenLayout()
        {
            MainPage.Instance.DisplayElement(new InterfaceBuilder(ViewModel.DocumentController), new Point(0,0), this);
        }

        private void CommandLine_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            if (!(tb.Text.EndsWith("\r")))
                return;
            var docController = (DataContext as DocumentViewModel).DocumentController;
            foreach (var tag in (sender as TextBox).Text.Split('#'))
                if (tag.Contains("="))
                {
                    var eqPos = tag.IndexOfAny(new char[] { '=' });
                    var word = tag.Substring(0, eqPos).TrimEnd(' ').TrimStart(' ');
                    var valu = tag.Substring(eqPos + 1, Math.Max(0, tag.Length - eqPos - 1)).TrimEnd(' ', '\r');
                    var key = new Key(word, word);
                    foreach (var keyFields in docController.EnumFields())
                        if (keyFields.Key.Name == word)
                        {
                            key = keyFields.Key;
                            break;
                        }

                    if (valu.StartsWith("@") && !valu.Contains("="))
                    {
                        var proto = docController.GetPrototype() == null ? docController : docController.GetPrototype();
                        proto.SetField(DashConstants.KeyStore.ThisKey, new DocumentFieldModelController(proto), true);

                        var searchDoc = DBSearchOperatorFieldModelController.CreateSearch(new ReferenceFieldModelController(proto.GetId(), DashConstants.KeyStore.ThisKey), valu.Substring(1, valu.Length - 1));
                        proto.SetField(key, new ReferenceFieldModelController(searchDoc.GetId(), DBSearchOperatorFieldModelController.ResultsKey), true);
                    }
                    else if (valu.StartsWith("@"))
                    {
                        var eqPos2 = valu.IndexOfAny(new char[] { '=' });
                        var fieldName = valu.Substring(1, eqPos2-1).TrimEnd(' ').TrimStart(' ');
                        var fieldValue = valu.Substring(eqPos2 + 1, Math.Max(0, valu.Length - eqPos2 - 1)).Trim(' ', '\r');

                        foreach (var doc in ContentController.GetControllers<DocumentController>())
                            foreach (var field in doc.EnumFields())
                                if (field.Key.Name == fieldName && (field.Value as TextFieldModelController)?.Data == fieldValue)
                                {
                                    docController.SetField(key, new DocumentFieldModelController(doc), true);
                                    break;
                                }
                    }
                    else
                    {
                        var tagField = docController.GetDereferencedField(new Key(word, word), null);
                        if (tagField is TextFieldModelController)
                            (tagField as TextFieldModelController).Data = valu;
                        else docController.SetField(key, new TextFieldModelController(valu), true);
                    }
                }
        }

        public void RemoveScroll()
        {
            PointerWheelChanged -= This_PointerWheelChanged;
        }
        #endregion

        protected override void OnActivated(bool isSelected)
        {
            
        }

        public override void OnLowestActivated(bool isLowestSelected)
        {
            if (xIcon.Visibility == Visibility.Collapsed && !HasCollection && isLowestSelected)
                ViewModel?.OpenMenu();
            else
                ViewModel?.CloseMenu();
        }
        
    }
}