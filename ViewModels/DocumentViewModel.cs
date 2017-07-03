using Dash;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using DashShared;
using Microsoft.Extensions.DependencyInjection;
using Dash.Models;
using Windows.Foundation;
using System.Diagnostics;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Shapes;

namespace Dash
{
    public class DocumentViewModel : ViewModelBase
    {
        // == MEMBERS, GETTERS, SETTERS ==
        private ManipulationModes _manipulationMode;
        private double _height;
        private double _width;
        private double _x, _y;
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
        public double X
        {
            get { return _x; }
            set
            {
                SetProperty(ref _x, value);
                var posField = DocumentController.GetField(DashConstants.KeyStore.PositionFieldKey);
                if (posField != null)
                {
                    var pfm = ContentController.GetController<PointFieldModelController>(posField.GetId()).PointFieldModel;
                    pfm.Data = new Point(value, pfm.Data.Y);
                }
            }
        }

        public double Y
        {
            get { return _y; }
            set
            {
                SetProperty(ref _y, value);
                var posField = DocumentController.GetField(DashConstants.KeyStore.PositionFieldKey);
                if (posField != null)
                {
                    var pfm = ContentController.GetController<PointFieldModelController>(posField.GetId()).PointFieldModel;
                    pfm.Data = new Point(pfm.Data.X, value);
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

            // set the X and Y position if the fields for those positions exist
       
            var posFieldModelController = DocumentController.GetField(DashConstants.KeyStore.PositionFieldKey);
            if (posFieldModelController != null)
            {
                X = (posFieldModelController as PointFieldModelController).Data.X;
                Y = (posFieldModelController as PointFieldModelController).Data.Y;
            }
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
