using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Dash;
using Dash.Annotations;
using MyToolkit.Multimedia;
using static Dash.DataTransferTypeInfo;
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
        protected readonly NewAnnotationOverlay ParentOverlay;
        protected double XPos = double.PositiveInfinity;
        protected double YPos = double.PositiveInfinity;
        public SelectionViewModel ViewModel => DataContext as SelectionViewModel;
        public static AnchorableAnnotation CreateAnnotation(NewAnnotationOverlay parent, 
            DocumentController regionDocumentController)
        {
            var svm = new SelectionViewModel(regionDocumentController);
            AnchorableAnnotation annotation = null;
            switch (regionDocumentController.GetAnnotationType())
            {
                case AnnotationType.Pin:       svm = new SelectionViewModel(regionDocumentController,
                                                      new SolidColorBrush(Color.FromArgb(255, 0x1f, 0xff, 0)), new SolidColorBrush(Colors.Red));
                                               annotation = new PinAnnotation(parent, svm); break;
                case AnnotationType.Region:    annotation = new RegionAnnotation(parent, svm); break;
                case AnnotationType.Selection: annotation = new TextAnnotation(parent, svm); break;
                default:  break;
            }
            return annotation;
        }
        protected AnchorableAnnotation(NewAnnotationOverlay parentOverlay, DocumentController regionDocumentController)
        {
            ParentOverlay = parentOverlay;
            RegionDocumentController = regionDocumentController;
        }
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
                foreach (var r in nvo.SelectableRegions.Where(r => r.RegionDocument.Equals(selectable.RegionDocument)))
                { 
                    if (nvo.SelectedRegion != null)
                        nvo.SelectedRegion.IsSelected = false;
                    nvo.SelectedRegion = r;
                    r.IsSelected = true;
                }
            }
        }

        protected virtual void InitializeAnnotationObject(Shape shape, Point? pos, PlacementMode mode)
        {
            shape.SetBinding(Shape.FillProperty, ViewModel.GetFillBinding());
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

            if (pos != null)
            {
                shape.RenderTransform = new TranslateTransform() { X = pos.Value.X, Y = pos.Value.Y };
            }
            else
            {
                var bindingX = new FieldBinding<PointController>()
                {
                    Mode = BindingMode.OneWay,
                    Document = RegionDocumentController,
                    Key = KeyStore.PositionFieldKey,
                    Converter = new PointToCoordinateConverter(false)
                };
                this.AddFieldBinding(Canvas.LeftProperty, bindingX);
                var bindingY = new FieldBinding<PointController>()
                {
                    Mode = BindingMode.OneWay,
                    Document = RegionDocumentController,
                    Key = KeyStore.PositionFieldKey,
                    Converter = new PointToCoordinateConverter(true)
                };
                this.AddFieldBinding(Canvas.TopProperty, bindingY);
            }

            if (RegionDocumentController != null)
            {
                FormatRegionOptionsFlyout(RegionDocumentController, this);
            }
        }

        public sealed class SelectionViewModel : INotifyPropertyChanged, ISelectable
        {
            SolidColorBrush _selectedBrush, _unselectedBrush;
            bool _isSelected = false;
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
                    Path = new PropertyPath(nameof(ViewModel.IsSelected)),
                    Mode = BindingMode.OneWay,
                    Converter = new BoolToBrushConverter(_selectedBrush, _unselectedBrush)
                };
            }

            public SelectionViewModel(DocumentController region,
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