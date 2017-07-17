using System.Collections.ObjectModel;
using System.Diagnostics;
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
                    Context c = new Context(DocumentController);
                    var layoutDocController = (DocumentController.GetDereferencedField(DashConstants.KeyStore.ActiveLayoutKey, c) as DocumentFieldModelController)?.Data;

                    if (layoutDocController == null)
                        layoutDocController = DocumentController;

                    var widthFieldModelController =
                        layoutDocController.GetDereferencedField(DashConstants.KeyStore.WidthFieldKey, c) as
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
                    Context c = new Context(DocumentController);
                    var layoutDocController = (DocumentController.GetDereferencedField(DashConstants.KeyStore.ActiveLayoutKey, c) as DocumentFieldModelController)?.Data;

                    if (layoutDocController == null)
                        layoutDocController = DocumentController;

                    var heightFieldModelController =
                        layoutDocController.GetDereferencedField(DashConstants.KeyStore.HeightFieldKey, c) as
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
                    Context c = new Context(DocumentController);
                    var layoutDocController = (DocumentController.GetDereferencedField(DashConstants.KeyStore.ActiveLayoutKey, c) as DocumentFieldModelController)?.Data;
                    if (layoutDocController == null)
                        layoutDocController = DocumentController;

                    var posFieldModelController =
                        layoutDocController.GetDereferencedField(DashConstants.KeyStore.PositionFieldKey, c) as
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


        public DocumentViewModel(DocumentController documentController, Context context = null)
        {
            DocumentController = documentController;
            BackgroundBrush = new SolidColorBrush(Colors.White);
            BorderBrush = new SolidColorBrush(Colors.LightGray);


            var c = context == null ? new Context() : new Context(context);
            c.AddDocumentContext(DocumentController);
            // FIELD FETCHERS
            // overrides defaults with document fields if layout-relevant fields are set
            var layoutDocController = (DocumentController.GetDereferencedField(DashConstants.KeyStore.ActiveLayoutKey, c) as DocumentFieldModelController)?.Data;

            if (layoutDocController == null)
                layoutDocController = documentController;
            var posFieldModelController = layoutDocController.GetDereferencedField(DashConstants.KeyStore.PositionFieldKey, c) as PointFieldModelController;
            if (posFieldModelController == null)
            {
                posFieldModelController = new PointFieldModelController(0, 0);
                layoutDocController.SetField(DashConstants.KeyStore.PositionFieldKey, posFieldModelController, true);
            }
            Position = posFieldModelController.Data;
            posFieldModelController.FieldModelUpdated += PosFieldModelController_FieldModelUpdatedEvent;

            var widthFieldModelController = layoutDocController.GetDereferencedField(DashConstants.KeyStore.WidthFieldKey, c) as NumberFieldModelController;
            if (widthFieldModelController == null)
            {
                widthFieldModelController = new NumberFieldModelController(double.NaN);
                layoutDocController.SetField(DashConstants.KeyStore.WidthFieldKey, widthFieldModelController, true);
            }
            Width = widthFieldModelController.Data;
            widthFieldModelController.FieldModelUpdated += WidthFieldModelController_FieldModelUpdatedEvent;


            var heightFieldModelController = layoutDocController.GetDereferencedField(DashConstants.KeyStore.HeightFieldKey, c) as NumberFieldModelController;
            if (heightFieldModelController == null)
            {
                heightFieldModelController = new NumberFieldModelController(double.NaN);
                layoutDocController.SetField(DashConstants.KeyStore.HeightFieldKey, heightFieldModelController, true);
            }
            Height = heightFieldModelController.Data;
            heightFieldModelController.FieldModelUpdated += HeightFieldModelController_FieldModelUpdatedEvent; ;

            // set icon via field 
            var iconFieldModelController = DocumentController.GetDereferencedField(DashConstants.KeyStore.IconTypeFieldKey, c) as NumberFieldModelController;
            if (iconFieldModelController == null) {
                iconFieldModelController = new NumberFieldModelController((int)IconTypeEnum.Document);
                DocumentController.SetField(DashConstants.KeyStore.IconTypeFieldKey, iconFieldModelController, true);
            } else Debug.WriteLine("we did it right: " + iconFieldModelController.Data);
            iconType = (IconTypeEnum)iconFieldModelController.Data;
            iconFieldModelController.FieldModelUpdated += IconFieldModelController_FieldModelUpdatedEvent;


            var documentFieldModelController = DocumentController.GetDereferencedField(DashConstants.KeyStore.ActiveLayoutKey, c) as DocumentFieldModelController;

            DataBindingSource.Add(documentController.DocumentModel);

            Content = documentController.MakeViewUI(c);

            documentController.DocumentFieldUpdated += delegate(DocumentController.DocumentFieldUpdatedEventArgs args)
            {
                if (args.Reference.DocId.Equals(DocumentController.GetId()) && args.Reference.FieldKey.Equals(DashConstants.KeyStore.ActiveLayoutKey))
                {
                    Context c2 = new Context(context);
                    c2.AddDocumentContext(DocumentController);
                    Content = DocumentController.MakeViewUI(c2);
                }
            };

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
        
        private void IconFieldModelController_FieldModelUpdatedEvent(FieldModelController sender, Context c) {
            var iconFieldModelController = sender as NumberFieldModelController;
            if (iconFieldModelController != null) {
                iconType = (IconTypeEnum)iconFieldModelController.Data;
            }
        }

        private void PosFieldModelController_FieldModelUpdatedEvent(FieldModelController sender, Context c)
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
    }
}
