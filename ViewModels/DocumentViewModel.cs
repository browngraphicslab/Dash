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

namespace Dash
{
    public enum IconTypeEnum { Document, Collection, Api } // on super-collapse, what icon is displayed?

    public class DocumentViewModel : ViewModelBase, IDisposable
    {

        // == MEMBERS, GETTERS, SETTERS ==
        private double _height;
        private double _width;
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
                }
            }
        }

        public double XPos
        {
            get => Position.X; // infinity causes problems with Bounds and other things expecting a number. double.PositiveInfinity;//Use inf so that sorting works reasonably
            set
            {
                var positionController =
                    LayoutDocument.GetDereferencedField<PointController>(KeyStore.PositionFieldKey, null);

                if (positionController != null && Math.Abs(positionController.Data.X - value) > 0.05f)
                {
                    positionController.Data = new Point(value, positionController.Data.Y);
                }
            }
        }

        public double YPos
        {
            get => Position.Y; // infinity causes problems with Bounds and other things expecting a number. 
            set
            {
                var positionController =  LayoutDocument.GetDereferencedField<PointController>(KeyStore.PositionFieldKey, null);

                if (positionController != null && Math.Abs(positionController.Data.Y - value) > 0.05f)
                {
                    positionController.Data = new Point(positionController.Data.X, value);
                }
            }
        }

        public double Width
        {
            get => _width;
            set
            {
                //Debug.Assert(double.IsNaN(value) == false);

                if (SetProperty(ref _width, value))
                {
                    var widthFieldModelController = LayoutDocument.GetDereferencedField<NumberController>(KeyStore.WidthFieldKey, null);
                    if (widthFieldModelController != null)
                    {
                        widthFieldModelController.Data = value;
                    }
                }
            }
        }

        public double Height
        {
            get => _height;
            set
            {
                //Debug.Assert(double.IsNaN(value) == false);

                if (SetProperty(ref _height, value))
                {
                    var heightFieldModelController = LayoutDocument.GetDereferencedField<NumberController>(KeyStore.HeightFieldKey, null);
                    if (heightFieldModelController != null)
                        heightFieldModelController.Data = value;
                }
            }
        }

        public Point Scale
        {
            get => LayoutDocument.GetDereferencedField<PointController>(KeyStore.ScaleAmountFieldKey, null)?.Data ?? new Point(1,1);
            set
            {
                var scaleController =
                    LayoutDocument.GetDereferencedField<PointController>(KeyStore.ScaleAmountFieldKey, null);

                if (scaleController != null)
                {
                    scaleController.Data = value;
                }
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
            return Equals((DocumentViewModel) obj);
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
            Y = YPos
        }.TransformBounds(new Rect(0, 0, _actualWidth * Scale.X, _actualHeight * Scale.Y));

        public void TransformDelta(TransformGroupData delta)
        {
            var currentTranslate = Position;
            var currentScaleAmount = Scale;

            var deltaTranslate = delta.Translate;
            var deltaScaleAmount = delta.ScaleAmount;
            var scaleAmount = new Point(currentScaleAmount.X * deltaScaleAmount.X, currentScaleAmount.Y * deltaScaleAmount.Y);
            var translate = new Point(currentTranslate.X + deltaTranslate.X , currentTranslate.Y + deltaTranslate.Y);

            Position = translate;
            Scale = scaleAmount;
        }

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
                    _content = LayoutDocument.MakeViewUI(null);
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

        private void DocumentController_LayoutUpdated(FieldControllerBase fieldControllerBase, FieldUpdatedEventArgs fieldUpdatedEventArgs, Context context)
        {
            if (fieldUpdatedEventArgs.Action != DocumentController.FieldUpdatedAction.Replace)
            {
                return;
            }
            var dargs = (DocumentController.DocumentFieldUpdatedEventArgs)fieldUpdatedEventArgs;
            Debug.Assert(dargs.Reference.FieldKey.Equals(KeyStore.ActiveLayoutKey));
            Debug.WriteLine(dargs.Action);
            OnActiveLayoutChanged(new Context(LayoutDocument));
            if (dargs.OldValue == null) return;
            var oldLayoutDoc = (DocumentController)dargs.OldValue;
            RemoveListenersFromLayout(oldLayoutDoc);
        }

        private void RemoveListenersFromLayout(DocumentController oldLayoutDoc)
        {
            if (oldLayoutDoc == null) return;
            oldLayoutDoc.GetHeightField().FieldModelUpdated -= HeightFieldModelController_FieldModelUpdatedEvent;
            oldLayoutDoc.GetWidthField().FieldModelUpdated -= WidthFieldModelController_FieldModelUpdatedEvent;
        }

        private void RemoveControllerListeners()
        {
            LayoutDocument.RemoveFieldUpdatedListener(KeyStore.ActiveLayoutKey, DocumentController_LayoutUpdated);
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
        public delegate void OnLayoutChangedHandler(DocumentViewModel sender, Context c);

        public event OnLayoutChangedHandler LayoutChanged;

        public void OnActiveLayoutChanged(Context context)
        {
            UpdateContent();
            LayoutChanged?.Invoke(this, context);

            ListenToHeightField();
            ListenToWidthField();

            LayoutDocument.AddFieldUpdatedListener(KeyStore.ActiveLayoutKey, DocumentController_LayoutUpdated);
            var newContext = new Context(context);  // bcz: not sure if this is right, but it avoids layout cycles with collections
            newContext.AddDocumentContext(LayoutDocument);
            Context = newContext;
        }

        private void ListenToWidthField()
        {
            var widthField = LayoutDocument.GetWidthField();
            if (widthField != null)
            {
                widthField.FieldModelUpdated += WidthFieldModelController_FieldModelUpdatedEvent;
                Width = widthField.Data;
            }
            else
                Width = double.NaN;
        }

        private void ListenToHeightField()
        {
            var heightField = LayoutDocument.GetHeightField();
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
