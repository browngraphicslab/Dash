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

    public class DocumentViewModel : BaseSelectionElementViewModel
    {

        public delegate void OnContentChangedHandler(DocumentViewModel sender, FrameworkElement content);
        public event OnContentChangedHandler OnContentChanged;

        // == MEMBERS, GETTERS, SETTERS ==
        private ManipulationModes _manipulationMode;
        private double _height;
        private double _width;
        private TransformGroupData _normalGroupTransform;
        private TransformGroupData _interfaceBuilderGroupTransform;
        private Brush _backgroundBrush;
        private Brush _borderBrush;
        private IconTypeEnum iconType;
        private Visibility _docMenuVisibility;
        private bool _menuOpen = false;
        private bool _isDetailedUserInterfaceVisible = true;
        private bool _isMoveable = true;
        private FrameworkElement _content;
        private WidthAndMenuOpenWrapper _widthBinding;
        public string DebugName = "";
        public bool DoubleTapEnabled = true;
        public DocumentController DocumentController;
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
            set { SetProperty(ref _menuOpen, value); }
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
                    var layoutDocController = (DocumentController.GetDereferencedField(KeyStore.ActiveLayoutKey, context) as DocumentFieldModelController)?.Data;

                    if (layoutDocController == null)
                        layoutDocController = DocumentController;

                    var widthFieldModelController =
                        layoutDocController.GetDereferencedField(KeyStore.WidthFieldKey, context) as
                            NumberFieldModelController;

                    if (widthFieldModelController != null)
                    {
                        widthFieldModelController.Data = value;
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
                    var layoutDocController = (DocumentController.GetDereferencedField(KeyStore.ActiveLayoutKey, context) as DocumentFieldModelController)?.Data;

                    if (layoutDocController == null)
                        layoutDocController = DocumentController;
                    var heightFieldModelController =
                        layoutDocController.GetDereferencedField(KeyStore.HeightFieldKey, context) as
                            NumberFieldModelController;
                    if (heightFieldModelController != null)
                        heightFieldModelController.Data = value;
                }
            }
        }

        public TransformGroupData GroupTransform
        {
            get { return IsInInterfaceBuilder ? _interfaceBuilderGroupTransform : _normalGroupTransform; }
            set
            {
                if (IsInInterfaceBuilder)
                {
                    SetProperty(ref _interfaceBuilderGroupTransform, value);
                    return;
                }

                if (SetProperty(ref _normalGroupTransform, value))
                {
                    // get layout
                    var context = new Context(DocumentController);
                    var layoutDocController = (DocumentController.GetDereferencedField(KeyStore.ActiveLayoutKey, context) as DocumentFieldModelController)?.Data;

                    if (layoutDocController == null)
                        layoutDocController = DocumentController;
                    // set position
                    var posFieldModelController =
                        layoutDocController.GetDereferencedField(KeyStore.PositionFieldKey, context) as
                            PointFieldModelController;
                    posFieldModelController.Data = value.Translate;
                    // set scale center
                    var scaleCenterFieldModelController =
                        layoutDocController.GetDereferencedField(KeyStore.ScaleCenterFieldKey, context) as
                            PointFieldModelController;
                    if (scaleCenterFieldModelController != null)
                        scaleCenterFieldModelController.Data = value.ScaleCenter;
                    // set scale amount
                    var scaleAmountFieldModelController =
                        layoutDocController.GetDereferencedField(KeyStore.ScaleAmountFieldKey, context) as
                            PointFieldModelController;
                    if (scaleAmountFieldModelController != null)
                        scaleAmountFieldModelController.Data = value.ScaleAmount;
                }
            }
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

        public Visibility DocMenuVisibility
        {
            get { return _docMenuVisibility; }
            set { SetProperty(ref _docMenuVisibility, value); }
        }

        public readonly bool IsInInterfaceBuilder;


        public DocumentViewModel(DocumentController documentController, bool isInInterfaceBuilder = false, Context context = null) : base(isInInterfaceBuilder)
        {
            IsInInterfaceBuilder = isInInterfaceBuilder;
            DocumentController = documentController;
            BackgroundBrush = new SolidColorBrush(Colors.White);
            BorderBrush = new SolidColorBrush(Colors.LightGray);
            DataBindingSource.Add(documentController.DocumentModel);

            SetUpSmallIcon();
            _interfaceBuilderGroupTransform = new TransformGroupData(new Point(), new Point(), new Point(1, 1));
            documentController.AddFieldUpdatedListener(KeyStore.ActiveLayoutKey, DocumentController_DocumentFieldUpdated);
            var newContext = new Context(context);  // bcz: not sure if this is right, but it avoids layout cycles with collections
            newContext.AddDocumentContext(DocumentController);
            OnActiveLayoutChanged(newContext);
        }

        private void SetUpSmallIcon()
        {
            var iconFieldModelController =
                DocumentController.GetDereferencedField(KeyStore.IconTypeFieldKey, new Context(DocumentController)) as NumberFieldModelController;
            if (iconFieldModelController == null)
            {
                iconFieldModelController = new NumberFieldModelController((int)(IconTypeEnum.Document));
                DocumentController.SetField(KeyStore.IconTypeFieldKey, iconFieldModelController, true);
            }
            iconType = (IconTypeEnum)iconFieldModelController.Data;
            iconFieldModelController.FieldModelUpdated += IconFieldModelController_FieldModelUpdatedEvent;
        }

        private void DocumentController_DocumentFieldUpdated(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            Debug.Assert(args.Reference.FieldKey.Equals(KeyStore.ActiveLayoutKey));
            Debug.WriteLine(args.Action);
            OnActiveLayoutChanged(new Dash.Context(DocumentController));
        }
        private void OnActiveLayoutChanged(Context context)
        {
            Content = DocumentController.MakeViewUI(context, IsInInterfaceBuilder);
            OnContentChanged?.Invoke(this, Content);

            ListenToHeightField(DocumentController);
            ListenToWidthField(DocumentController);

            if (!IsInInterfaceBuilder)
            {
                ListenToTransformGroupField(DocumentController);
            }
        }

        private void ListenToTransformGroupField(DocumentController docController)
        {
            var posFieldModelController = docController.GetPositionField();
            var activeLayout = docController.GetActiveLayout()?.Data;
            if (activeLayout != null)
            {
                var scaleCenterFieldModelController =
                    activeLayout.GetDereferencedField(KeyStore.ScaleCenterFieldKey,
                        new Context(DocumentController)) as PointFieldModelController;
                var scaleAmountFieldModelController =
                    activeLayout.GetDereferencedField(KeyStore.ScaleAmountFieldKey,
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
            var translate = new TranslateTransform { X = 125 - actualWidth / 2, Y = 125 - actualHeight / 2 };
            var scale = new ScaleTransform { CenterX = translate.X + actualWidth / 2, CenterY = translate.Y + actualHeight / 2, ScaleX = 220.0 / max, ScaleY = 220.0 / max };
            var group = new TransformGroup();
            group.Children.Add(translate);
            group.Children.Add(scale);
        }

        // == FIELD UPDATED EVENT HANDLERS == 
        // these update the view model's variables when the document's corresponding fields update

        private void HeightFieldModelController_FieldModelUpdatedEvent(FieldModelController sender, FieldUpdatedEventArgs args, Context c)
        {
            var heightFieldModelController = sender as NumberFieldModelController;
            if (heightFieldModelController != null)
            {
                Height = heightFieldModelController.Data;
            }
        }

        private void WidthFieldModelController_FieldModelUpdatedEvent(FieldModelController sender, FieldUpdatedEventArgs args, Context c)
        {
            var widthFieldModelController = sender as NumberFieldModelController;
            if (widthFieldModelController != null)
            {
                Width = widthFieldModelController.Data;
            }
        }

        private void IconFieldModelController_FieldModelUpdatedEvent(FieldModelController sender, FieldUpdatedEventArgs args, Context c)
        {
            var iconFieldModelController = sender as NumberFieldModelController;
            if (iconFieldModelController != null)
            {
                iconType = (IconTypeEnum)iconFieldModelController.Data;
            }
        }

        private void PosFieldModelController_FieldModelUpdatedEvent(FieldModelController sender, FieldUpdatedEventArgs args, Context c)
        {
            var posFieldModelController = sender as PointFieldModelController;
            if (posFieldModelController != null)
            {
                GroupTransform = new TransformGroupData(posFieldModelController.Data, GroupTransform.ScaleCenter, GroupTransform.ScaleAmount);
            }
        }

        private void ScaleCenterFieldModelController_FieldModelUpdatedEvent(FieldModelController sender, FieldUpdatedEventArgs args, Context context)
        {
            var scaleCenterFieldModelController = sender as PointFieldModelController;
            if (scaleCenterFieldModelController != null)
            {
                GroupTransform = new TransformGroupData(GroupTransform.Translate, scaleCenterFieldModelController.Data, GroupTransform.ScaleAmount);
            }
        }

        private void ScaleAmountFieldModelController_FieldModelUpdatedEvent(FieldModelController sender, FieldUpdatedEventArgs args, Context context)
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
            MenuOpen = false;
        }

        public void OpenMenu()
        {
            DocMenuVisibility = Visibility.Visible;
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
            delLayout.SetField(KeyStore.PositionFieldKey,
                new PointFieldModelController(new Point(oldPosition.X + 15, oldPosition.Y + 15)),
                true);
            del.SetActiveLayout(delLayout, forceMask: true, addToLayoutList: false);
            return del;
        }
    }
}
