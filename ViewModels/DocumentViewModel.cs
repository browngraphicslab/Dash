using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Windows.ApplicationModel.DataTransfer;
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

    public class DocumentViewModel : BaseSelectionElementViewModel, IDisposable
    {

        // == MEMBERS, GETTERS, SETTERS ==
        private double _height;
        private double _width;
        private TransformGroupData _normalGroupTransform = new TransformGroupData(new Point(), new Point(), new Point(1, 1));
        private TransformGroupData _interfaceBuilderGroupTransform;
        private Brush _backgroundBrush;
        private Brush _borderBrush;
        private IconTypeEnum iconType;
        private Visibility _docMenuVisibility = Visibility.Collapsed;
        private bool _menuOpen = false;
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

        public double Width
        {
            get { return _width; }
            set
            {
                if (SetProperty(ref _width, value))
                {
                    var widthFieldModelController =
                        LayoutDocument.GetDereferencedField(KeyStore.WidthFieldKey, new Context(DocumentController)) as NumberFieldModelController;

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
                    var heightFieldModelController =
                        LayoutDocument.GetDereferencedField(KeyStore.HeightFieldKey, new Context(DocumentController)) as
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
                    var context = new Context(DocumentController);
                    // set position
                    var posFieldModelController =
                        LayoutDocument.GetDereferencedField(KeyStore.PositionFieldKey, context) as
                            PointFieldModelController;
                    //if(!PointEquals(posFieldModelController.Data, _normalGroupTransform.Translate))
                    posFieldModelController.Data = value.Translate;
                    // set scale center
                    var scaleCenterFieldModelController =
                        LayoutDocument.GetDereferencedField(KeyStore.ScaleCenterFieldKey, context) as
                            PointFieldModelController;
                    if (scaleCenterFieldModelController != null)
                        scaleCenterFieldModelController.Data = value.ScaleCenter;
                    // set scale amount
                    var scaleAmountFieldModelController =
                        LayoutDocument.GetDereferencedField(KeyStore.ScaleAmountFieldKey, context) as
                            PointFieldModelController;
                    if (scaleAmountFieldModelController != null)
                        scaleAmountFieldModelController.Data = value.ScaleAmount;
                }
            }
        }

        public static bool PointEquals(Point a, Point b)
        {
            var ax = double.IsNaN(a.X);
            var ay = double.IsNaN(a.Y);
            var bx = double.IsNaN(b.X);
            var by = double.IsNaN(b.Y);
            if (!ax && !ay) return a == b;
            if (ax == ay) return bx && by;
            return ax ? bx : by;
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

        private FrameworkElement _content = null;
        public FrameworkElement Content
        {
            get
            {
                if (_content == null)
                {
                    _content = DocumentController.MakeViewUI(new Context(LayoutDocument), IsInInterfaceBuilder);
                }
                return _content;
            }
        }


        string _displayName = "<doc>";

        public string DisplayName
        {
            get { return _displayName; }
            set {
                if (SetProperty<string>(ref _displayName, value)) {
                    OnPropertyChanged("DisplayName");
                }
            }
        }

        public void UpdateContent()
        {
            _content = null;
            OnPropertyChanged(nameof(Content));
            this.DocumentController.DocumentFieldUpdated += DocumentController_DocumentFieldUpdated1;
        }

        private void DocumentController_DocumentFieldUpdated1(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            var context = args.Context;
            var primKeys = sender.GetDereferencedField(KeyStore.PrimaryKeyKey, context)?.GetValue(context) as List<FieldModelController>;

            if (primKeys != null && primKeys.Select((k) => (k as TextFieldModelController).Data).Contains(args.Reference.FieldKey.KeyModel.Id))
            {
                updateDisplayName();
            }
        }

        private void updateDisplayName()
        {
            var keyList = DocumentController.GetDereferencedField(KeyStore.PrimaryKeyKey, Context);
            var keys = keyList as ListFieldModelController<TextFieldModelController>;
            if (keys != null)
            {
                var docString = "";
                foreach (var k in keys.Data)
                {
                    var keyField = DocumentController.GetDereferencedField(new KeyController((k as TextFieldModelController).Data), Context);
                    if (keyField is TextFieldModelController)
                        docString += (keyField as TextFieldModelController).Data + " ";
                }
                DisplayName = docString.TrimEnd(' ');
            }
        }

        public Context Context { get; set; }
        public DocumentViewModel(DocumentController documentController, bool isInInterfaceBuilder = false, Context context = null) : base(isInInterfaceBuilder)
        {
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
            Context = newContext;
            updateDisplayName();
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
            OnActiveLayoutChanged(new Context(DocumentController));
            if (args.OldValue == null) return;
            var oldLayoutDoc = ((DocumentFieldModelController)args.OldValue).Data;
            RemoveListenersFromLayout(oldLayoutDoc);
        }

        private void RemoveListenersFromLayout(DocumentController oldLayoutDoc)
        {
            if (oldLayoutDoc == null) return;
            oldLayoutDoc.GetHeightField().FieldModelUpdated -= HeightFieldModelController_FieldModelUpdatedEvent;
            oldLayoutDoc.GetWidthField().FieldModelUpdated -= WidthFieldModelController_FieldModelUpdatedEvent;
            oldLayoutDoc.GetPositionField().FieldModelUpdated -= PosFieldModelController_FieldModelUpdatedEvent;
            oldLayoutDoc.GetScaleCenterField().FieldModelUpdated -= ScaleCenterFieldModelController_FieldModelUpdatedEvent;
            oldLayoutDoc.GetScaleAmountField().FieldModelUpdated -= ScaleAmountFieldModelController_FieldModelUpdatedEvent;
        }

        private void RemoveControllerListeners()
        {
            DocumentController.RemoveFieldUpdatedListener(KeyStore.ActiveLayoutKey, DocumentController_DocumentFieldUpdated);
            var icon = (NumberFieldModelController)DocumentController.GetDereferencedField(KeyStore.IconTypeFieldKey, new Context(DocumentController));
            icon.FieldModelUpdated -= IconFieldModelController_FieldModelUpdatedEvent;
        }
        
        public DocumentController LayoutDocument
        {
            get
            {
                var layoutDoc = (DocumentController?.GetDereferencedField(KeyStore.ActiveLayoutKey, new Context(DocumentController)) as DocumentFieldModelController)?.Data;
                return layoutDoc == null ? DocumentController : layoutDoc;
            }
        }
        public delegate void OnLayoutChangedHandler(DocumentViewModel sender, Context c);

        public event OnLayoutChangedHandler LayoutChanged;

        private void OnActiveLayoutChanged(Context context)
        {
            UpdateContent();
            LayoutChanged?.Invoke(this, context);

            ListenToHeightField(DocumentController);
            ListenToWidthField(DocumentController);

            if (!IsInInterfaceBuilder)
            {
                ListenToTransformGroupField(DocumentController);
            }
        }

        private void ListenToTransformGroupField(DocumentController docController)
        {
            var activeLayout = docController.GetActiveLayout()?.Data;
            if (activeLayout != null)
            {
                var scaleCenterFieldModelController = docController.GetScaleCenterField();
                var scaleAmountFieldModelController = docController.GetScaleAmountField();
                if (scaleCenterFieldModelController != null)
                {
                    var posFieldModelController = docController.GetPositionField();
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


        public void DocumentView_DragStarting(UIElement sender, DragStartingEventArgs args)
        {
            var docView = sender as DocumentView;
            DocumentView.DragDocumentView = docView;

            // create border around the doc being dragged
            if (docView != null)
                docView.OuterGrid.BorderThickness = new Thickness(5);

            args.Data.Properties.Add("DocumentControllerList", new List<DocumentController>(new DocumentController[] { DocumentController }));
                // different sources based on whether it's a collection or a document 
            docView.IsHitTestVisible = false; // so that collectionviews can't drop to anything within it 
        }

        public void Dispose()
        {
            if (LayoutDocument != null)
            {
                RemoveListenersFromLayout(LayoutDocument);
            }
            RemoveControllerListeners();
        }
    }
}
