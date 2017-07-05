using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using DashShared;
using Windows.Foundation;
using Windows.UI.Xaml.Data;

namespace Dash
{
    public class DocumentViewModel : ViewModelBase
    {
        // == MEMBERS, GETTERS, SETTERS ==
        private ManipulationModes _manipulationMode;
        private double _height;
        private double _width;
        private Point _pos;
        private Brush _backgroundBrush;
        private Brush _borderBrush;
        public bool DoubleTapEnabled = true;
        public DocumentController DocumentController;

        public delegate void OnLayoutChangedHandler(DocumentViewModel sender);

        public event OnLayoutChangedHandler OnLayoutChanged;

        public ObservableCollection<DocumentModel> DataBindingSource { get; set; } =
            new ObservableCollection<DocumentModel>();

        public double Width
        {
            get { return _width; }
            set
            {
                SetProperty(ref _width, value);
                var widthField = DocumentController.GetField(DashConstants.KeyStore.WidthFieldKey);
                if (widthField != null)
                {
                    var pfm = ContentController.GetController<NumberFieldModelController>(widthField.GetId()).NumberFieldModel;
                    pfm.Data = value;
                }
            }
        }

        public double Height
        {
            get { return _height; }
            set
            {
                SetProperty(ref _height, value);
                var heightField = DocumentController.GetField(DashConstants.KeyStore.HeightFieldKey);
                if (heightField != null) {
                    var pfm = ContentController.GetController<NumberFieldModelController>(heightField.GetId()).NumberFieldModel;
                    pfm.Data = value;
                }
            }
        }

        public Point Position
        {
            get { return _pos; }
            set {
                if (SetProperty(ref _pos, value))
                {
                    var posFieldModelController =
                        DocumentController.GetField(DashConstants.KeyStore.PositionFieldKey) as
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

        // == CONSTRUCTORS == 
        public DocumentViewModel() { }

        public DocumentViewModel(DocumentController documentController)
        {
            DocumentController = documentController;
            BackgroundBrush = new SolidColorBrush(Colors.White);
            BorderBrush = new SolidColorBrush(Color.FromArgb(50, 34, 34, 34));
       
            var posFieldModelController = DocumentController.GetField(DashConstants.KeyStore.PositionFieldKey) as PointFieldModelController;
            if (posFieldModelController == null)
            {
                var pointFieldModel = new PointFieldModel(0,0);
                posFieldModelController = new PointFieldModelController(pointFieldModel);
                ContentController.AddController(posFieldModelController);
                ContentController.AddModel(pointFieldModel);
                DocumentController.SetField(DashConstants.KeyStore.PositionFieldKey, posFieldModelController, true);
            }
            posFieldModelController.FieldModelUpdatedEvent += PosFieldModelController_FieldModelUpdatedEvent;

            var widthFieldModelController = DocumentController.GetField(DashConstants.KeyStore.WidthFieldKey);
            if (widthFieldModelController != null)
            {
                Width = (widthFieldModelController as NumberFieldModelController).Data;
            }
            var heightFieldModelController = DocumentController.GetField(DashConstants.KeyStore.HeightFieldKey);
            if (heightFieldModelController != null)
            {
                Height = (heightFieldModelController as NumberFieldModelController).Data;
            }

            var documentFieldModelController = DocumentController.GetField(DashConstants.KeyStore.LayoutKey) as DocumentFieldModelController;
            if (documentFieldModelController != null)
                documentFieldModelController.Data.OnLayoutChanged += DocumentController_OnLayoutChanged;

            DataBindingSource.Add(documentController.DocumentModel);
        }


        private void PosFieldModelController_FieldModelUpdatedEvent(FieldModelController sender)
        {
            var posFieldModelController = sender as PointFieldModelController;
            if (posFieldModelController != null)
            {
                Position = posFieldModelController.Data;
            }
        }

        private void DocumentController_OnLayoutChanged(DocumentController sender)
        {
            OnLayoutChanged?.Invoke(this);
        }

        // == METHODS ==
        /// <summary>
        /// Generates a list of UIElements by making FieldViewModels of a document;s
        /// given fields.
        /// </summary>
        /// TODO: rename this to create ui elements
        /// <returns>List of all UIElements generated</returns>
        public virtual List<FrameworkElement> GetUiElements(Rect bounds)
        {
            return DocumentController.MakeViewUI();
        }
    }
}
