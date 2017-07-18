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

namespace Dash
{
    public enum IconTypeEnum { Document, Collection, Api } // on super-collapse, what icon is displayed?

    public class DocumentViewModel : ViewModelBase
    {
        // == MEMBERS, GETTERS, SETTERS ==
        private ManipulationModes _manipulationMode;
        private double _height;
        private double _width;
        private Point _pos;
        private Brush _backgroundBrush;
        private Brush _borderBrush;
        private IconTypeEnum iconType;
        public bool DoubleTapEnabled = true;
        public DocumentController DocumentController;
        
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

                    var layoutDocController = (DocumentController.GetDereferencedField(DashConstants.KeyStore.ActiveLayoutKey) as DocumentFieldModelController)?.Data;

                    if (layoutDocController == null)
                        layoutDocController = DocumentController;

                    var widthFieldModelController =
                        layoutDocController.GetDereferencedField(DashConstants.KeyStore.WidthFieldKey) as
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
                    var layoutDocController = (DocumentController.GetDereferencedField(DashConstants.KeyStore.ActiveLayoutKey) as DocumentFieldModelController)?.Data;

                    if (layoutDocController == null)
                        layoutDocController = DocumentController;

                    var heightFieldModelController =
                        layoutDocController.GetDereferencedField(DashConstants.KeyStore.HeightFieldKey) as
                            NumberFieldModelController;
                    heightFieldModelController.Data = value;
                }
            }
        }

        public Point Position
        {
            get { return _pos; }
            set {
                if (SetProperty(ref _pos, value))
                {
                    var layoutDocController = (DocumentController.GetDereferencedField(DashConstants.KeyStore.ActiveLayoutKey) as DocumentFieldModelController)?.Data;

                    if (layoutDocController == null)
                        layoutDocController = DocumentController;

                    var posFieldModelController =
                        layoutDocController.GetDereferencedField(DashConstants.KeyStore.PositionFieldKey) as
                            PointFieldModelController;
                    posFieldModelController.Data = value;
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

            Content = documentController.MakeViewUI();

            documentController.DocumentFieldUpdated += DocumentController_DocumentFieldUpdated;
            OnActiveLayoutChanged();
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
            Content = DocumentController.MakeViewUI();
            ListenToHeightField(DocumentController);
            ListenToWidthField(DocumentController);
            ListenToPositionField(DocumentController);
        }

        private void ListenToPositionField(DocumentController docController)
        {
            var positionField = docController.GetPositionField();
            positionField.FieldModelUpdated += PosFieldModelController_FieldModelUpdatedEvent;
            Position = positionField.Data;
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
                Position = posFieldModelController.Data;
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
