using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
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

    public class DocumentViewModel : ViewModelBase
    {
        // == MEMBERS, GETTERS, SETTERS ==
        private ManipulationModes _manipulationMode;
        private double _height;
        private double _width;
        private TransformGroupData _trans;
        private Brush _backgroundBrush;
        private Brush _borderBrush;
        private IconTypeEnum iconType;
        private TransformGroup _gridViewIconGroupTransform;
        private Visibility _docMenuVisibility;
        private GridLength _menuColumnWidth;
        private bool _menuOpen = false;
        private bool _isDetailedUserInterfaceVisible = true;
        private bool _isMoveable = true;
        private FrameworkElement _content;
        private WidthAndMenuOpenWrapper _widthBinding;
        public string DebugName = "";
        public bool DoubleTapEnabled = true;
        public DocumentController DocumentController;
        public WidthAndMenuOpenWrapper WidthBinding
        {
            get { return _widthBinding; }
            set { SetProperty(ref _widthBinding, value); }
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

        public bool MenuOpen
        {
            get { return _menuOpen; }
            set
            {
                if (SetProperty(ref _menuOpen, value))
                    WidthBinding = new WidthAndMenuOpenWrapper(Width, value);
            }
        }

        public TransformGroup GridViewIconGroupTransform
        {
            get { return _gridViewIconGroupTransform; }
            set { SetProperty(ref _gridViewIconGroupTransform, value); }
        }

        public IconTypeEnum IconType { get { return iconType; } }

        public ObservableCollection<DocumentModel> DataBindingSource { get; set; } =
            new ObservableCollection<DocumentModel>();

        public FrameworkElement Content
        {
            get { return _content; }
            set
            {
                SetProperty(ref _content, value);
            }
        }

        public double Width
        {
            get { return _width; }
            set
            {
                if (SetProperty(ref _width, value))
                {
                    var context = new Context(DocumentController);
                    var layoutDocController = (DocumentController.GetDereferencedField(DashConstants.KeyStore.ActiveLayoutKey, context) as DocumentFieldModelController)?.Data;

                    if (layoutDocController == null)
                        layoutDocController = DocumentController;

                    var widthFieldModelController =
                        layoutDocController.GetDereferencedField(DashConstants.KeyStore.WidthFieldKey, context) as
                            NumberFieldModelController;

                    if (widthFieldModelController != null)
                    {
                        widthFieldModelController.Data = value;
                        WidthBinding = new WidthAndMenuOpenWrapper(value, MenuOpen);
                    }
                }
            }
        }

        public double Height
        {
            get { return _height; }
            set
            {
                if (SetProperty(ref _height, value))
                {
                    var context = new Context(DocumentController);
                    var layoutDocController = (DocumentController.GetDereferencedField(DashConstants.KeyStore.ActiveLayoutKey, context) as DocumentFieldModelController)?.Data;

                    if (layoutDocController == null)
                        layoutDocController = DocumentController;
                    var heightFieldModelController =
                        layoutDocController.GetDereferencedField(DashConstants.KeyStore.HeightFieldKey, context) as
                            NumberFieldModelController;
                    if (heightFieldModelController != null)
                        heightFieldModelController.Data = value;
                }
            }
        }

        public TransformGroupData GroupTransform
        {
            get { return _trans; }
            set
            {
                if (SetProperty(ref _trans, value))
                {
                    // get layout
                    var context = new Context(DocumentController);
                    var layoutDocController = (DocumentController.GetDereferencedField(DashConstants.KeyStore.ActiveLayoutKey, context) as DocumentFieldModelController)?.Data;

                    if (layoutDocController == null)
                        layoutDocController = DocumentController;
                    // set position
                    var posFieldModelController =
                        layoutDocController.GetDereferencedField(DashConstants.KeyStore.PositionFieldKey, context) as
                            PointFieldModelController;
                    posFieldModelController.Data = value.Translate;
                    // set scale center
                    var scaleCenterFieldModelController =
                        layoutDocController.GetDereferencedField(DashConstants.KeyStore.ScaleCenterFieldKey, context) as
                            PointFieldModelController;
                    if (scaleCenterFieldModelController != null)
                        scaleCenterFieldModelController.Data = value.ScaleCenter;
                    // set scale amount
                    var scaleAmountFieldModelController =
                        layoutDocController.GetDereferencedField(DashConstants.KeyStore.ScaleAmountFieldKey, context) as
                            PointFieldModelController;
                    if (scaleAmountFieldModelController != null)
                        scaleAmountFieldModelController.Data = value.ScaleAmount;
                }
            }
        }

        public ManipulationModes ManipulationMode
        {
            get { return _manipulationMode; }
            set { SetProperty(ref _manipulationMode, value); }
        }

        public Brush BackgroundBrush
        {
            get { return _backgroundBrush; }
            set { SetProperty(ref _backgroundBrush, value); }
        }

        public Brush BorderBrush
        {
            get { return _borderBrush; }
            set { SetProperty(ref _borderBrush, value); }
        }

        public bool IsDetailedUserInterfaceVisible
        {
            get { return _isDetailedUserInterfaceVisible; }
            set { SetProperty(ref _isDetailedUserInterfaceVisible, value); }
        }

        public bool IsMoveable
        {
            get { return _isMoveable; }
            set { SetProperty(ref _isMoveable, value); }
        }

        public Visibility DocMenuVisibility
        {
            get { return _docMenuVisibility; }
            set { SetProperty(ref _docMenuVisibility, value); }
        }
        
        public readonly bool IsInInterfaceBuilder;
        
        public GridLength MenuColumnWidth
        {
            get { return _menuColumnWidth; }
            set { SetProperty(ref _menuColumnWidth, value); }
        }

        // == CONSTRUCTORS == 
        public DocumentViewModel() { }

  
        public DocumentViewModel(DocumentController documentController, bool isInInterfaceBuilder = false)
        {
            if (IsInInterfaceBuilder = isInInterfaceBuilder)
                ManipulationMode = ManipulationModes.None;
            DocumentController = documentController;
            BackgroundBrush = new SolidColorBrush(Colors.White);
            BorderBrush = new SolidColorBrush(Colors.LightGray);
            DataBindingSource.Add(documentController.DocumentModel);     

            SetUpSmallIcon();
            documentController.AddFieldUpdatedListener(DashConstants.KeyStore.ActiveLayoutKey, DocumentController_DocumentFieldUpdated);
            OnActiveLayoutChanged();
            WidthBinding = new WidthAndMenuOpenWrapper();
        }

        private void SetUpSmallIcon()
        {
            var iconFieldModelController =
                DocumentController.GetDereferencedField(DashConstants.KeyStore.IconTypeFieldKey, new Context(DocumentController)) as NumberFieldModelController;
            if (iconFieldModelController == null)
            {
                iconFieldModelController = new NumberFieldModelController((int)(IconTypeEnum.Document));
                DocumentController.SetField(DashConstants.KeyStore.IconTypeFieldKey, iconFieldModelController, true);
            }
            iconType = (IconTypeEnum)iconFieldModelController.Data;
            iconFieldModelController.FieldModelUpdated += IconFieldModelController_FieldModelUpdatedEvent;
        }

        private void DocumentController_DocumentFieldUpdated(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            Debug.Assert(args.Reference.FieldKey.Equals(DashConstants.KeyStore.ActiveLayoutKey));
            OnActiveLayoutChanged();
        }
        private void OnActiveLayoutChanged()
        {
            Content = DocumentController.MakeViewUI(new Context(DocumentController), IsInInterfaceBuilder);
            ListenToHeightField(DocumentController);
            ListenToWidthField(DocumentController);
            ListenToTransformGroupField(DocumentController);
        }

        private void ListenToTransformGroupField(DocumentController docController)
        {
            var posFieldModelController = docController.GetPositionField();
            var activeLayout = docController.GetActiveLayout()?.Data;
            if (activeLayout != null)
            {
                var scaleCenterFieldModelController =
                    activeLayout.GetDereferencedField(DashConstants.KeyStore.ScaleCenterFieldKey,
                        new Context(DocumentController)) as PointFieldModelController;
                var scaleAmountFieldModelController =
                    activeLayout.GetDereferencedField(DashConstants.KeyStore.ScaleAmountFieldKey,
                        new Context(DocumentController)) as PointFieldModelController;
                if (scaleCenterFieldModelController != null)
                {
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
            var translate = new TranslateTransform() { X = 125 - actualWidth / 2, Y = 125 - actualHeight / 2 };
            var scale = new ScaleTransform() { CenterX = translate.X + actualWidth / 2, CenterY = translate.Y + actualHeight / 2, ScaleX = 220.0 / max, ScaleY = 220.0 / max };
            var group = new TransformGroup();
            group.Children.Add(translate);
            group.Children.Add(scale);
            GridViewIconGroupTransform = group;
        }

        // == FIELD UPDATED EVENT HANDLERS == 
        // these update the view model's variables when the document's corresponding fields update

        private void HeightFieldModelController_FieldModelUpdatedEvent(FieldModelController sender, Context c)
        {
            var heightFieldModelController = sender as NumberFieldModelController;
            if (heightFieldModelController != null)
            {
                Height = heightFieldModelController.Data;
            }
        }

        private void WidthFieldModelController_FieldModelUpdatedEvent(FieldModelController sender, Context c)
        {
            var widthFieldModelController = sender as NumberFieldModelController;
            if (widthFieldModelController != null)
            {
                Width = widthFieldModelController.Data;
            }
        }

        private void IconFieldModelController_FieldModelUpdatedEvent(FieldModelController sender, Context c)
        {
            var iconFieldModelController = sender as NumberFieldModelController;
            if (iconFieldModelController != null)
            {
                iconType = (IconTypeEnum)iconFieldModelController.Data;
            }
        }

        private void PosFieldModelController_FieldModelUpdatedEvent(FieldModelController sender, Context c)
        {
            var posFieldModelController = sender as PointFieldModelController;
            if (posFieldModelController != null)
            {
                GroupTransform = new TransformGroupData(posFieldModelController.Data, GroupTransform.ScaleCenter, GroupTransform.ScaleAmount);
            }
        }

        private void ScaleCenterFieldModelController_FieldModelUpdatedEvent(FieldModelController sender, Context context)
        {
            var scaleCenterFieldModelController = sender as PointFieldModelController;
            if (scaleCenterFieldModelController != null)
            {
                GroupTransform = new TransformGroupData(GroupTransform.Translate, scaleCenterFieldModelController.Data, GroupTransform.ScaleAmount);
            }
        }

        private void ScaleAmountFieldModelController_FieldModelUpdatedEvent(FieldModelController sender, Context context)
        {
            var scaleAmountFieldModelController = sender as PointFieldModelController;
            if (scaleAmountFieldModelController != null)
            {
                GroupTransform = new TransformGroupData(GroupTransform.Translate, GroupTransform.ScaleCenter, scaleAmountFieldModelController.Data);
            }
        }

        public void CloseMenu()
        {
            DocMenuVisibility = Visibility.Collapsed;
            MenuColumnWidth = new GridLength(0);
            MenuOpen = false;
        }

        public void OpenMenu()
        {
            DocMenuVisibility = Visibility.Visible;
            MenuColumnWidth = new GridLength(50);
            MenuOpen = true;
        }

        public DocumentController Copy()
        {
            var copy = DocumentController.GetCopy();
            var layoutField = copy.GetActiveLayout().Data;
            var layoutCopy = layoutField.GetCopy();
            copy.SetActiveLayout(layoutCopy, forceMask: true, addToLayoutList: false);
            var positionField = copy.GetPositionField();
            if (positionField != null)
            {
                var oldPosition = DocumentController.GetPositionField().Data;
                positionField.Data = new Point(oldPosition.X + 15, oldPosition.Y + 15);
            }
            return copy;
        }

        public DocumentController GetDelegate()
        {
            var del = DocumentController.MakeDelegate();
            var delLayout = DocumentController.GetActiveLayout().Data.MakeDelegate();
            var oldPosition = DocumentController.GetPositionField().Data;
            delLayout.SetField(DashConstants.KeyStore.PositionFieldKey,
                new PointFieldModelController(new Point(oldPosition.X + 15, oldPosition.Y + 15)),
                true);
            del.SetActiveLayout(delLayout, forceMask: true, addToLayoutList: false);
            return del;
        }
    }
}
