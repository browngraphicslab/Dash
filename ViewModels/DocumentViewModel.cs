using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using DashShared;
using Windows.Foundation;
using Visibility = Windows.UI.Xaml.Visibility;

namespace Dash
{
    public enum IconTypeEnum { Document, Collection, Api } // on super-collapse, what icon is displayed?

    public class DocumentViewModel : BaseSelectionElementViewModel, IDisposable
    {

        // == MEMBERS, GETTERS, SETTERS ==
        private double _height;
        private double _width;
        private double _groupMargin = 40;
        private TransformGroupData _normalGroupTransform = new TransformGroupData(new Point(), new Point(), new Point(1, 1));
        private TransformGroupData _interfaceBuilderGroupTransform;
        private Brush _backgroundBrush;
        private Brush _borderBrush;
        private IconTypeEnum iconType;
        private Visibility _docMenuVisibility = Visibility.Collapsed;
        private bool _menuOpen = false;
        public string DebugName = "";
        public bool DoubleTapEnabled = true;
        public DocumentController DocumentController { get; set; }

        bool _hasTitle = false;
        public bool HasTitle
        {
            get => _hasTitle;
            set => SetProperty(ref _hasTitle, value);
        }
        public void SetHasTitle(bool active)
        {
            if (active)
                HasTitle = active;
            else HasTitle = DocumentController.GetDataDocument(null).HasTitle && !Undecorated;
        }
        public struct WidthAndMenuOpenWrapper
        {
            public double Width { get; set; }
            public bool MenuOpen { get; set; }
            public WidthAndMenuOpenWrapper(double width, bool menuOpen)
            {
                Width = width;
                MenuOpen = menuOpen;
            }
        }

        public bool IsDraggerVisible
        {
            get => _isDraggerVisible;
            set => SetProperty(ref _isDraggerVisible, value);
        }

        public bool MenuOpen
        {
            get => _menuOpen;
            set => SetProperty(ref _menuOpen, value);
        }

        public IconTypeEnum IconType => iconType;

        public ObservableCollection<DocumentModel> DataBindingSource { get; set; } =
            new ObservableCollection<DocumentModel>();

        public double Width
        {
            get => _width;
            set
            {
                //Debug.Assert(double.IsNaN(value) == false);

                if (SetProperty(ref _width, value))
                {
                    var widthFieldModelController =
                        LayoutDocument.GetDereferencedField(KeyStore.WidthFieldKey, new Context(DocumentController)) as NumberController;

                    if (widthFieldModelController != null)
                    {
                        widthFieldModelController.Data = value;
                    }
                }
            }
        }

        public double Height
        {
            get => _height;
            set
            {
                //Debug.Assert(double.IsNaN(value) == false);

                if (SetProperty(ref _height, value))
                {
                    var heightFieldModelController =
                        LayoutDocument.GetDereferencedField(KeyStore.HeightFieldKey, new Context(DocumentController)) as
                            NumberController;
                    if (heightFieldModelController != null)
                        heightFieldModelController.Data = value;
                }
            }
        }

        public TransformGroupData GroupTransform
        {
            get => IsInInterfaceBuilder ? _interfaceBuilderGroupTransform : _normalGroupTransform;
            set
            {
                if (IsInInterfaceBuilder)
                {
                    SetProperty(ref _interfaceBuilderGroupTransform, value);
                    return;
                }

                if (SetProperty(ref _normalGroupTransform, value))
                {
                    var context = new Context(DocumentController);

                     // set position
                    var posFieldModelController =
                        LayoutDocument.GetDereferencedField(KeyStore.PositionFieldKey, context) as
                            PointController;
                    //if(!PointEquals(posFieldModelController.Data, _normalGroupTransform.Translate))
                    Debug.Assert(posFieldModelController != null, "posFieldModelController != null");
                    posFieldModelController.Data = value.Translate;
                    // set scale center
                    var scaleCenterFieldModelController =
                        LayoutDocument.GetDereferencedField(KeyStore.ScaleCenterFieldKey, context) as
                            PointController;
                    if (scaleCenterFieldModelController != null)
                        scaleCenterFieldModelController.Data = value.ScaleCenter;
                    // set scale amount
                    var scaleAmountFieldModelController =
                        LayoutDocument.GetDereferencedField(KeyStore.ScaleAmountFieldKey, context) as
                            PointController;
                    if (scaleAmountFieldModelController != null)
                        scaleAmountFieldModelController.Data = value.ScaleAmount;
                }

                _groupingBounds = new TranslateTransform
                {
                    X = GroupTransform.Translate.X,
                    Y = GroupTransform.Translate.Y
                }.TransformBounds(new Rect(-GroupMargin, -GroupMargin, _actualWidth + 2 * GroupMargin, _actualHeight + 2 * GroupMargin));
            }
        }
        public double GroupMargin
        {
            get => _groupMargin;
            set => SetProperty(ref _groupMargin, value);
        }

