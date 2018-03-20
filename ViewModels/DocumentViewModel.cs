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
using System.Globalization;
using Dash.Models.DragModels;
using static Dash.DocumentController;

namespace Dash
{
    public enum IconTypeEnum { Document, Collection, Api } // on super-collapse, what icon is displayed?

    public class DocumentViewModel : ViewModelBase, IDisposable
    {

        // == MEMBERS, GETTERS, SETTERS ==
        private double _groupMargin = 25;
        private TransformGroupData _normalGroupTransform = new TransformGroupData(new Point(), new Point(1, 1));
        private Brush _backgroundBrush = new SolidColorBrush(Colors.Transparent);
        private Brush _borderBrush;
        private IconTypeEnum iconType;
        private Visibility _docMenuVisibility = Visibility.Collapsed;
        private bool _menuOpen = false;
        public string DebugName = "";
        public DocumentController DocumentController { get; set; }
        public DocumentController DataDocument { get => DocumentController.GetDataDocument(); }

        bool _hasTitle;
        public bool HasTitle
        {
            get => _hasTitle;
            set => SetProperty(ref _hasTitle, value);
        }
        public void SetHasTitle(bool active)
        {
            if (active)
                HasTitle = active;
            else HasTitle = DataDocument.HasTitle && !Undecorated;
        }

        private bool _showLocalContext;

        public bool ShowLocalContext
        {
            get => _showLocalContext;
            set => SetProperty(ref _showLocalContext, value);
        }

        /// <summary>
        /// this sucks
        /// </summary>
        public double ActualHeight
        {
            get { return _actualHeight; }
        }

        /// <summary>
        /// this too
        /// </summary>
        public double ActualWidth
        {
            get { return _actualWidth; }
        }

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
            get => _menuOpen;
            set => SetProperty(ref _menuOpen, value);
        }

        public IconTypeEnum IconType => iconType;

        /// <summary>
        /// The cached Position of the document **during** a ManipulationControls interaction.
        /// When not interacting, use Position instead
        /// </summary>
        public Point InteractiveManipulationPosition;
        /// <summary>
        /// The cached Scale of the document **during** a ManipulationControls interaction.
        /// When not interacting, use Scale instead
        /// </summary>
        public Point InteractiveManipulationScale;

        /// <summary>
        /// The actual position of the document as written to the LayoutDocument  model
        /// </summary>
        public Point Position
        {
            get => LayoutDocument.GetDereferencedField<PointController>(KeyStore.PositionFieldKey, null)?.Data ?? new Point();
            set
            {
                var positionController =
                    LayoutDocument.GetDereferencedField<PointController>(KeyStore.PositionFieldKey, null);

                if (positionController != null && (Math.Abs(positionController.Data.X - value.X) > 0.05f || Math.Abs(positionController.Data.Y - value.Y) > 0.05f))
                {
                    positionController.Data = value;
                    OnPropertyChanged();
                }
                else LayoutDocument.SetField(KeyStore.PositionFieldKey, new PointController(value), true);
                InteractiveManipulationPosition = value;
            }
        }

        public double XPos
        {
            get => Position.X; // infinity causes problems with Bounds and other things expecting a number. double.PositiveInfinity;//Use inf so that sorting works reasonably
            set => Position = new Point(value, YPos);
        }

        public double YPos
        {
            get => Position.Y; // infinity causes problems with Bounds and other things expecting a number. 
            set => Position = new Point(XPos, value);
        }

        public double Width
        {
            get => LayoutDocument.GetDereferencedField<NumberController>(KeyStore.WidthFieldKey, null).Data;
            set
            {
                var widthController = LayoutDocument.GetDereferencedField<NumberController>(KeyStore.WidthFieldKey, null);
                if (widthController != null)
                {
                    if (Math.Abs(widthController.Data - value) > 0.05f)
                    {
                        widthController.Data = value;
                    }
                }
                else
                    LayoutDocument.SetField(KeyStore.WidthFieldKey, new NumberController(value), true);
            }
        }

        public double Height
        {
            get => LayoutDocument.GetDereferencedField<NumberController>(KeyStore.HeightFieldKey, null).Data;
            set
            {
                var heightController = LayoutDocument.GetDereferencedField<NumberController>(KeyStore.HeightFieldKey, null);
                if (heightController != null)
                {
                    if (Math.Abs(heightController.Data - value) > 0.05f)
                    {
                        heightController.Data = value;
                    }
                }
                else
                    LayoutDocument.SetField(KeyStore.HeightFieldKey, new NumberController(value), true);
            }
        }

