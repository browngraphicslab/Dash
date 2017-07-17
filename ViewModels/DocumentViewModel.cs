﻿using System.Collections.ObjectModel;
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
                    var layoutDocController = (DocumentController.GetDereferencedField(DashConstants.KeyStore.ActiveLayoutKey, DocumentContext) as DocumentFieldModelController)?.Data;

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
                    var layoutDocController = (DocumentController.GetDereferencedField(DashConstants.KeyStore.ActiveLayoutKey, DocumentContext) as DocumentFieldModelController)?.Data;

                    if (layoutDocController == null)
                        layoutDocController = DocumentController;

                    var heightFieldModelController =
                        layoutDocController.GetDereferencedField(DashConstants.KeyStore.HeightFieldKey, DocumentContext) as
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
                    var layoutDocController = (DocumentController.GetDereferencedField(DashConstants.KeyStore.ActiveLayoutKey, DocumentContext) as DocumentFieldModelController)?.Data;

                    if (layoutDocController == null)
                        layoutDocController = DocumentController;

                    var posFieldModelController =
                        layoutDocController.GetDereferencedField(DashConstants.KeyStore.PositionFieldKey, DocumentContext) as
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
            var layoutDocController = (DocumentController.GetDereferencedField(DashConstants.KeyStore.ActiveLayoutKey, context) as DocumentFieldModelController)?.Data;

            if (layoutDocController == null)
                layoutDocController = documentController;
            var posFieldModelController = layoutDocController.GetDereferencedField(DashConstants.KeyStore.PositionFieldKey, context) as PointFieldModelController;
            if (posFieldModelController == null)
            {
                posFieldModelController = new PointFieldModelController(0, 0);
                layoutDocController.SetField(DashConstants.KeyStore.PositionFieldKey, posFieldModelController, true);
            }
            Position = posFieldModelController.Data;
            posFieldModelController.FieldModelUpdated += PosFieldModelController_FieldModelUpdatedEvent;

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
            heightFieldModelController.FieldModelUpdated += HeightFieldModelController_FieldModelUpdatedEvent; ;

            // set icon via field 
            var iconFieldModelController = DocumentController.GetDereferencedField(DashConstants.KeyStore.IconTypeFieldKey, context) as NumberFieldModelController;
            if (iconFieldModelController == null) {
                iconFieldModelController = new NumberFieldModelController((int)IconTypeEnum.Document);
                DocumentController.SetField(DashConstants.KeyStore.IconTypeFieldKey, iconFieldModelController, true);
            } else Debug.WriteLine("we did it right: " + iconFieldModelController.Data);
            iconType = (IconTypeEnum)iconFieldModelController.Data;
            iconFieldModelController.FieldModelUpdated += IconFieldModelController_FieldModelUpdatedEvent;

            var documentFieldModelController = DocumentController.GetDereferencedField(DashConstants.KeyStore.ActiveLayoutKey, context) as DocumentFieldModelController;


            DataBindingSource.Add(documentController.DocumentModel);

            Content = documentController.makeViewUI(context);
            documentController.DocumentFieldUpdated += delegate(DocumentController.DocumentFieldUpdatedEventArgs args)
            {
                if (args.Reference.FieldKey.Equals(DashConstants.KeyStore.ActiveLayoutKey))
                {
                    Content = DocumentController.makeViewUI(context);
                }
            };
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

        public DocumentController GetCopy()
        {
            var copy = DocumentController.GetPrototype().MakeDelegate();
            var fields = new ObservableDictionary<Key, FieldModelController>();
            foreach (var kvp in DocumentController.EnumFields())
            {
                fields[kvp.Key] = kvp.Value;
            }
            copy.SetFields(fields, true);
            var documentFieldModelController = fields[DashConstants.KeyStore.ActiveLayoutKey] as DocumentFieldModelController;
            if (documentFieldModelController != null)
            {
                var layout = documentFieldModelController.Data;
                var pointFieldModelController = layout.GetField(DashConstants.KeyStore.PositionFieldKey) as PointFieldModelController;
                if (pointFieldModelController != null)
                {
                    var pos = pointFieldModelController.Data;
                    var layoutDel = layout.MakeDelegate();
                    layoutDel.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(pos.X + 15, pos.Y + 15), true);
                    copy.SetField(DashConstants.KeyStore.ActiveLayoutKey, new DocumentFieldModelController(layoutDel), true);
                }
            }
            
            return copy;
        }

        public DocumentController GetDelegate()
        {
            var del = DocumentController.MakeDelegate();
            var documentFieldModelController = DocumentController.GetField(DashConstants.KeyStore.ActiveLayoutKey) as DocumentFieldModelController;
            if (documentFieldModelController != null)
            {
                var layout = documentFieldModelController.Data;
                var pointFieldModelController = layout.GetField(DashConstants.KeyStore.PositionFieldKey) as PointFieldModelController;
                if (pointFieldModelController != null)
                {
                    var pos = pointFieldModelController.Data;
                    var layoutDel = layout.MakeDelegate();
                    layoutDel.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(pos.X + 15, pos.Y + 15), true);
                    //var docs =
                    //    (layoutDel.GetField(DashConstants.KeyStore.DataKey) as DocumentCollectionFieldModelController)
                    //    .GetDocuments();
                    //layoutDel.SetField(DashConstants.KeyStore.DataKey, new DocumentCollectionFieldModelController(docs), true); TODO should we copy the collection over or leave it as original? -GH
                    del.SetField(DashConstants.KeyStore.ActiveLayoutKey, new DocumentFieldModelController(layoutDel), true);
                }
            }
            return del;
        }
    }
}