        private Rect _groupingBounds;

        public void UpdateActualSize(double actualwidth, double actualheight)
        {
            _actualWidth = actualwidth;
            _actualHeight = actualheight;
            _groupingBounds = new TranslateTransform
            {
                X = GroupTransform.Translate.X,
                Y = GroupTransform.Translate.Y
            }.TransformBounds(new Rect(-GroupMargin, -GroupMargin, _actualWidth + 2 * GroupMargin, _actualHeight + 2 * GroupMargin));

        }

        public Rect GroupingBounds => _groupingBounds;

        public void TransformDelta(TransformGroupData delta)
        {
            var currentTranslate = GroupTransform.Translate;
            var currentScaleAmount = GroupTransform.ScaleAmount;

            var deltaTranslate = delta.Translate;
            var deltaScaleAmount = delta.ScaleAmount;

            var translate = new Point(currentTranslate.X + deltaTranslate.X, currentTranslate.Y + deltaTranslate.Y);
            //delta does contain information about scale center as is, but it looks much better if you just zoom from middle tbh
            var scaleCenter = new Point(0, 0);
            var scaleAmount = new Point(currentScaleAmount.X * deltaScaleAmount.X, currentScaleAmount.Y * deltaScaleAmount.Y);

            GroupTransform = new TransformGroupData(translate, scaleCenter, scaleAmount);
        }

        public Brush BackgroundBrush
        {
            get => _backgroundBrush;
            set => SetProperty(ref _backgroundBrush, value);
        }

        public Brush BorderBrush
        {
            get => _borderBrush;
            set => SetProperty(ref _borderBrush, value);
        }

        public Visibility DocMenuVisibility
        {
            get => _docMenuVisibility;
            set => SetProperty(ref _docMenuVisibility, value);
        }

        private FrameworkElement _content = null;
        public FrameworkElement Content
        {
            get
            {
                if (_content == null)
                {
                    _content = DocumentController.MakeViewUI(null, IsInInterfaceBuilder, KeysToFrameworkElements);
                    //TODO: get mapping of key --> framework element
                }
                return _content;
            }
        }

        private Dictionary<KeyController, FrameworkElement> keysToFrameworkElements = new Dictionary<KeyController, FrameworkElement>();
        public Dictionary<KeyController, FrameworkElement> KeysToFrameworkElements { get => keysToFrameworkElements;
            set => keysToFrameworkElements = value;
        }

        private bool _isDraggerVisible = true;
        private double _actualWidth;
        private double _actualHeight;

        public void UpdateContent()
        {
            _content = null;
            OnPropertyChanged(nameof(Content));
        }


        public Context Context { get; set; }

        public bool Undecorated { get; set; }

        // == CONSTRUCTOR ==
        public DocumentViewModel(DocumentController documentController, bool isInInterfaceBuilder = false, Context context = null) : base(isInInterfaceBuilder)
        {
            DocumentController = documentController;//TODO This would be useful but doesn't work//.GetField(KeyStore.PositionFieldKey) == null ? documentController.GetViewCopy(null) :  documentController;

            BackgroundBrush = new SolidColorBrush(Colors.White);
            BorderBrush = new SolidColorBrush(Colors.LightGray);
            DataBindingSource.Add(documentController.DocumentModel);

            SetUpSmallIcon();
            _interfaceBuilderGroupTransform = new TransformGroupData(new Point(), new Point(), new Point(1, 1));
            documentController.AddFieldUpdatedListener(KeyStore.ActiveLayoutKey, DocumentController_LayoutUpdated);
            var newContext = new Context(context);  // bcz: not sure if this is right, but it avoids layout cycles with collections
            newContext.AddDocumentContext(DocumentController);
            OnActiveLayoutChanged(newContext);
            Context = newContext;
            
            DocumentController.GetDataDocument(context).AddFieldUpdatedListener(KeyStore.TitleKey, titleChanged);
            titleChanged(null, null, null);
        }
        void titleChanged(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            SetHasTitle(!Undecorated && DocumentController.GetDataDocument(null).HasTitle);
        }
        private void SetUpSmallIcon()
        {
            var iconFieldModelController =
                DocumentController.GetDereferencedField(KeyStore.IconTypeFieldKey, new Context(DocumentController)) as NumberController;
            if (iconFieldModelController == null)
            {
                iconFieldModelController = new NumberController((int)(IconTypeEnum.Document));
                DocumentController.SetField(KeyStore.IconTypeFieldKey, iconFieldModelController, true);
            }
            iconType = (IconTypeEnum)iconFieldModelController.Data;
            iconFieldModelController.FieldModelUpdated += IconFieldModelController_FieldModelUpdatedEvent;
        }

