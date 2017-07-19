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
using static Dash.CourtesyDocuments.CourtesyDocument;
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
                    var context = new Context(DocumentController);
                    var layoutDocController = (DocumentController.GetDereferencedField(DashConstants.KeyStore.ActiveLayoutKey, context) as DocumentFieldModelController)?.Data;

                    if (layoutDocController == null)
                        layoutDocController = DocumentController;

                    var widthFieldModelController =
                        layoutDocController.GetDereferencedField(DashConstants.KeyStore.WidthFieldKey, context) as
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
                    var context = new Context(DocumentController);
                    var layoutDocController = (DocumentController.GetDereferencedField(DashConstants.KeyStore.ActiveLayoutKey, context) as DocumentFieldModelController)?.Data;

                    if (layoutDocController == null)
                        layoutDocController = DocumentController;
                    var heightFieldModelController =
                        layoutDocController.GetDereferencedField(DashConstants.KeyStore.HeightFieldKey, context) as
                            NumberFieldModelController;
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
                    var layoutDocController = (DocumentController.GetDereferencedField(DashConstants.KeyStore.ActiveLayoutKey , context) as DocumentFieldModelController)?.Data;

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
                    scaleCenterFieldModelController.Data = value.ScaleCenter;
                    // set scale amount
                    var scaleAmountFieldModelController =
                        layoutDocController.GetDereferencedField(DashConstants.KeyStore.ScaleAmountFieldKey, context) as
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

        // == CONSTRUCTORS == 
        public DocumentViewModel() { }


        public DocumentViewModel(DocumentController documentController)
        {
            DocumentController = documentController;
            BackgroundBrush = new SolidColorBrush(Colors.White);
            BorderBrush = new SolidColorBrush(Colors.LightGray);

            DataBindingSource.Add(documentController.DocumentModel);

            Content = documentController.MakeViewUI(new Context(DocumentController));

            SetUpSmallIcon();

            documentController.DocumentFieldUpdated += DocumentController_DocumentFieldUpdated;
            OnActiveLayoutChanged();
        }

        private void SetUpSmallIcon()
        {
            var iconFieldModelController =
                DocumentController.GetDereferencedField(DashConstants.KeyStore.IconTypeFieldKey, new Context(DocumentController)) as NumberFieldModelController;
            if (iconFieldModelController == null)
            {
                iconFieldModelController = new NumberFieldModelController((int) (IconTypeEnum.Document));
                DocumentController.SetField(DashConstants.KeyStore.IconTypeFieldKey, iconFieldModelController, true);
            }

            iconType = (IconTypeEnum) iconFieldModelController.Data;
            iconFieldModelController.FieldModelUpdated += IconFieldModelController_FieldModelUpdatedEvent;
        }

        private void DocumentController_DocumentFieldUpdated(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            if (args.Reference.FieldKey.Equals(DashConstants.KeyStore.ActiveLayoutKey))
            {
                OnActiveLayoutChanged();
            }
        }

        private void OnActiveLayoutChanged()
        {
            Content = DocumentController.MakeViewUI(new Context(DocumentController));
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
                GroupTransform = new TransformGroupData(posFieldModelController.Data,
                    scaleCenterFieldModelController.Data, scaleAmountFieldModelController.Data);
                posFieldModelController.FieldModelUpdated += PosFieldModelController_FieldModelUpdatedEvent;
                scaleCenterFieldModelController.FieldModelUpdated +=
                    ScaleCenterFieldModelController_FieldModelUpdatedEvent;
                scaleAmountFieldModelController.FieldModelUpdated +=
                    ScaleAmountFieldModelController_FieldModelUpdatedEvent;
            }
            
        }

        private void ListenToWidthField(DocumentController docController)
        {
            var widthField = docController.GetWidthField();
            widthField.FieldModelUpdated += WidthFieldModelController_FieldModelUpdatedEvent;
            Width = widthField.Data;
        }

        private void ListenToHeightField(DocumentController docController)
        {
            var heightField = docController.GetHeightField();
            heightField.FieldModelUpdated += HeightFieldModelController_FieldModelUpdatedEvent;
            Height = heightField.Data;
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

        public DocumentController Copy()
        {
            var copy = DocumentController.GetCopy();
            var layoutField = copy.GetActiveLayout().Data;
            var layoutCopy = layoutField.GetCopy();
            copy.SetActiveLayout(layoutCopy, true);
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

            del.SetActiveLayout(delLayout, true);

            return del;
        }
    }
}
