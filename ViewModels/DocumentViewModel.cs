using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
                    var layoutDocController = (DocumentController.GetDereferencedField(DashConstants.KeyStore.LayoutKey, DocContextList) as DocumentFieldModelController)?.Data;
                    if (layoutDocController == null)
                        layoutDocController = DocumentController;

                    var widthFieldModelController =
                        layoutDocController.GetDereferencedField(DashConstants.KeyStore.WidthFieldKey, DocContextList) as
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
                    var layoutDocController = (DocumentController.GetDereferencedField(DashConstants.KeyStore.LayoutKey, DocContextList) as DocumentFieldModelController)?.Data;
                    if (layoutDocController == null)
                        layoutDocController = DocumentController;

                    var heightFieldModelController =
                        layoutDocController.GetDereferencedField(DashConstants.KeyStore.HeightFieldKey, DocContextList) as
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
                    var layoutDocController = (DocumentController.GetDereferencedField(DashConstants.KeyStore.LayoutKey, DocContextList) as DocumentFieldModelController)?.Data;
                    if (layoutDocController == null)
                        layoutDocController = DocumentController;

                    var posFieldModelController =
                        layoutDocController.GetDereferencedField(DashConstants.KeyStore.PositionFieldKey, DocContextList) as
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

        public List<DocumentController> DocContextList = null;
        // == CONSTRUCTORS == 
        public DocumentViewModel() { }

        public DocumentViewModel(DocumentController documentController, IEnumerable<DocumentController> docContextList = null)
        {
            DocContextList = docContextList == null ? null : new List<DocumentController>(docContextList);
            DocumentController = documentController;
            BackgroundBrush = new SolidColorBrush(Colors.White);
            BorderBrush = new SolidColorBrush(Color.FromArgb(50, 34, 34, 34));

            var layoutDocController = (DocumentController.GetDereferencedField(DashConstants.KeyStore.LayoutKey, docContextList) as DocumentFieldModelController)?.Data;
            if (layoutDocController == null)
                layoutDocController = documentController;
            var posFieldModelController = layoutDocController.GetDereferencedField(DashConstants.KeyStore.PositionFieldKey, docContextList) as PointFieldModelController;
            if (posFieldModelController == null)
            {
                posFieldModelController = new PointFieldModelController(0, 0);
                layoutDocController.SetField(DashConstants.KeyStore.PositionFieldKey, posFieldModelController, true);
            }
            Position = posFieldModelController.Data;
            posFieldModelController.FieldModelUpdatedEvent += PosFieldModelController_FieldModelUpdatedEvent;

            var widthFieldModelController = layoutDocController.GetDereferencedField(DashConstants.KeyStore.WidthFieldKey, docContextList) as NumberFieldModelController;
            if (widthFieldModelController == null)
            {
                widthFieldModelController = new NumberFieldModelController(double.NaN);
                layoutDocController.SetField(DashConstants.KeyStore.WidthFieldKey, widthFieldModelController, true);
            }
            Width = widthFieldModelController.Data;
            widthFieldModelController.FieldModelUpdatedEvent += WidthFieldModelController_FieldModelUpdatedEvent;


            var heightFieldModelController = layoutDocController.GetDereferencedField(DashConstants.KeyStore.HeightFieldKey, docContextList) as NumberFieldModelController;
            if (heightFieldModelController == null)
            {
                heightFieldModelController = new NumberFieldModelController(double.NaN);
                layoutDocController.SetField(DashConstants.KeyStore.HeightFieldKey, heightFieldModelController, true);
            }
            Height = heightFieldModelController.Data;
            heightFieldModelController.FieldModelUpdatedEvent += HeightFieldModelController_FieldModelUpdatedEvent;

            DataBindingSource.Add(documentController.DocumentModel);

            Content = documentController.makeViewUI(docContextList);
            documentController.DocumentFieldUpdated += delegate(FieldModelController value, FieldModelController newValue, ReferenceFieldModelController reference)
            {
                if (reference.FieldKey.Equals(DashConstants.KeyStore.LayoutKey))
                {
                    Content = DocumentController.makeViewUI(DocContextList);
                }
            };
        }

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

        private void PosFieldModelController_FieldModelUpdatedEvent(FieldModelController sender)
        {
            var posFieldModelController = sender as PointFieldModelController;
            if (posFieldModelController != null)
            {
                Position = posFieldModelController.Data;
            }
        }
    }
}
