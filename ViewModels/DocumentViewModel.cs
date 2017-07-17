using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using DashShared;
using Windows.Foundation;
using Visibility = Windows.UI.Xaml.Visibility;
using System;

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
        public bool DoubleTapEnabled = true;
        public DocumentController DocumentController;
        public TransformGroup GridViewIconGroupTransform
        {
            get { return _gridViewIconGroupTransform; }
            set { SetProperty(ref _gridViewIconGroupTransform, value); }
        }
        
        public IconTypeEnum IconType { get { return iconType; } }
        
        public ObservableCollection<DocumentModel> DataBindingSource { get; set; } =
            new ObservableCollection<DocumentModel>();

        private FrameworkElement _content;
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
                    var layoutDocController = (DocumentController.GetDereferencedField(DashConstants.KeyStore.LayoutKey, DocumentContext) as DocumentFieldModelController)?.Data;
                    if (layoutDocController == null)
                        layoutDocController = DocumentController;

                    var widthFieldModelController =
                        layoutDocController.GetDereferencedField(DashConstants.KeyStore.WidthFieldKey, DocumentContext) as
                            NumberFieldModelController;
                    widthFieldModelController.Data = value;
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
                    var layoutDocController = (DocumentController.GetDereferencedField(DashConstants.KeyStore.LayoutKey, DocumentContext) as DocumentFieldModelController)?.Data;
                    if (layoutDocController == null)
                        layoutDocController = DocumentController;
                    var heightFieldModelController =
                        layoutDocController.GetDereferencedField(DashConstants.KeyStore.HeightFieldKey, DocumentContext) as
                            NumberFieldModelController;
                    heightFieldModelController.Data = value;
                }
            }
        }

        public TransformGroupData GroupTransform
        {
            get { return _trans; }
            set {
                if (SetProperty(ref _trans, value))
                {
                    var layoutDocController = (DocumentController.GetDereferencedField(DashConstants.KeyStore.LayoutKey, DocumentContext) as DocumentFieldModelController)?.Data;
                    if (layoutDocController == null)
                        layoutDocController = DocumentController;
                    var posFieldModelController =
                        layoutDocController.GetDereferencedField(DashConstants.KeyStore.PositionFieldKey, DocumentContext) as
                            PointFieldModelController;
                    posFieldModelController.Data = value.Translate;
                    var scaleCenterFieldModelController =
                        layoutDocController.GetDereferencedField(DashConstants.KeyStore.ScaleCenterFieldKey, DocumentContext) as
                            PointFieldModelController;
                    scaleCenterFieldModelController.Data = value.ScaleCenter;
                    var scaleAmountFieldModelController =
                        layoutDocController.GetDereferencedField(DashConstants.KeyStore.ScaleAmountFieldKey, DocumentContext) as
                            PointFieldModelController;
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
        public string DebugName = "";

        public Brush BorderBrush
        {
            get { return _borderBrush; }
            set { SetProperty(ref _borderBrush, value); }
        }

        private bool _isDetailedUserInterfaceVisible = true;

        public bool IsDetailedUserInterfaceVisible
        {
            get { return _isDetailedUserInterfaceVisible; }
            set { SetProperty(ref _isDetailedUserInterfaceVisible, value); }
        }

        private bool _isMoveable = true;

        public bool IsMoveable
        {
            get { return _isMoveable; }
            set { SetProperty(ref _isMoveable, value); }
        }

        private Visibility _docMenuVisibility;
        public Visibility DocMenuVisibility
        {
            get { return _docMenuVisibility; }
            set { SetProperty(ref _docMenuVisibility, value); }
        }

        private GridLength _menuColumnWidth;
        public GridLength MenuColumnWidth
        {
            get { return _menuColumnWidth; }
            set { SetProperty(ref _menuColumnWidth, value); }
        }

        public Context DocumentContext = null;
        

        // == CONSTRUCTORS == 
        public DocumentViewModel() { }

        public DocumentViewModel(DocumentController documentController, Context context = null)
        {
            DocumentContext = context ?? new Context();
            DocumentController = documentController;
            BackgroundBrush = new SolidColorBrush(Colors.White);
            BorderBrush = new SolidColorBrush(Colors.LightGray);


            // FIELD FETCHERS
            // overrides defaults with document fields if layout-relevant fields are set
            var layoutDocController = (DocumentController.GetDereferencedField(DashConstants.KeyStore.LayoutKey, context) as DocumentFieldModelController)?.Data;
            if (layoutDocController == null)
                layoutDocController = documentController;

            SetupTransformGroupFieldModelControllers(layoutDocController, context);
            SetupSizeFieldModelControllers(layoutDocController, context);

            // set icon via field 
            var iconFieldModelController = DocumentController.GetDereferencedField(DashConstants.KeyStore.IconTypeFieldKey, context) as NumberFieldModelController;
            if (iconFieldModelController == null) {
                iconFieldModelController = new NumberFieldModelController((int)IconTypeEnum.Document);
                DocumentController.SetField(DashConstants.KeyStore.IconTypeFieldKey, iconFieldModelController, true);
            } else Debug.WriteLine("we did it right: " + iconFieldModelController.Data);
            iconType = (IconTypeEnum)iconFieldModelController.Data;
            iconFieldModelController.FieldModelUpdated += IconFieldModelController_FieldModelUpdatedEvent;

            var documentFieldModelController = DocumentController.GetDereferencedField(DashConstants.KeyStore.LayoutKey, context) as DocumentFieldModelController;
            DataBindingSource.Add(documentController.DocumentModel);
            Content = documentController.makeViewUI(context);
            documentController.DocumentFieldUpdated += delegate(DocumentController.DocumentFieldUpdatedEventArgs args)
            {
                if (args.Reference.FieldKey.Equals(DashConstants.KeyStore.LayoutKey))
                {
                    Content = DocumentController.makeViewUI(context);
                }
            };
        }

        private void SetupTransformGroupFieldModelControllers(DocumentController layoutDocController, Context context)
        {
            var posFieldModelController = layoutDocController.GetDereferencedField(DashConstants.KeyStore.PositionFieldKey, context) as PointFieldModelController;
            if (posFieldModelController == null)
            {
                posFieldModelController = new PointFieldModelController(0, 0);
                layoutDocController.SetField(DashConstants.KeyStore.PositionFieldKey, posFieldModelController, true);
            }
            var scaleCenterFieldModelController = layoutDocController.GetDereferencedField(DashConstants.KeyStore.ScaleCenterFieldKey, context) as PointFieldModelController;
            if (scaleCenterFieldModelController == null)
            {
                scaleCenterFieldModelController = new PointFieldModelController(0, 0);
                layoutDocController.SetField(DashConstants.KeyStore.ScaleCenterFieldKey, scaleCenterFieldModelController, true);
            }
            var scaleAmountFieldModelController = layoutDocController.GetDereferencedField(DashConstants.KeyStore.ScaleAmountFieldKey, context) as PointFieldModelController;
            if (scaleAmountFieldModelController == null)
            {
                scaleAmountFieldModelController = new PointFieldModelController(1, 1);
                layoutDocController.SetField(DashConstants.KeyStore.ScaleAmountFieldKey, scaleAmountFieldModelController, true);
            }
            GroupTransform = new TransformGroupData(posFieldModelController.Data, scaleCenterFieldModelController.Data, scaleAmountFieldModelController.Data);
            posFieldModelController.FieldModelUpdated += PosFieldModelController_FieldModelUpdatedEvent;
            scaleCenterFieldModelController.FieldModelUpdated += ScaleCenterFieldModelController_FieldModelUpdatedEvent;
            scaleAmountFieldModelController.FieldModelUpdated += ScaleAmountFieldModelController_FieldModelUpdatedEvent;
        }

        private void SetupSizeFieldModelControllers(DocumentController layoutDocController, Context context)
        {
            var widthFieldModelController = layoutDocController.GetDereferencedField(DashConstants.KeyStore.WidthFieldKey, context) as NumberFieldModelController;
            if (widthFieldModelController == null)
            {
                widthFieldModelController = new NumberFieldModelController(double.NaN);
                layoutDocController.SetField(DashConstants.KeyStore.WidthFieldKey, widthFieldModelController, true);
            }
            Width = widthFieldModelController.Data;
            widthFieldModelController.FieldModelUpdated += WidthFieldModelController_FieldModelUpdatedEvent;


            var heightFieldModelController = layoutDocController.GetDereferencedField(DashConstants.KeyStore.HeightFieldKey, context) as NumberFieldModelController;
            if (heightFieldModelController == null)
            {
                heightFieldModelController = new NumberFieldModelController(double.NaN);
                layoutDocController.SetField(DashConstants.KeyStore.HeightFieldKey, heightFieldModelController, true);
            }
            Height = heightFieldModelController.Data;
            heightFieldModelController.FieldModelUpdated += HeightFieldModelController_FieldModelUpdatedEvent;
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

        private void HeightFieldModelController_FieldModelUpdatedEvent(FieldModelController sender)
        {
            var heightFieldModelController = sender as NumberFieldModelController;
            if (heightFieldModelController != null)
            {
                Height = heightFieldModelController.Data;
            }
        }

        private void WidthFieldModelController_FieldModelUpdatedEvent(FieldModelController sender)
        {
            var widthFieldModelController = sender as NumberFieldModelController;
            if (widthFieldModelController != null)
            {
                Width = widthFieldModelController.Data;
            }
        }
        
        private void IconFieldModelController_FieldModelUpdatedEvent(FieldModelController sender) {
            var iconFieldModelController = sender as NumberFieldModelController;
            if (iconFieldModelController != null) {
                iconType = (IconTypeEnum)iconFieldModelController.Data;
            }
        }

        private void PosFieldModelController_FieldModelUpdatedEvent(FieldModelController sender)
        {
            var posFieldModelController = sender as PointFieldModelController;
            if (posFieldModelController != null)
            {
                GroupTransform.Translate = posFieldModelController.Data;
            }
        }

        private void ScaleCenterFieldModelController_FieldModelUpdatedEvent(FieldModelController sender)
        {
            var scaleCenterFieldModelController = sender as PointFieldModelController;
            if (scaleCenterFieldModelController != null)
            {
                GroupTransform.ScaleCenter = scaleCenterFieldModelController.Data;
            }
        }

        private void ScaleAmountFieldModelController_FieldModelUpdatedEvent(FieldModelController sender)
        {
            var scaleAmountFieldModelController = sender as PointFieldModelController;
            if (scaleAmountFieldModelController != null)
            {
                GroupTransform.ScaleAmount = scaleAmountFieldModelController.Data;
            }
        }


        public void ToggleMenuVisibility()
        {
            if (DocMenuVisibility == Visibility.Collapsed)
            {
                OpenMenu();
            }
            else
            {
                CloseMenu();
            }
        }

        public void CloseMenu()
        {
            DocMenuVisibility = Visibility.Collapsed;
            MenuColumnWidth = new GridLength(0);
        }

        public void OpenMenu()
        {
            DocMenuVisibility = Visibility.Visible;
            MenuColumnWidth = new GridLength(50);
        }
    }
}