        public Point Scale
        {
            get => LayoutDocument.GetDereferencedField<PointController>(KeyStore.ScaleAmountFieldKey, null)?.Data ?? new Point(1, 1);
            set
            {
                var scaleController =
                    LayoutDocument.GetDereferencedField<PointController>(KeyStore.ScaleAmountFieldKey, null);

                if (scaleController != null)
                {
                    scaleController.Data = value;
                    OnPropertyChanged();
                }
                else
                    LayoutDocument.SetField(KeyStore.ScaleAmountFieldKey, new PointController(value), true);
                InteractiveManipulationScale = value;
            }
        }

        public double GroupMargin
        {
            get => _groupMargin;
            set => SetProperty(ref _groupMargin, value);
        }

        protected bool Equals(DocumentViewModel other)
        {
            return Equals(LayoutDocument, other.LayoutDocument);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DocumentViewModel)obj);
        }

        public override string ToString()
        {
            return LayoutDocument.ToString();
        }

        public override int GetHashCode()
        {
            return LayoutDocument.GetHashCode();
        }

        public void UpdateActualSize(double actualwidth, double actualheight)      
        {
            _actualWidth = actualwidth;
            _actualHeight = actualheight;
            LayoutDocument.SetField(KeyStore.ActualWidthKey, new NumberController(_actualWidth), true);
            LayoutDocument.SetField(KeyStore.ActualHeightKey, new NumberController(_actualHeight), true);
        }

        public Rect Bounds => new TranslateTransform
        {
            X = XPos,
            Y = YPos,
        }.TransformBounds(new Rect(0, 0, _actualWidth * Scale.X, _actualHeight * Scale.Y));

        public Brush BackgroundBrush
        {
            get => _backgroundBrush;
            set
            {
                if (SetProperty(ref _backgroundBrush, value))
                {
                    if (value is SolidColorBrush scb)
                    {
                        LayoutDocument.SetField(KeyStore.BackgroundColorKey, new TextController(scb.Color.ToString()), true);
                    }
                }
            }
        }

        public Brush BorderBrush
        {
            get => _borderBrush;
            set => SetProperty(ref _borderBrush, value);
        }

        public Visibility DocMenuVisibility
        {
            get => _docMenuVisibility;
            set => SetProperty(ref _docMenuVisibility, value);
        }

        private FrameworkElement _content = null;
        public FrameworkElement Content
        {
            get
            {
                if (_content == null)
                {
                    _content = LayoutDocument.MakeViewUI(new Context(DataDocument));
                    //TODO: get mapping of key --> framework element
                }
                return _content;
            }
            set
            {
                _content = value;
            }
        }

        private double _actualWidth;
        private double _actualHeight;

        public void UpdateContent()
        {
            _content = null;
            OnPropertyChanged(nameof(Content));
        }


        public Context Context { get; set; }

        public bool Undecorated { get; set; }

        bool _decorationState = false;
        public bool DecorationState
        {
            get => _decorationState;
            set => SetProperty(ref _decorationState, value);
        }

        // == CONSTRUCTOR ==
        public DocumentViewModel(DocumentController documentController, Context context = null) : base()
        {
            DocumentController = documentController;//TODO This would be useful but doesn't work//.GetField(KeyStore.PositionFieldKey) == null ? documentController.GetViewCopy(null) :  documentController;


            InteractiveManipulationPosition = Position; // update the interaction caches in case they are accessed outside of a Manipulation
            InteractiveManipulationScale = Scale;

            BorderBrush = new SolidColorBrush(Colors.LightGray);
            SetUpSmallIcon();
            OnActiveLayoutChanged(context);

            DocumentController.GetDataDocument(context).AddFieldUpdatedListener(KeyStore.TitleKey, titleChanged);
            titleChanged(null, null, null);


            var hexColor = LayoutDocument.GetDereferencedField<TextController>(KeyStore.BackgroundColorKey, null)?.Data;
            if (hexColor != null)
            {
                byte a = byte.Parse(hexColor.Substring(1, 2), NumberStyles.HexNumber);
                byte r = byte.Parse(hexColor.Substring(3, 2), NumberStyles.HexNumber);
                byte g = byte.Parse(hexColor.Substring(5, 2), NumberStyles.HexNumber);
                byte b = byte.Parse(hexColor.Substring(7, 2), NumberStyles.HexNumber);
                _backgroundBrush = new SolidColorBrush(Color.FromArgb(a, r, g, b));
            }
        }

        void titleChanged(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            SetHasTitle(!Undecorated && DocumentController.GetDataDocument(null).HasTitle);
        }

        private void SetUpSmallIcon()
        {
            var iconFieldModelController = LayoutDocument.GetDereferencedField<NumberController>(KeyStore.IconTypeFieldKey, null);
            if (iconFieldModelController == null)
            {
                iconFieldModelController = new NumberController((int)(IconTypeEnum.Document));
                LayoutDocument.SetField(KeyStore.IconTypeFieldKey, iconFieldModelController, true);
            }
            iconType = (IconTypeEnum)iconFieldModelController.Data;
            iconFieldModelController.FieldModelUpdated += IconFieldModelController_FieldModelUpdatedEvent;
        }


        private void RemoveListenersFromLayout(DocumentController oldLayoutDoc)
        {
            if (oldLayoutDoc != null)
            {
                oldLayoutDoc.GetHeightField().FieldModelUpdated -= HeightFieldModelController_FieldModelUpdatedEvent;
                oldLayoutDoc.GetWidthField().FieldModelUpdated -= WidthFieldModelController_FieldModelUpdatedEvent;
            }
        }

        private void RemoveControllerListeners()
        {
            LayoutDocument.RemoveFieldUpdatedListener(KeyStore.ActiveLayoutKey, DocumentController_LayoutUpdated);
            DocumentController.RemoveFieldUpdatedListener(KeyStore.ActiveLayoutKey, DocumentController_LayoutUpdated);
            var icon = LayoutDocument.GetDereferencedField<NumberController>(KeyStore.IconTypeFieldKey, null);
            icon.FieldModelUpdated -= IconFieldModelController_FieldModelUpdatedEvent;
        }

        public DocumentController LayoutDocument
        {
            get
            {
                return DocumentController?.GetActiveLayout() ?? DocumentController;
            }
        }

        DocumentController _lastLayout = null;
        public void OnActiveLayoutChanged(Context context, bool force = false)
        {
            if (!force && _lastLayout?.Equals(LayoutDocument) == true)
                return;
            if (_lastLayout != null)
            {
                _lastLayout.FieldModelUpdated -= LayoutDocument_FieldModelUpdated;
                RemoveListenersFromLayout(_lastLayout);
            }
            _lastLayout = LayoutDocument;
            
            LayoutDocument.FieldModelUpdated += LayoutDocument_FieldModelUpdated;
            LayoutDocument.AddFieldUpdatedListener(KeyStore.ActiveLayoutKey, DocumentController_LayoutUpdated);
            DocumentController.AddFieldUpdatedListener(KeyStore.ActiveLayoutKey, DocumentController_LayoutUpdated);
            var newContext = new Context(context);  // bcz: not sure if this is right, but it avoids layout cycles with collections
            newContext.AddDocumentContext(LayoutDocument);
            Context = newContext;

            UpdateContent();
        }
        private void DocumentController_LayoutUpdated(FieldControllerBase fieldControllerBase, FieldUpdatedEventArgs fieldUpdatedEventArgs, Context context)
        {
            OnActiveLayoutChanged(new Context(LayoutDocument));
        }

        private void LayoutDocument_FieldModelUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            var dargs = args as DocumentFieldUpdatedEventArgs;
            var fargs = (dargs?.FieldArgs as DocumentFieldUpdatedEventArgs)?.Reference.FieldKey;
            if (dargs != null && dargs.Reference.FieldKey.Equals(KeyStore.ActiveLayoutKey)) {
                if (dargs.NewValue.Equals(_lastLayout) == false)
                {
                    OnActiveLayoutChanged(context);
                }
            }
            else if (dargs != null && fargs != null && (fargs.Equals(KeyStore.DataKey) == true))
                OnActiveLayoutChanged(context);
            else if (dargs != null && dargs.Reference.FieldKey.Equals(KeyStore.DataKey) == true && dargs.FieldArgs == null)
                OnActiveLayoutChanged(context, true);
        }

        // == FIELD UPDATED EVENT HANDLERS == 
        // these update the view model's variables when the document's corresponding fields update

        private void HeightFieldModelController_FieldModelUpdatedEvent(FieldControllerBase sender, FieldUpdatedEventArgs args, Context c)
        {
            var heightFieldModelController = sender as NumberController;
            if (heightFieldModelController != null)
            {
                Height = heightFieldModelController.Data;
            }
        }

        private void WidthFieldModelController_FieldModelUpdatedEvent(FieldControllerBase sender, FieldUpdatedEventArgs args, Context c)
        {
            var widthFieldModelController = sender as NumberController;
            if (widthFieldModelController != null)
            {
                Width = widthFieldModelController.Data;
            }
        }

        private void IconFieldModelController_FieldModelUpdatedEvent(FieldControllerBase sender, FieldUpdatedEventArgs args, Context c)
        {
            var iconFieldModelController = sender as NumberController;
            if (iconFieldModelController != null)
            {
                iconType = (IconTypeEnum)iconFieldModelController.Data;
            }
        }

        public void OnCollectionSelectedChanged(bool isCollectionSelected)
        {

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
