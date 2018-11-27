using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Dash.Annotations;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Dash
{
    public enum AnnotationType
    {
        None,
        Region,
        Selection,
        Ink,
        Pin
    }

    public enum PushpinType
    {
        Text,
        Video,
        Image
    }

    public abstract class AnchorableAnnotation : UserControl
    {
        public DocumentController RegionDocumentController;
        public AnnotationType AnnotationType = AnnotationType.None;
        protected readonly AnnotationOverlay ParentOverlay;
        protected double XPos = double.PositiveInfinity;
        protected double YPos = double.PositiveInfinity;
        public Selection ViewModel => DataContext as Selection;
        
        protected AnchorableAnnotation(AnnotationOverlay parentOverlay, DocumentController regionDocumentController)
        {
            ParentOverlay = parentOverlay;
            RegionDocumentController = regionDocumentController;
        }
        public abstract bool IsInView(Rect bounds);
        public abstract void StartAnnotation(Point p);
        public abstract void UpdateAnnotation(Point p);
        public abstract void EndAnnotation(Point p);
        public abstract double AddToRegion(DocumentController region);

        public void FormatRegionOptionsFlyout(DocumentController region, UIElement regionGraphic)
        {
            // context menu that toggles whether annotations should be show/ hidden on scroll

            var flyout = new MenuFlyout();
            var visOnScrollON = new MenuFlyoutItem();
            var visOnScrollOFF = new MenuFlyoutItem();
            visOnScrollON.Text = "Unpin Annotation";
            visOnScrollOFF.Text = "Pin Annotation";

            void VisOnScrollOnOnClick(object o, RoutedEventArgs routedEventArgs)
            {
                var allLinks = region.GetDataDocument().GetLinks(null);
                var allVisible = allLinks.All(doc => doc.GetDataDocument().GetField<BoolController>(KeyStore.IsAnnotationScrollVisibleKey)?.Data ?? false);

                foreach (var link in allLinks)
                {
                    link.GetDataDocument().SetField<BoolController>(KeyStore.IsAnnotationScrollVisibleKey, !allVisible, true);
                }
            }
            visOnScrollON.Click += VisOnScrollOnOnClick;
            visOnScrollOFF.Click += VisOnScrollOnOnClick;
            regionGraphic.ContextFlyout = flyout;
            regionGraphic.RightTapped += (s, e) =>
            {
                var allLinks = region.GetDataDocument().GetLinks(null);
                bool allVisible = allLinks.All(doc => doc.GetDataDocument().GetField<BoolController>(KeyStore.IsAnnotationScrollVisibleKey)?.Data ?? false);

                var item = allVisible ? visOnScrollON : visOnScrollOFF;
                flyout.Items?.Clear();
                flyout.Items?.Add(item);
                flyout.ShowAt(regionGraphic as FrameworkElement);
            };
        }

        public void SelectRegionFromParent(Selection selectable, Point? mousePos)
        {
            // get the list of linkhandlers starting from this all the way up to the mainpage
            var linkHandlers = ParentOverlay.GetAncestorsOfType<ILinkHandler>().ToList();
            // AnnotationOverlay is an ILinkHandler but isn't included in GetAncestorsOfType()
            linkHandlers.Insert(0, ParentOverlay);
            ParentOverlay.AnnotationManager.FollowRegion(this.GetFirstAncestorOfType<DocumentView>(), selectable.RegionDocument, linkHandlers, mousePos ?? new Point(0, 0));

            // we still want to follow the region even if it's already selected, so this code's position matters
            if (ParentOverlay.SelectedRegion != selectable && ParentOverlay.IsInVisualTree())
            {
                foreach (var nvo in ParentOverlay.GetFirstAncestorOfType<DocumentView>().GetDescendantsOfType<AnnotationOverlay>())
                foreach (var r in nvo.SelectableRegions.Where(r => r.RegionDocument.Equals(selectable.RegionDocument)))
                { 
                    if (nvo.SelectedRegion != null)
                        nvo.SelectedRegion.IsSelected = false;
                    nvo.SelectedRegion = r;
                    r.IsSelected = true;
                }
            }
        }

        protected virtual void InitializeAnnotationObject(FrameworkElement shape, Point? pos, PlacementMode mode)
        {
            if (shape is Shape)
            {
                shape.SetBinding(Shape.FillProperty, ViewModel.GetFillBinding());
            }
            shape.Tapped += (sender, args) =>
            {
                if (this.IsCtrlPressed() && this.IsAltPressed())
                {
                    ParentOverlay.XAnnotationCanvas.Children.Remove(this);
                    ParentOverlay.RegionDocsList.Remove(RegionDocumentController);
                }
                else SelectRegionFromParent(ViewModel, args.GetPosition(this));
                args.Handled = true;
            };
            //TOOLTIP TO SHOW TAGS
            var tip = new ToolTip { Placement = mode  };
            ToolTipService.SetToolTip(shape, tip);
            tip.IsHitTestVisible = false;
            shape.PointerExited += (s, e) => tip.IsOpen = false;
            shape.PointerEntered += (s, e) =>
            {
                tip.IsOpen = true;
                var regionDoc = ViewModel.RegionDocument.GetDataDocument();

                var allLinkSets = new List<IEnumerable<DocumentController>>
                {
                     regionDoc.GetLinks(KeyStore.LinkFromKey)?.Select(l => l.GetDataDocument()) ?? new DocumentController[] { },
                     regionDoc.GetLinks(KeyStore.LinkToKey)?.Select(l => l.GetDataDocument()) ?? new DocumentController[] { }
                };
                var allTagSets = allLinkSets.SelectMany(lset => lset.Select(l => l.GetLinkTag()));
                var allTags = regionDoc.GetLinks(null).Select((l) => l.GetDataDocument().GetLinkTag().Data);

                //update tag content based on current tags of region
                tip.Content = allTags.Where((t, i) => i > 0).Aggregate(allTags.FirstOrDefault(), (input, str) => input += ", " + str);
            };

            if (pos != null)
            {

                shape.RenderTransform = new TranslateTransform { X = pos.Value.X, Y = pos.Value.Y };
            }
            else
            {
                var bindingXf = new FieldBinding<PointController>()
                {
                    Mode = BindingMode.OneWay,
                    Document = RegionDocumentController,
                    Key = KeyStore.PositionFieldKey,
                    Converter = new PointToTranslateTransformConverter()
                };
                this.AddFieldBinding(RenderTransformProperty, bindingXf);
            }

            if (RegionDocumentController != null)
            {
                FormatRegionOptionsFlyout(RegionDocumentController, this);
            }
        }
        
        public sealed class Selection : INotifyPropertyChanged
        {
            SolidColorBrush _selectedBrush, _unselectedBrush;
            bool            _isSelected = false;
            public bool IsSelected
            {
                [UsedImplicitly]
                get => _isSelected;
                set
                {
                    if (_isSelected != value)
                    {
                        _isSelected = value;
                        OnPropertyChanged();
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public Binding GetFillBinding()
            {
                return new Binding
                {
                    Path = new PropertyPath(nameof(IsSelected)),
                    Mode = BindingMode.OneWay,
                    Converter = new BoolToBrushConverter(_selectedBrush, _unselectedBrush)
                };
            }

            public Selection(DocumentController region,
                SolidColorBrush selectedBrush=null,
                SolidColorBrush unselectedBrush=null)
            {
                RegionDocument = region;
                _unselectedBrush = unselectedBrush ?? new SolidColorBrush(Color.FromArgb(100, 0xff, 0xff, 0));
                _selectedBrush   = selectedBrush   ?? new SolidColorBrush(Color.FromArgb(0x30, 0xff, 0, 0));
            }

            public DocumentController RegionDocument { get; }

            [NotifyPropertyChangedInvocator]
            private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
