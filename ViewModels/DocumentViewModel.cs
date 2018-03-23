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
        bool _showLocalContext;
        bool _decorationState = false;
        FrameworkElement _content = null;
        
        // == CONSTRUCTOR ==
        public DocumentViewModel(DocumentController documentController, Context context = null) : base()
        {
            DocumentController = documentController;
            DocumentController.AddFieldUpdatedListener(KeyStore.ActiveLayoutKey, DocumentController_ActiveLayoutChanged);
            LayoutDocument.AddFieldUpdatedListener(KeyStore.DataKey, LayoutDocument_DataChanged);
            _lastLayout = LayoutDocument;

            InteractiveManipulationPosition = Position; // update the interaction caches in case they are accessed outside of a Manipulation
            InteractiveManipulationScale = Scale;
            
            if (IconTypeController == null)
            {
                LayoutDocument.SetField(KeyStore.IconTypeFieldKey, new NumberController((int)(IconTypeEnum.Document)), true);
            }
        }

        public DocumentController DocumentController { get; set; }
        public DocumentController DataDocument => DocumentController.GetDataDocument();
        public DocumentController LayoutDocument => DocumentController?.GetActiveLayout() ?? DocumentController;
        public NumberController IconTypeController => LayoutDocument.GetDereferencedField<NumberController>(KeyStore.IconTypeFieldKey, null);
        
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

        public Brush BackgroundBrush
        {
            get
            {
                var backgroundStr = LayoutDocument.GetDereferencedField<TextController>(KeyStore.BackgroundColorKey, null)?.Data;

                if (!string.IsNullOrEmpty(backgroundStr) && backgroundStr.Length == 9)
                {
                    byte a = byte.Parse(backgroundStr.Substring(1, 2), NumberStyles.HexNumber);
                    byte r = byte.Parse(backgroundStr.Substring(3, 2), NumberStyles.HexNumber);
                    byte g = byte.Parse(backgroundStr.Substring(5, 2), NumberStyles.HexNumber);
                    byte b = byte.Parse(backgroundStr.Substring(7, 2), NumberStyles.HexNumber);
                    return new SolidColorBrush(Color.FromArgb(a, r, g, b));
                }
                return new SolidColorBrush(Colors.Transparent);
            }
            set => LayoutDocument.SetField<TextController,string>(KeyStore.BackgroundColorKey, ((value as SolidColorBrush)?.Color ?? Colors.Transparent).ToString(), true);
        }
        /// <summary>
        /// The actual position of the document as written to the LayoutDocument  model
        /// </summary>
        public Point Position
        {
            get => LayoutDocument.GetDereferencedField<PointController>(KeyStore.PositionFieldKey, null)?.Data ?? new Point();
            set => LayoutDocument.SetField<PointController, Point>(KeyStore.PositionFieldKey, InteractiveManipulationPosition = value, true);
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
            set => LayoutDocument.SetField<NumberController, double>(KeyStore.WidthFieldKey, value, true);
        }
        public double Height
        {
            get => LayoutDocument.GetDereferencedField<NumberController>(KeyStore.HeightFieldKey, null).Data;
            set => LayoutDocument.SetField<NumberController, double>(KeyStore.HeightFieldKey, value, true);
        }
        public Point Scale
        {
            get => LayoutDocument.GetDereferencedField<PointController>(KeyStore.ScaleAmountFieldKey, null)?.Data ?? new Point(1, 1);
            set => LayoutDocument.SetField<PointController, Point>(KeyStore.ScaleAmountFieldKey, InteractiveManipulationScale = value, true);
        }
        public Rect Bounds => new TranslateTransform { X = XPos, Y = YPos}.TransformBounds(new Rect(0, 0, ActualWidth * Scale.X, ActualHeight * Scale.Y));
        public double ActualHeight { get; private set; }
        public double ActualWidth { get; private set; }
        public void UpdateActualSize(double actualwidth, double actualheight)
        {
            ActualWidth = actualwidth;
            ActualHeight = actualheight;
            LayoutDocument.SetField<NumberController,double>(KeyStore.ActualWidthKey, ActualWidth, true);
            LayoutDocument.SetField<NumberController,double>(KeyStore.ActualHeightKey, ActualHeight, true);
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
        /// <summary>
        /// Called whenever the contents (Data field) of the active Layout document have been changed.
        /// This causes the layout to be re-created.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <param name="context"></param>
        void LayoutDocument_DataChanged(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            if (new Context(LayoutDocument).IsCompatibleWith(context)) // filter out callbacks on prototype from delegate
                // some updates to LayoutDocuments are not bound to the UI.  In these cases, we need to rebuild the UI.
                //   bcz: need some better mechanism than this....
                if (LayoutDocument.DocumentType.Equals(StackLayout.DocumentType) ||
                    LayoutDocument.DocumentType.Equals(GridLayout.DocumentType))
                    Content = null; // forces layout to be recomputed by listeners who will access Content
        }
        /// <summary>
        /// Called when the ActiveLayout field of the Layout document has changed (or a field on the ActiveLayout).
        /// Such a change requires that the layout view be re-created.  
        /// If the layout was changed on a prototype and the instance doesn't mask the field, then this instance makes 
        /// a delegate of the prototype's activeLayout field. Otherwise, the instance would share the position, 
        /// size, etc of the prototype and changes to the instance would affect the prototype.
        /// </summary>
        /// <param name="fieldControllerBase"></param>
        /// <param name="fieldUpdatedEventArgs"></param>
        /// <param name="context"></param>
        void DocumentController_ActiveLayoutChanged(FieldControllerBase fieldControllerBase, FieldUpdatedEventArgs fieldUpdatedEventArgs, Context context)
        {
            var dargs = fieldUpdatedEventArgs as DocumentFieldUpdatedEventArgs;
            var fargs = (dargs?.FieldArgs as DocumentFieldUpdatedEventArgs)?.Reference.FieldKey;
            // test that the ActiveLayout field changed and not one of the fields on the ActiveLayout.
            // if a field of the activelayout changed, we ignore that here since it should update the layout directly
            // through bindings.
            if (fargs == null && _lastLayout != LayoutDocument)
            {
                var curActive = DocumentController.GetField(KeyStore.ActiveLayoutKey, true) as DocumentController;
                if (curActive == null)
                {
                    curActive = LayoutDocument.GetViewInstance(_lastLayout.GetDereferencedField<PointController>(KeyStore.PositionFieldKey, new Context(DocumentController)).Data);
                    curActive.SetField(KeyStore.DocumentContextKey, DataDocument, true);
                    DocumentController.SetField(KeyStore.ActiveLayoutKey, curActive, true);
                }
                _lastLayout.RemoveFieldUpdatedListener(KeyStore.DataKey, LayoutDocument_DataChanged);
                _lastLayout = LayoutDocument;
                LayoutDocument.AddFieldUpdatedListener(KeyStore.DataKey, LayoutDocument_DataChanged);
                LayoutDocument_DataChanged(null, null, new Context(DocumentController));
            }
        }
        public void Dispose()
        {
            DocumentController.RemoveFieldUpdatedListener(KeyStore.ActiveLayoutKey, DocumentController_ActiveLayoutChanged);
            _lastLayout?.RemoveFieldUpdatedListener(KeyStore.DataKey, LayoutDocument_DataChanged);
        }
    }
}
