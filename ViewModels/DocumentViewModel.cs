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
        DocumentController _lastLayout = null;
        TransformGroupData _normalGroupTransform = new TransformGroupData(new Point(), new Point(1, 1));
        Brush _backgroundBrush = new SolidColorBrush(Colors.Transparent);
        Brush _borderBrush;
        bool _hasTitle;
        bool _showLocalContext;
        bool _decorationState = false;
        FrameworkElement _content = null;
        
        // == CONSTRUCTOR ==
        public DocumentViewModel(DocumentController documentController, Context context = null) : base()
        {
            DocumentController = documentController;//TODO This would be useful but doesn't work//.GetField(KeyStore.PositionFieldKey) == null ? documentController.GetViewCopy(null) :  documentController;

            DocumentController.AddFieldUpdatedListener(KeyStore.ActiveLayoutKey, DocumentController_LayoutUpdated);

            InteractiveManipulationPosition = Position; // update the interaction caches in case they are accessed outside of a Manipulation
            InteractiveManipulationScale = Scale;

            BorderBrush = new SolidColorBrush(Colors.LightGray);
            if (IconTypeController == null)
            {
                LayoutDocument.SetField(KeyStore.IconTypeFieldKey, new NumberController((int)(IconTypeEnum.Document)), true);
            }
            OnActiveLayoutChanged(context);

            DataDocument.AddFieldUpdatedListener(KeyStore.TitleKey, titleChanged);
            titleChanged(null, null, null);
            DataDocument.AddFieldUpdatedListener(KeyStore.BackgroundColorKey, backgroundChanged);
            backgroundChanged(null, null, null);
        }

        public DocumentController DocumentController { get; set; }
        public DocumentController DataDocument => DocumentController.GetDataDocument();
        public DocumentController LayoutDocument => DocumentController?.GetActiveLayout() ?? DocumentController;
        public NumberController IconTypeController => LayoutDocument.GetDereferencedField<NumberController>(KeyStore.IconTypeFieldKey, null);

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
        public bool ShowLocalContext
        {
            get => _showLocalContext;
            set => SetProperty(ref _showLocalContext, value);
        }

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
        public Rect Bounds => new TranslateTransform { X = XPos, Y = YPos}.TransformBounds(new Rect(0, 0, ActualWidth * Scale.X, ActualHeight * Scale.Y));
        public double ActualHeight { get; private set; }
        public double ActualWidth { get; private set; }
        public void UpdateActualSize(double actualwidth, double actualheight)
        {
            ActualWidth = actualwidth;
            ActualHeight = actualheight;
            LayoutDocument.SetField(KeyStore.ActualWidthKey, new NumberController(ActualWidth), true);
            LayoutDocument.SetField(KeyStore.ActualHeightKey, new NumberController(ActualHeight), true);
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
        public FrameworkElement Content
        {
            get => _content ?? (_content = LayoutDocument.MakeViewUI(new Context(DataDocument))); 
            private set  {
                _content = value; // content will be recomputed when someone accesses Content
                OnPropertyChanged(nameof(Content)); // let everyone know that _content has changed
            }
        }
        public bool Undecorated { get; set; }
        public bool DecorationState
        {
            get => _decorationState;
            set => SetProperty(ref _decorationState, value);
        }


        // == FIELD UPDATED EVENT HANDLERS == 
        // these update the view model's variables when the document's corresponding fields update
        void titleChanged(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            SetHasTitle(!Undecorated && DataDocument.HasTitle);
        }
        void backgroundChanged(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            var hexColor = LayoutDocument.GetDereferencedField<TextController>(KeyStore.BackgroundColorKey, null)?.Data;
            if (hexColor != null)
            {
                byte a = byte.Parse(hexColor.Substring(1, 2), NumberStyles.HexNumber);
                byte r = byte.Parse(hexColor.Substring(3, 2), NumberStyles.HexNumber);
                byte g = byte.Parse(hexColor.Substring(5, 2), NumberStyles.HexNumber);
                byte b = byte.Parse(hexColor.Substring(7, 2), NumberStyles.HexNumber);
                BackgroundBrush = new SolidColorBrush(Color.FromArgb(a, r, g, b));
            }
        }
        void OnActiveLayoutChanged(Context context, bool force = false)
        {
            if (force || _lastLayout?.Equals(LayoutDocument) != true)
            {
                if (_lastLayout != null)
                    _lastLayout.FieldModelUpdated -= LayoutDocument_FieldModelUpdated;
                _lastLayout = LayoutDocument;
                LayoutDocument.FieldModelUpdated += LayoutDocument_FieldModelUpdated;
                
                Content = null; // forces layout to be recomputed by listeners who will access Content
            }
        }
        void DocumentController_LayoutUpdated(FieldControllerBase fieldControllerBase, FieldUpdatedEventArgs fieldUpdatedEventArgs, Context context)
        {
            var dargs = fieldUpdatedEventArgs as DocumentFieldUpdatedEventArgs;
            var fargs = (dargs?.FieldArgs as DocumentFieldUpdatedEventArgs)?.Reference.FieldKey;
            if (dargs?.Reference.FieldKey == KeyStore.ActiveLayoutKey && fargs == null)
            {
                if (_lastLayout != LayoutDocument)
                {
                    var curActive = DocumentController.GetField(KeyStore.ActiveLayoutKey, true) as DocumentController;
                    if (curActive == null)
                    {
                        curActive = LayoutDocument.GetViewInstance(_lastLayout.GetDereferencedField<PointController>(KeyStore.PositionFieldKey, new Context(DocumentController)).Data);
                        curActive.SetField(KeyStore.DocumentContextKey, DataDocument, true);
                        DocumentController.SetField(KeyStore.ActiveLayoutKey, curActive, true);
                        return;
                    }
                }
            }
            OnActiveLayoutChanged(new Context(DocumentController));
        }
        void LayoutDocument_FieldModelUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
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
        public void Dispose()
        {
            DataDocument.RemoveFieldUpdatedListener(KeyStore.TitleKey, titleChanged);
            DocumentController.RemoveFieldUpdatedListener(KeyStore.ActiveLayoutKey, DocumentController_LayoutUpdated);
            if (_lastLayout != null)
                _lastLayout.FieldModelUpdated -= LayoutDocument_FieldModelUpdated;
        }
    }
}
