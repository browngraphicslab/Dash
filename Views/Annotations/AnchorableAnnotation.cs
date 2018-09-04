using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Dash.Annotations;
using System;

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
        protected readonly NewAnnotationOverlay ParentOverlay;
        protected double XPos = double.PositiveInfinity;
        protected double YPos = double.PositiveInfinity;
        protected SelectionViewModel ViewModel => DataContext as SelectionViewModel;

        protected AnchorableAnnotation(NewAnnotationOverlay parentOverlay, DocumentController regionDocumentController)
        {
            ParentOverlay = parentOverlay;
            RegionDocumentController = regionDocumentController;
        }

        public abstract void Render(SelectionViewModel vm);
        public abstract void StartAnnotation(Point p);
        public abstract void UpdateAnnotation(Point p);
        public abstract void EndAnnotation(Point p);
        public abstract double AddSubregionToRegion(DocumentController region);

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

        public void SelectRegionFromParent(ISelectable selectable, Point? mousePos)
        {
            // get the list of linkhandlers starting from this all the way up to the mainpage
            var linkHandlers = ParentOverlay.GetAncestorsOfType<ILinkHandler>().ToList();
            // NewAnnotationOverlay is an ILinkHandler but isn't included in GetAncestorsOfType()
            linkHandlers.Insert(0, ParentOverlay);
            ParentOverlay.AnnotationManager.FollowRegion(selectable.RegionDocument, linkHandlers, mousePos ?? new Point(0, 0));

            // we still want to follow the region even if it's already selected, so this code's position matters
            if (ParentOverlay.SelectedRegion != selectable && ParentOverlay.IsInVisualTree())
            {
                foreach (var nvo in ParentOverlay.GetFirstAncestorOfType<DocumentView>().GetDescendantsOfType<NewAnnotationOverlay>())
                foreach (var r in nvo.Regions.Where(r => r.RegionDocument.Equals(selectable.RegionDocument)))
                {
                    nvo.SelectedRegion?.Deselect();
                    nvo.SelectedRegion = r;
                    r.Select();
                }
            }
        }

        protected virtual void InitializeAnnotationObject(Shape shape, Point? pos, PlacementMode mode)
        {
            shape.SetBinding(Shape.FillProperty, new Binding
            {
                Path = new PropertyPath(nameof(ViewModel.SelectionColor)),
                Mode = BindingMode.OneWay
            });
            shape.Tapped += (sender, args) =>
            {
                if (this.IsCtrlPressed() && this.IsAltPressed())
                {
                    ParentOverlay.XAnnotationCanvas.Children.Remove(shape);
                    ParentOverlay.RegionDocsList.Remove(RegionDocumentController);
                }
                SelectRegionFromParent(ViewModel, args.GetPosition(this));
                args.Handled = true;
            };
            //TOOLTIP TO SHOW TAGS
            var tip = new ToolTip { Placement = mode };
            ToolTipService.SetToolTip(shape, tip);
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
            //formatting bindings
            shape.SetBinding(Shape.FillProperty, new Binding
            {
                Path = new PropertyPath(nameof(ViewModel.SelectionColor)),
                Mode = BindingMode.OneWay
            });

            if (pos != null)
            {
                System.Diagnostics.Debug.WriteLine(" rende " + pos.Value.X + " " + pos.Value.Y);
                shape.RenderTransform = new TranslateTransform() { X = pos.Value.X, Y = pos.Value.Y };
                //Canvas.SetLeft(shape, pos.Value.X);
                //Canvas.SetTop(shape, pos.Value.Y);
            }
            else
            {
                var bindingX = new FieldBinding<PointController>()
                {
                    Mode = BindingMode.OneWay,
                    Document = RegionDocumentController,
                    Key = KeyStore.PositionFieldKey,
                    Converter = new PositionToCanvasPositionConverter(false, shape)
                };
                this.AddFieldBinding(Canvas.LeftProperty, bindingX);
                var bindingY = new FieldBinding<PointController>()
                {
                    Mode = BindingMode.OneWay,
                    Document = RegionDocumentController,
                    Key = KeyStore.PositionFieldKey,
                    Converter = new PositionToCanvasPositionConverter(true, shape)
                };
                this.AddFieldBinding(Canvas.TopProperty, bindingY);
            }

            if (RegionDocumentController != null)
            {
                FormatRegionOptionsFlyout(RegionDocumentController, this);
            }
        }

        public class PositionToCanvasPositionConverter : SafeDataToXamlConverter<Point, double>
        {
            bool _y;
            Shape _xShape;
            public PositionToCanvasPositionConverter(bool y, Shape xShape) { _y = y; _xShape = xShape; }
            public override double ConvertDataToXaml(Point data, object parameter = null)
            {
                return _y ? data.Y - _xShape.Height / 2 : data.X - _xShape.Width / 2;
            }
            public override Point ConvertXamlToData(double xaml, object parameter = null)
            {
                throw new NotImplementedException();
            }
        }

        public sealed class SelectionViewModel : INotifyPropertyChanged, ISelectable
        {
            private SolidColorBrush _selectionColor;
            public SolidColorBrush SelectionColor
            {
                [UsedImplicitly]
                get => _selectionColor;
                private set
                {
                    if (_selectionColor == value)
                    {
                        return;
                    }
                    _selectionColor = value;
                    OnPropertyChanged();
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public SolidColorBrush SelectedBrush { get; set; }

            public SolidColorBrush UnselectedBrush { get; set; }

            public SelectionViewModel(DocumentController region,
                SolidColorBrush selectedBrush=null,
                SolidColorBrush unselectedBrush=null)
            {
                RegionDocument = region;
                UnselectedBrush = unselectedBrush;
                SelectedBrush = selectedBrush;
                _selectionColor = UnselectedBrush;
            }

            public bool Selected { get; private set; }

            public DocumentController RegionDocument { get; }

            public void Select()
            {
                SelectionColor = SelectedBrush;
                Selected = true;
            }

            public void Deselect()
            {
                SelectionColor = UnselectedBrush;
                Selected = false;
            }

            [NotifyPropertyChangedInvocator]
            private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}