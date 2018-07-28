using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Dash.Annotations;
using Dash.Models.DragModels;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class DocumentDecorations : UserControl, INotifyPropertyChanged
    {
        private Visibility _visibilityState;
        private List<DocumentView> _selectedDocs;
        private bool _isMoving;

        public Visibility VisibilityState
        {
            get => _visibilityState;
            set
            {
                if (value != _visibilityState && !_visibilityLock)
                {
                    _visibilityState = value;
                    SetPositionAndSize();
                    OnPropertyChanged(nameof(VisibilityState));
                }
            }
        }

        private bool _visibilityLock;

        public List<DocumentView> SelectedDocs
        {
            get => _selectedDocs;
            set
            {
                foreach (var doc in _selectedDocs)
                {
                    doc.PointerEntered -= SelectedDocView_PointerEntered;
                    doc.PointerExited -= SelectedDocView_PointerExited;
                    doc.ViewModel?.DocumentController.RemoveFieldUpdatedListener(KeyStore.PositionFieldKey, DocumentController_OnPositionFieldUpdated);
                    doc.SizeChanged -= DocView_OnSizeChanged;
                    if ((doc.ViewModel?.DocumentController.DocumentType.Equals(RichTextBox.DocumentType) ?? false) && doc.GetFirstDescendantOfType<RichTextView>() != null)
                    {
                        doc.GetFirstDescendantOfType<RichTextView>().OnManipulatorHelperStarted -= ManipulatorStarted;
                        doc.GetFirstDescendantOfType<RichTextView>().OnManipulatorHelperCompleted -=
                            ManipulatorCompleted;
                    }
                    doc.ManipulationControls.OnManipulatorStarted -= ManipulatorStarted;
                    doc.ManipulationControls.OnManipulatorCompleted -= ManipulatorCompleted;
                    doc.FadeOutBegin -= DocView_OnDeleted;
                }

                _visibilityLock = false;
                foreach (var doc in value)
                {
                    if (doc.ViewModel.Undecorated)
                    {
                        _visibilityLock = true;
                        VisibilityState = Visibility.Collapsed;
                    }

                    doc.PointerEntered += SelectedDocView_PointerEntered;
                    doc.PointerExited += SelectedDocView_PointerExited;
                    doc.ViewModel?.DocumentController.AddFieldUpdatedListener(KeyStore.PositionFieldKey, DocumentController_OnPositionFieldUpdated);
                    doc.SizeChanged += DocView_OnSizeChanged;
                    if (doc.ViewModel.DocumentController.DocumentType.Equals(RichTextBox.DocumentType) && doc.GetFirstDescendantOfType<RichTextView>() != null)
                    {
                        doc.GetFirstDescendantOfType<RichTextView>().OnManipulatorHelperStarted += ManipulatorStarted;
                        doc.GetFirstDescendantOfType<RichTextView>().OnManipulatorHelperCompleted += OnManipulatorHelperCompleted;
                    }
                    doc.ManipulationControls.OnManipulatorStarted += ManipulatorStarted;
                    doc.ManipulationControls.OnManipulatorTranslatedOrScaled += ManipulatorMoving;
                    doc.ManipulationControls.OnManipulatorCompleted += ManipulatorCompleted;
                    doc.FadeOutBegin += DocView_OnDeleted;
                }

                _selectedDocs = value;
            }
        }

        private void OnManipulatorHelperCompleted()
        {
            if (!_isMoving)
            {
                VisibilityState = Visibility.Visible;
            }
        }

        private void ManipulatorMoving(TransformGroupData transformationDelta)
        {
            if (!_isMoving)
            {
                _isMoving = true;
            }
        }

        private void DocView_OnDeleted()
        {
            VisibilityState = Visibility.Collapsed;
        }

        private void ManipulatorCompleted()
        {
            VisibilityState = Visibility.Visible;
            _isMoving = false;
        }

        private void ManipulatorStarted()
        {
            VisibilityState = Visibility.Collapsed;
            _isMoving = true;
        }

        private void DocView_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetPositionAndSize();
        }

        private void DocumentController_OnPositionFieldUpdated(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args, Context context)
        {
            SetPositionAndSize();
        }

        public DocumentDecorations()
        {
            this.InitializeComponent();
            _visibilityState = Visibility.Collapsed;
            _selectedDocs = new List<DocumentView>();
            Loaded += DocumentDecorations_Loaded;
            Unloaded += DocumentDecorations_Unloaded;
        }

        private void DocumentDecorations_Unloaded(object sender, RoutedEventArgs e)
        {
            SelectionManager.SelectionChanged -= SelectionManager_SelectionChanged;
        }

        private void DocumentDecorations_Loaded(object sender, RoutedEventArgs e)
        {
            SelectionManager.SelectionChanged += SelectionManager_SelectionChanged;
        }

        private void SelectionManager_SelectionChanged(DocumentSelectionChangedEventArgs args)
        {
            SelectedDocs = SelectionManager.SelectedDocs.ToList();
            if (SelectedDocs.Count > 1)
            {
                xMultiSelectBorder.BorderThickness = new Thickness(2);
            }
            else
            {
                xMultiSelectBorder.BorderThickness = new Thickness(0);
            }

            SetPositionAndSize();
            SetTitleIcon();
            if (SelectedDocs.Any() && !this.IsRightBtnPressed())
            {
                VisibilityState = Visibility.Visible;
            }
            else
            {
                VisibilityState = Visibility.Collapsed;
            }
        }

        private void SetTitleIcon()
        {
            if (SelectedDocs.Count == 1)
            {
                var type = SelectedDocs.First().ViewModel?.DocumentController.GetDereferencedField(KeyStore.DataKey, null)?.TypeInfo;
                switch (type)
                {
                    case DashShared.TypeInfo.Image:
                        xTitleIcon.Text = Application.Current.Resources["ImageDocumentIcon"] as string;
                        break;
                    case DashShared.TypeInfo.Audio:
                        xTitleIcon.Text = Application.Current.Resources["AudioDocumentIcon"] as string;
                        break;
                    case DashShared.TypeInfo.Video:
                        xTitleIcon.Text = Application.Current.Resources["VideoDocumentIcon"] as string;
                        break;
                    case DashShared.TypeInfo.RichText:
                    case DashShared.TypeInfo.Text:
                        xTitleIcon.Text = Application.Current.Resources["TextIcon"] as string;
                        break;
                    case DashShared.TypeInfo.Document:
                        xTitleIcon.Text = Application.Current.Resources["DocumentPlainIcon"] as string;
                        break;
                    case DashShared.TypeInfo.Template:
                        xTitleIcon.Text = Application.Current.Resources["CollectionIcon"] as string;
                        break;
                    default:
                        xTitleIcon.Text = Application.Current.Resources["DefaultIcon"] as string;
                        break;
                }
            }
            else
            {
                xTitleIcon.Text = Application.Current.Resources["CollectionIcon"] as string;
            }

            //if (type.Equals(DashShared.TypeInfo.Template))
            //{
            //    xTitleIcon.Text = Application.Current.Resources["CollectionIcon"] as string;
            //}

            //if (_newpoint.X.Equals(0) && _newpoint.Y.Equals(0))
            //{
            //    xOperatorEllipseBorder.Margin = new Thickness(10, 0, 0, 0);
            //    xAnnotateEllipseBorder.Margin = new Thickness(10, AnnotateEllipseUnhighlight.Width + 5, 0, 0);
            //    xTemplateEditorEllipseBorder.Margin =
            //        new Thickness(10, 2 * (AnnotateEllipseUnhighlight.Width + 5), 0, 0);
            //}
            //else
            //{
            //    UpdateEllipses(_newpoint);
            //}
        }

        private void SetPositionAndSize()
        {
            var topLeft = new Point(double.PositiveInfinity, double.PositiveInfinity);
            var botRight = new Point(double.NegativeInfinity, double.NegativeInfinity);

            foreach (var doc in SelectedDocs)
            {
                var viewModelBounds = doc.TransformToVisual(MainPage.Instance.MainDocView).TransformBounds(new Rect(new Point(), new Size(doc.ActualWidth, doc.ActualHeight)));

                topLeft.X = Math.Min(viewModelBounds.Left - doc.xTargetBorder.BorderThickness.Left, topLeft.X);
                topLeft.Y = Math.Min(viewModelBounds.Top - doc.xTargetBorder.BorderThickness.Top, topLeft.Y);

                botRight.X = Math.Max(viewModelBounds.Right + doc.xTargetBorder.BorderThickness.Right, botRight.X);
                botRight.Y = Math.Max(viewModelBounds.Bottom + doc.xTargetBorder.BorderThickness.Bottom, botRight.Y);
            }

            if (double.IsPositiveInfinity(topLeft.X) || double.IsPositiveInfinity(topLeft.Y) || double.IsNegativeInfinity(botRight.X) || double.IsNegativeInfinity(botRight.Y))
            {
                return;
            }

            this.RenderTransform = new TranslateTransform
            {
                X = topLeft.X - xLeftColumn.Width.Value-3,
                Y = topLeft.Y
            };

            ContentColumn.Width = new GridLength(botRight.X - topLeft.X);
            xRow.Height = new GridLength(botRight.Y - topLeft.Y);
        }

        private void SelectedDocView_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            var doc = sender as DocumentView;
            if (doc.ViewModel != null)
            {
                if ((doc.StandardViewLevel.Equals(CollectionViewModel.StandardViewLevel.None) ||
                     doc.StandardViewLevel.Equals(CollectionViewModel.StandardViewLevel.Detail)) && doc.ViewModel != null &&
                    !e.GetCurrentPoint(doc).Properties.IsLeftButtonPressed && !e.GetCurrentPoint(doc).Properties.IsRightButtonPressed)
                {
                    VisibilityState = Visibility.Visible;
                }

                MainPage.Instance.HighlightTreeView(doc.ViewModel.DocumentController, true);
            }

            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 1);
            if (MainPage.Instance.MainDocView == doc && MainPage.Instance.MainDocView.ViewModel != null)
            {
                var level = MainPage.Instance.MainDocView.ViewModel.ViewLevel;
                if (level.Equals(CollectionViewModel.StandardViewLevel.Overview) ||
                    level.Equals(CollectionViewModel.StandardViewLevel.Region))
                    Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.SizeAll, 0);
                else if (level.Equals(CollectionViewModel.StandardViewLevel.Detail))
                    Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.IBeam, 0);
            }
        }

        private void SelectedDocView_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            var doc = sender as DocumentView;
            if (doc.StandardViewLevel.Equals(CollectionViewModel.StandardViewLevel.None) ||
                doc.StandardViewLevel.Equals(CollectionViewModel.StandardViewLevel.Detail))
            {
                if (e == null ||
                    (!e.GetCurrentPoint(doc).Properties.IsRightButtonPressed &&
                     !e.GetCurrentPoint(doc).Properties.IsLeftButtonPressed) && doc.ViewModel != null)
                    VisibilityState = Visibility.Collapsed;
            }

            MainPage.Instance.HighlightTreeView(doc.ViewModel.DocumentController, false);
            if (MainPage.Instance.MainDocView != doc)
            {
                var viewlevel = MainPage.Instance.MainDocView.ViewModel.ViewLevel;
                if (viewlevel.Equals(CollectionViewModel.StandardViewLevel.Overview) ||
                    viewlevel.Equals(CollectionViewModel.StandardViewLevel.Region))
                    Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.SizeAll, 0);
                else if (viewlevel.Equals(CollectionViewModel.StandardViewLevel.Detail))
                    Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.IBeam, 0);
            }
        }

        private void XAnnotateEllipseBorder_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            foreach (var doc in SelectedDocs)
            {
                var ann = new AnnotationManager(doc);
                ann.FollowRegion(doc.ViewModel.DocumentController, doc.GetAncestorsOfType<ILinkHandler>(), e.GetPosition(doc));
            }
        }

        private void AllEllipses_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            foreach (var doc in SelectedDocs)
            {
                doc.ManipulationMode = ManipulationModes.All;
            }
        }

        private void XAnnotateEllipseBorder_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            foreach (var doc in SelectedDocs)
            {
                doc.ManipulationMode = ManipulationModes.None;
            }
        }

        private void XAnnotateEllipseBorder_OnDragStarting(UIElement sender, DragStartingEventArgs args)
        {
            foreach (var doc in SelectedDocs)
            {
                args.Data.Properties[nameof(DragDocumentModel)] =
                    new DragDocumentModel(doc.ViewModel.DocumentController, false, doc);
                args.AllowedOperations =
                    DataPackageOperation.Link | DataPackageOperation.Move | DataPackageOperation.Copy;
                args.Data.RequestedOperation =
                    DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;
                doc.ViewModel.DecorationState = false;
            }
        }

        private void XTemplateEditorEllipseBorder_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            foreach (var doc in SelectedDocs)
            {
                doc.ManipulationMode = ManipulationModes.None; 
                doc.ToggleTemplateEditor();
            }
        }

        private void XTitleBorder_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            foreach (var doc in SelectedDocs)
            {
                CapturePointer(e.Pointer);
                doc.ManipulationMode = e.GetCurrentPoint(doc).Properties.IsRightButtonPressed
                    ? ManipulationModes.None
                    : ManipulationModes.All;
                e.Handled = doc.ManipulationMode == ManipulationModes.All;
            }
        }

        private void XTitleBorder_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            foreach (var doc in SelectedDocs)
            {
                doc.ShowContext();
                e.Handled = true;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void DocumentDecorations_OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            VisibilityState = Visibility.Visible;
        }

        private void DocumentDecorations_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            VisibilityState = Visibility.Collapsed;
        }
    }
}