        private void DocumentController_LayoutUpdated(FieldControllerBase fieldControllerBase, FieldUpdatedEventArgs fieldUpdatedEventArgs, Context context)
        {
            if (fieldUpdatedEventArgs.Action != DocumentController.FieldUpdatedAction.Replace)
            {
                return;
            }
            var dargs = (DocumentController.DocumentFieldUpdatedEventArgs) fieldUpdatedEventArgs;
            Debug.Assert(dargs.Reference.FieldKey.Equals(KeyStore.ActiveLayoutKey));
            Debug.WriteLine(dargs.Action);
            OnActiveLayoutChanged(new Context(DocumentController));
            if (dargs.OldValue == null) return;
            var oldLayoutDoc = (DocumentController)dargs.OldValue;
            RemoveListenersFromLayout(oldLayoutDoc);
        }

        private void RemoveListenersFromLayout(DocumentController oldLayoutDoc)
        {
            if (oldLayoutDoc == null) return;
            oldLayoutDoc.GetHeightField().FieldModelUpdated -= HeightFieldModelController_FieldModelUpdatedEvent;
            oldLayoutDoc.GetWidthField().FieldModelUpdated -= WidthFieldModelController_FieldModelUpdatedEvent;
            oldLayoutDoc.GetPositionField().FieldModelUpdated -= PosFieldModelController_FieldModelUpdatedEvent;
            oldLayoutDoc.GetScaleCenterField().FieldModelUpdated -= ScaleCenterFieldModelController_FieldModelUpdatedEvent;
            oldLayoutDoc.GetScaleAmountField().FieldModelUpdated -= ScaleAmountFieldModelController_FieldModelUpdatedEvent;
        }

        private void RemoveControllerListeners()
        {
            DocumentController.RemoveFieldUpdatedListener(KeyStore.ActiveLayoutKey, DocumentController_LayoutUpdated);
            var icon = (NumberController)DocumentController.GetDereferencedField(KeyStore.IconTypeFieldKey, new Context(DocumentController));
            icon.FieldModelUpdated -= IconFieldModelController_FieldModelUpdatedEvent;
        }
        
        public DocumentController LayoutDocument
        {
            get
            {
                var layoutDoc = DocumentController?.GetDereferencedField(KeyStore.ActiveLayoutKey, new Context(DocumentController)) as DocumentController;
                return layoutDoc == null ? DocumentController : layoutDoc;
            }
        }
        public delegate void OnLayoutChangedHandler(DocumentViewModel sender, Context c);

        public event OnLayoutChangedHandler LayoutChanged;

        private void OnActiveLayoutChanged(Context context)
        {
            UpdateContent();
            LayoutChanged?.Invoke(this, context);

            ListenToHeightField(DocumentController);
            ListenToWidthField(DocumentController);

            if (!IsInInterfaceBuilder)
            {
                ListenToTransformGroupField(DocumentController);
            }
        }

        private void ListenToTransformGroupField(DocumentController docController)
        {
            var activeLayout = docController.GetActiveLayout();
            if (activeLayout == null)
                activeLayout = docController;
            if (activeLayout != null)
            {
                var scaleCenterFieldModelController = docController.GetScaleCenterField();
                var scaleAmountFieldModelController = docController.GetScaleAmountField();
                if (scaleCenterFieldModelController != null)
                {
                    var posFieldModelController = docController.GetPositionField();
                    if (scaleAmountFieldModelController != null)
                        GroupTransform = new TransformGroupData(posFieldModelController.Data,
                            scaleCenterFieldModelController.Data, scaleAmountFieldModelController.Data);
                    posFieldModelController.FieldModelUpdated += PosFieldModelController_FieldModelUpdatedEvent;
                    scaleCenterFieldModelController.FieldModelUpdated +=
                        ScaleCenterFieldModelController_FieldModelUpdatedEvent;
                }
                if (scaleAmountFieldModelController != null)
                    scaleAmountFieldModelController.FieldModelUpdated +=
                        ScaleAmountFieldModelController_FieldModelUpdatedEvent;
            }

        }

