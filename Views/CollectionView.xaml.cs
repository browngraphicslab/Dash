using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Controls.Primitives;
using DashShared;
using Visibility = Windows.UI.Xaml.Visibility;

using System.Threading.Tasks;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI;
using System.Numerics;
using Windows.UI.Xaml.Data;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionView : UserControl
    {
        public int MaxZ { get; set; }

        public SelectionElement CurrentView { get; set; }

        private OverlayMenu _collectionMenu;

        public CollectionViewModel ViewModel
        {
            get => DataContext as CollectionViewModel;
            set => DataContext = value;
        }

        /// <summary>
        /// The <see cref="CollectionView"/> that this <see cref="CollectionView"/> is nested in. Can be null
        /// </summary>
        public CollectionView ParentCollection => this.GetFirstAncestorOfType<CollectionView>();

        /// <summary>
        /// The <see cref="CompoundOperatorEditor"/> that this <see cref="CollectionView"/> is nested in. Can be null
        /// </summary>
        public CompoundOperatorEditor CompoundFreeform => this.GetFirstAncestorOfType<CompoundOperatorEditor>();

        /// <summary>
        /// The <see cref="DocumentView"/> that this <see cref="CollectionView"/> is nested in. Can be null
        /// </summary>
        public DocumentView ParentDocument => this.GetFirstAncestorOfType<DocumentView>();

        public enum CollectionViewType
        {
            Freeform, List, Grid, Page, Text, DB, Schema
        }

        private CollectionViewType _viewType;
        
        public CollectionView(CollectionViewModel vm, CollectionViewType viewType = CollectionViewType.Freeform)
        {
            InitializeComponent();
            _viewType = viewType;
            ViewModel = vm;
            Loaded += CollectionView_Loaded;
            Unloaded += CollectionView_Unloaded;

        }

        #region Background Translation Variables
        private CanvasBitmap _bgImage;
        private bool _resourcesLoaded;
        private CanvasImageBrush _bgBrush;
        private Uri _backgroundPath = new Uri("ms-appx:///Assets/gridbg2.jpg");
        private const double _numberOfBackgroundRows = 2; // THIS IS A MAGIC NUMBER AND SHOULD CHANGE IF YOU CHANGE THE BACKGROUND IMAGE
        private float _backgroundOpacity = .95f;
        #endregion

        public void TryBindToParentDocumentSize()
        {
            Util.ForceBindHeightToParentDocumentHeight(this);
        }

        #region Load And Unload Initialization and Cleanup

        private void CollectionView_Unloaded(object sender, RoutedEventArgs e)
        {
            Loaded -= CollectionView_Loaded;
            Unloaded -= CollectionView_Unloaded;
            ViewModel.OnLowestSelectionSet -= OnLowestSelectionSet;
        }

        private void CollectionView_Loaded(object sender, RoutedEventArgs e)
        {
            if (_collectionMenu == null)
                MakeMenu();
            // set the top-level viewtype to be freeform by default
            if (ParentDocument == MainPage.Instance.xMainDocView)
            {
                _viewType = CollectionViewType.Freeform;
            }
            switch (_viewType)
            {
                case CollectionViewType.Freeform:
                    SetFreeformView();
                    break;
                case CollectionViewType.Grid:
                    SetGridView();
                    break;
                case CollectionViewType.Page:
                    SetBrowseView();
                    break;
                case CollectionViewType.DB:
                    SetDBView();
                    break;
                case CollectionViewType.Schema:
                    SetSchemaView();
                    break;
                case CollectionViewType.List:
                    SetListView();
                    break;
                case CollectionViewType.Text:
                    SetTextView();
                    break;
            }


            // use a fully dark gridbg for the parent-level, nested collectionviews
            // use a lighter background
            if (ParentDocument == MainPage.Instance.xMainDocView)
            {
                ParentDocument.IsMainCollection = true;
                xOuterGrid.BorderThickness = new Thickness(0);
                CurrentView.InitializeAsRoot();
                _backgroundPath = new Uri("ms-appx:///Assets/gridbg.jpg");
                (CurrentView as CollectionFreeformView).setBackgroundDarkness(true);
            }

            ViewModel.OnLowestSelectionSet += OnLowestSelectionSet; 
        }

        #endregion

        #region Operator connection output
        private void ConnectionEllipse_OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            e.Complete();
        }



        #endregion

        #region Connection input and output 
        private void ConnectionEllipseOutput_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            FireEllipseInteraction(sender, e, isInput: false, isPressed: true);
            //if (ParentCollection == null) return;
            //// containing documents docId
            //var docId = (ParentDocument.DataContext as DocumentViewModel)?.DocumentController.GetId();
            //// the ellipse which the link should attatch to
            //var el = (sender as Grid)?.Children[0] as Ellipse;
            //var outputKey = ViewModel.OutputKey ?? ViewModel.CollectionKey;
            //var ioRef = new IOReference(new DocumentFieldReference(docId, outputKey), true, TypeInfo.Collection, e, el, ParentDocument);

            //var freeform = ParentCollection.CurrentView as CollectionFreeformView;
            //if (CompoundFreeform != null) freeform = CompoundFreeform.xFreeFormEditor;
            //freeform.CanLink = true;
            //freeform.StartDrag(ioRef);
        }

        private void ConnectionEllipseOutput_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            FireEllipseInteraction(sender, e, isInput: false, isPressed: false);
        }

        
        private void ConnectionEllipseInput_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            FireEllipseInteraction(sender, e, isInput: true, isPressed: true);
            //if (ParentCollection == null) return;
            //var docId = (ParentDocument.DataContext as DocumentViewModel)?.DocumentController.GetId();
            //var el = ConnectionEllipseInput;
            //var outputKey = /*ViewModel.OutputKey ?? */ ViewModel.CollectionKey;
            //var ioRef = new IOReference(new DocumentFieldReference(docId, outputKey), false, TypeInfo.Collection, e, el, ParentDocument);

            //var freeform = ParentCollection.CurrentView as CollectionFreeformView;
            //if (CompoundFreeform != null) freeform = CompoundFreeform.xFreeFormEditor;
            //freeform.CanLink = true;
            //freeform.StartDrag(ioRef);
        }

        private void ConnectionEllipseInput_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            FireEllipseInteraction(sender, e, isInput: true, isPressed: false);
            //if (ParentCollection == null) return;
            //var docId = (ParentDocument.DataContext as DocumentViewModel)?.DocumentController.GetId();
            //var el = ConnectionEllipseInput;
            //var outputKey = /*ViewModel.OutputKey ?? */ ViewModel.CollectionKey;
            //var ioRef = new IOReference(new DocumentFieldReference(docId, outputKey), false, TypeInfo.Collection, e, el, ParentDocument);

            //var freeform = ParentCollection.CurrentView as CollectionFreeformView;
            //if (CompoundFreeform != null) freeform = CompoundFreeform.xFreeFormEditor;
            //freeform?.EndDrag(ioRef, false);
        }

        private void FireEllipseInteraction(object sender, PointerRoutedEventArgs e, bool isInput, bool isPressed)
        {
            if (ParentCollection == null) return;
            var docId = (ParentDocument.DataContext as DocumentViewModel)?.DocumentController.GetId();
            var el = (sender as Grid).Children[0] as Ellipse;
            KeyController refKey;
            if (!isInput)
                refKey = ViewModel.OutputKey ?? ViewModel.CollectionKey;
            else
                refKey = ViewModel.CollectionKey;

            var ioRef = new IOReference(new DocumentFieldReference(docId, refKey), !isInput, TypeInfo.Collection, e, el, ParentDocument);

            var freeform = ParentCollection.CurrentView as CollectionFreeformView;
            if (CompoundFreeform != null) freeform = CompoundFreeform.xFreeFormEditor;

            freeform.CanLink = isPressed;
            if (!isPressed)
                freeform.EndDrag(ioRef, false);
            else
                freeform.StartDrag(ioRef);
        }

        #endregion

        #region Menu
        private void SetFreeformView()
        {
            if (CurrentView is CollectionFreeformView) return;
            CurrentView = new CollectionFreeformView() { InkFieldModelController = ViewModel.InkFieldModelController };
            xContentControl.Content = CurrentView;
            ParentDocument?.ViewModel?.DocumentController?.GetActiveLayout()?.Data?.SetField(KeyStore.CollectionViewTypeKey, new TextFieldModelController(CollectionViewType.Freeform.ToString()), true);
            ViewModes?.HighlightAction(SetFreeformView);
        }

        private void SetTextView()
        {
            if (CurrentView is CollectionTextView) return;
            CurrentView = new CollectionTextView();
            xContentControl.Content = CurrentView;
            ParentDocument?.ViewModel?.DocumentController?.GetActiveLayout()?.Data?.SetField(KeyStore.CollectionViewTypeKey, new TextFieldModelController(CollectionViewType.Text.ToString()), true);
            ViewModes?.HighlightAction(SetTextView);
        }

        public void SetDBView()
        {
            if (CurrentView is CollectionDBView) return;
            CurrentView = new CollectionDBView();
            xContentControl.Content = CurrentView;
            ParentDocument?.ViewModel?.DocumentController?.GetActiveLayout()?.Data?.SetField(KeyStore.CollectionViewTypeKey, new TextFieldModelController(CollectionViewType.DB.ToString()), true);
            ViewModes?.HighlightAction(SetDBView);
        }
        private void SetSchemaView()
        {
            if (CurrentView is CollectionDBSchemaView) return;
            CurrentView = new CollectionDBSchemaView();
            xContentControl.Content = CurrentView;
            ParentDocument?.ViewModel?.DocumentController?.GetActiveLayout()?.Data?.SetField(KeyStore.CollectionViewTypeKey, new TextFieldModelController(CollectionViewType.Schema.ToString()), true);
            ViewModes?.HighlightAction(SetSchemaView);
        }

        private void SetListView()
        {
            if (CurrentView is CollectionListView) return;
            CurrentView = new CollectionListView();
            xContentControl.Content = CurrentView;
            ViewModes?.HighlightAction(SetListView);
        }
        private void SetBrowseView()
        {
            if (CurrentView is CollectionPageView) return;
            CurrentView = new CollectionPageView();
            xContentControl.Content = CurrentView;
            ParentDocument?.ViewModel?.DocumentController?.GetActiveLayout()?.Data?.SetField(KeyStore.CollectionViewTypeKey, new TextFieldModelController(CollectionViewType.Page.ToString()), true);
            ViewModes?.HighlightAction(SetBrowseView);
        }

        private void SetGridView()
        {
            if (CurrentView is CollectionGridView) return;
            CurrentView = new CollectionGridView();
            xContentControl.Content = CurrentView;
            ParentDocument?.ViewModel?.DocumentController?.GetActiveLayout()?.Data?.SetField(KeyStore.CollectionViewTypeKey, new TextFieldModelController(CollectionViewType.Grid.ToString()), true);
            ViewModes?.HighlightAction(SetGridView);
        }

        public void MakeSelectionModeMultiple()
        {
            ViewModel.ItemSelectionMode = ListViewSelectionMode.Multiple;
            ViewModel.CanDragItems = true;
            _collectionMenu.GoToDocumentMenu();

            if (CurrentView is CollectionFreeformView)
            {
                (CurrentView as CollectionFreeformView).IsSelectionEnabled = true;
            }
        }

        private void CloseMenu()
        {
            xMenuCanvas.Children.Remove(_collectionMenu);
            xMenuColumn.Width = new GridLength(0);
            var lvb = ((UIElement)xContentControl.Content)?.GetFirstDescendantOfType<ListViewBase>();
            var sitemsCount = lvb?.SelectedItems.Count;
            if (lvb?.SelectedItems.Count > 0)
                try
                {
                    lvb.SelectedItems.Clear();
                }
                catch (Exception)
                {
                }
        }

        private void SelectAllItems()
        {
            var view = CurrentView as ICollectionView;
            Debug.Assert(view != null, "make the view implement ICollectionView");
            view.ToggleSelectAllItems();
        }

        private void MakeSelectionModeSingle()
        {
            ViewModel.ItemSelectionMode = ListViewSelectionMode.Single;
            ViewModel.CanDragItems = true;
            _collectionMenu.BackToCollectionMenu();
        }

        private void MakeSelectionModeNone()
        {
            ViewModel.ItemSelectionMode = ListViewSelectionMode.None;
            ViewModel.CanDragItems = false;
            _collectionMenu.BackToCollectionMenu();

            if (CurrentView is CollectionFreeformView)
            {
                (CurrentView as CollectionFreeformView).IsSelectionEnabled = false;
            }
        }

        private void DeleteSelection()
        {
            ViewModel.DeleteSelected_Tapped();
        }

        private void DeleteCollection()
        {
            ParentDocument.DeleteDocument();
        }
        
        public MenuButton ViewModes;
        
        private void MakeMenu()
        {
            var menuColor = ((SolidColorBrush)App.Instance.Resources["WindowsBlue"]).Color;
            var collectionButtons = new List<MenuButton>
            {
                new MenuButton(Symbol.TouchPointer, "Select", menuColor, MakeSelectionModeMultiple)
                {
                    RotateOnTap = true
                },
                //toggle grid/list/freeform view buttons 
                (ViewModes = new MenuButton(
                    new List<Symbol> { Symbol.ViewAll, Symbol.BrowsePhotos, Symbol.Folder, Symbol.Admin, Symbol.View}, menuColor, 
                    new List<Action> { SetGridView, SetBrowseView, SetDBView, SetSchemaView, SetFreeformView})),
                new MenuButton(Symbol.Camera, "ScrCap", menuColor, ScreenCap)


            };

            if (ParentDocument.IsMainCollection == false)
                collectionButtons.Add(new MenuButton(Symbol.Delete, "Delete", menuColor, DeleteCollection));

            var documentButtons = new List<MenuButton>
            {
                new MenuButton(Symbol.Back, "Back", menuColor, MakeSelectionModeNone)
                {
                    RotateOnTap = true
                },
                new MenuButton(Symbol.Edit, "Interface", menuColor, null),
                new MenuButton(Symbol.SelectAll, "All", menuColor, SelectAllItems),
                new MenuButton(Symbol.Delete, "Delete", menuColor, DeleteSelection),
            };
            _collectionMenu = new OverlayMenu(collectionButtons, documentButtons);
        }

        private void OpenMenu()
        {
            if (xMenuCanvas.Children.Contains(_collectionMenu)) return;
            xMenuCanvas.Children.Add(_collectionMenu);
            xMenuColumn.Width = new GridLength(50);
            _collectionMenu.AddAndPlayOpenAnimation();
        }

        private void GetJson()
        {
            throw new NotImplementedException("The document view model does not have a context any more");
            //Util.ExportAsJson(ViewModel.DocumentContext.DocContextList); 
        }

        private void ScreenCap()
        {
            Util.ExportAsImage(xOuterGrid);
        }

        private void EnterCollection()
        {
            var rootFrame = Window.Current.Content as Frame;
            Debug.Assert(rootFrame != null);
            rootFrame.Navigate(typeof(MainPage), ParentDocument.ViewModel.DocumentController);

        }

        #endregion

        #region Collection Activation

        public void OnLowestSelectionSet(bool isLowestSelected)
        {
            // if we're the lowest selected then open the menu
            if (isLowestSelected)
            {
                OpenMenu();
                ParentDocument.ViewModel?.OpenMenu(); 
            }

            // if we are no longer the lowest selected and we are not the main collection then close the menu
            else if (_collectionMenu != null && !isLowestSelected && ParentDocument?.IsMainCollection == false)
            {
                CloseMenu();
                ParentDocument.ViewModel?.CloseMenu();
            }
        }

        #endregion



        private void SetInitialTransformOnBackground()
        {
            var composite = new TransformGroup();
            var scale = new ScaleTransform
            {
                CenterX = 0,
                CenterY = 0,
                ScaleX = 1,
                ScaleY = 1
            };

            composite.Children.Add(scale);
            SetTransformOnBackground(composite);
        }

        private void CanvasControl_OnCreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
        {
            var task = Task.Run(async () =>
            {
                // Load the background image and create an image brush from it
                _bgImage = await CanvasBitmap.LoadAsync(sender, _backgroundPath);
                _bgBrush = new CanvasImageBrush(sender, _bgImage)
                {
                    Opacity = _backgroundOpacity
                };

                // Set the brush's edge behaviour to wrap, so the image repeats if the drawn region is too big
                _bgBrush.ExtendX = _bgBrush.ExtendY = CanvasEdgeBehavior.Wrap;

                _resourcesLoaded = true;
            });
            args.TrackAsyncAction(task.AsAsyncAction());

            task.ContinueWith(continuationTask =>
            {
                SetInitialTransformOnBackground();
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }


        public void SetTransformOnBackground(TransformGroup composite)
        {
            var aliasSafeScale = ClampBackgroundScaleForAliasing(composite.Value.M11, _numberOfBackgroundRows);

            if (_resourcesLoaded)
            {
                _bgBrush.Transform = new Matrix3x2((float)aliasSafeScale,
                    (float)composite.Value.M12,
                    (float)composite.Value.M21,
                    (float)aliasSafeScale,
                    (float)composite.Value.OffsetX,
                    (float)composite.Value.OffsetY);
                xBackgroundCanvas.Invalidate();
            }
        }

        private double ClampBackgroundScaleForAliasing(double currentScale, double numberOfBackgroundRows)
        {
            while (currentScale / numberOfBackgroundRows > numberOfBackgroundRows)
            {
                currentScale /= numberOfBackgroundRows;
            }

            while (currentScale * numberOfBackgroundRows < numberOfBackgroundRows)
            {
                currentScale *= numberOfBackgroundRows;
            }
            return currentScale;
        }

        private void CanvasControl_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            if (!_resourcesLoaded) return;

            // Just fill a rectangle with our tiling image brush, covering the entire bounds of the canvas control
            var session = args.DrawingSession;
            session.FillRectangle(new Rect(new Point(), sender.Size), _bgBrush);
        }

        /// <summary>
        /// Binds the hit test visibility of xContentControl to the IsSelected of DocumentVieWModel as opposed to CollectionVieWModel 
        /// in order to make ellipses hit test visible and the rest not 
        /// </summary>
        private void xContentControl_Loaded(object sender, RoutedEventArgs e)         
        {
            var docView = xOuterGrid.GetFirstAncestorOfType<DocumentView>();
            var datacontext = docView?.DataContext as DocumentViewModel;

            if (datacontext == null) return;
            var visibilityBinding = new Binding
            {
                Source = datacontext,
                Path = new PropertyPath(nameof(datacontext.IsSelected)) 
            };
            xContentControl.SetBinding(IsHitTestVisibleProperty, visibilityBinding); 
        }
    }
}