        private void ListenToWidthField(DocumentController docController)
        {
            var widthField = docController.GetWidthField();
            if (widthField != null)
            {
                widthField.FieldModelUpdated += WidthFieldModelController_FieldModelUpdatedEvent;
                Width = widthField.Data;
            }
            else
                Width = double.NaN;
        }

        private void ListenToHeightField(DocumentController docController)
        {
            var heightField = docController.GetHeightField();
            if (heightField != null)
            {
                heightField.FieldModelUpdated += HeightFieldModelController_FieldModelUpdatedEvent;
                Height = heightField.Data;
            }
            else
                Height = double.NaN;
        }

        public void UpdateGridViewIconGroupTransform(double actualWidth, double actualHeight)
        {
            var max = actualWidth > actualHeight ? actualWidth : actualHeight;
            var translate = new TranslateTransform { X = 125 - actualWidth / 2, Y = 125 - actualHeight / 2 };
            var scale = new ScaleTransform { CenterX = translate.X + actualWidth / 2, CenterY = translate.Y + actualHeight / 2, ScaleX = 220.0 / max, ScaleY = 220.0 / max };
            var group = new TransformGroup();
            group.Children.Add(translate);
            group.Children.Add(scale);
        }

        // == FIELD UPDATED EVENT HANDLERS == 
        // these update the view model's variables when the document's corresponding fields update

        private void HeightFieldModelController_FieldModelUpdatedEvent(FieldControllerBase sender, FieldUpdatedEventArgs args, Context c)
        {
            var heightFieldModelController = sender as NumberController;
            if (heightFieldModelController != null)
            {
                Height = heightFieldModelController.Data;
            }
        }

        private void WidthFieldModelController_FieldModelUpdatedEvent(FieldControllerBase sender, FieldUpdatedEventArgs args, Context c)
        {
            var widthFieldModelController = sender as NumberController;
            if (widthFieldModelController != null)
            {
                Width = widthFieldModelController.Data;
            }
        }

        private void IconFieldModelController_FieldModelUpdatedEvent(FieldControllerBase sender, FieldUpdatedEventArgs args, Context c)
        {
            var iconFieldModelController = sender as NumberController;
            if (iconFieldModelController != null)
            {
                iconType = (IconTypeEnum)iconFieldModelController.Data;
            }
        }

        private void PosFieldModelController_FieldModelUpdatedEvent(FieldControllerBase sender, FieldUpdatedEventArgs args, Context c)
        {
            var posFieldModelController = sender as PointController;
            if (posFieldModelController != null)
            {
                GroupTransform = new TransformGroupData(posFieldModelController.Data, GroupTransform.ScaleCenter, GroupTransform.ScaleAmount);
            }
        }

        private void ScaleCenterFieldModelController_FieldModelUpdatedEvent(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            var scaleCenterFieldModelController = sender as PointController;
            if (scaleCenterFieldModelController != null)
            {
                GroupTransform = new TransformGroupData(GroupTransform.Translate, scaleCenterFieldModelController.Data, GroupTransform.ScaleAmount);
            }
        }

        private void ScaleAmountFieldModelController_FieldModelUpdatedEvent(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            var scaleAmountFieldModelController = sender as PointController;
            if (scaleAmountFieldModelController != null)
            {
                GroupTransform = new TransformGroupData(GroupTransform.Translate, GroupTransform.ScaleCenter, scaleAmountFieldModelController.Data);
            }
        }

        public void DocumentView_DragStarting(UIElement sender, DragStartingEventArgs args, BaseCollectionViewModel collectionViewModel)
        {
            var docView = sender as DocumentView;
            DocumentView.DragDocumentView = docView;

            // create border around the doc being dragged
            if (docView != null)
                docView.OuterGrid.BorderThickness = new Thickness(5);

            args.Data.Properties.Add(nameof(BaseCollectionViewModel), collectionViewModel);
            args.Data.Properties.Add("DocumentControllerList", new List<DocumentController>(new DocumentController[] { DocumentController }));
                // different sources based on whether it's a collection or a document 
            if (docView != null)
                docView.IsHitTestVisible = false; // so that collectionviews can't drop to anything within it 
        }

        public void OnCollectionSelectedChanged(bool isCollectionSelected)
        {
               
        }

        public void Dispose()
        {
            if (LayoutDocument != null)
            {
                RemoveListenersFromLayout(LayoutDocument);
            }
            RemoveControllerListeners();
        }
    }
}
