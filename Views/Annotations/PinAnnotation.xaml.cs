using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class PinAnnotation : UserControl, IAnchorable
    {
        public DocumentController DocumentController { get; set; }
        private NewAnnotationOverlay _parentOverlay;

        public PinAnnotation(NewAnnotationOverlay parent, DocumentController target = null)
        {
            this.InitializeComponent();
            
            _parentOverlay = parent;

            if (target != null)
            {
                var pdfView = _parentOverlay.GetFirstAncestorOfType<PdfView>();
                var width = pdfView?.PdfMaxWidth ??
                            _parentOverlay.GetFirstAncestorOfType<DocumentView>().ActualWidth;
                var height = pdfView?.PdfTotalHeight ??
                             _parentOverlay.GetFirstAncestorOfType<DocumentView>().ActualHeight;

                var dvm = new DocumentViewModel(target)
                {
                    Undecorated = true,
                    ResizersVisible = true,
                    DragBounds = new RectangleGeometry { Rect = new Rect(0, 0, width, height) }
                };
                (_parentOverlay.DataContext as NewAnnotationOverlayViewModel).ViewModels.Add(dvm);

                // bcz: should this be called in LoadPinAnnotations as well?
                dvm.DocumentController.AddFieldUpdatedListener(KeyStore.GoToRegionLinkKey,
                    delegate (DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args, Context context)
                    {
                        if (args.NewValue != null)
                        {
                            var regionDef = (args.NewValue as DocumentController).GetDataDocument()
                                .GetField<DocumentController>(KeyStore.LinkDestinationKey).GetDataDocument().GetRegionDefinition();
                            var pos = regionDef.GetPosition().Value;
                            pdfView?.ScrollToPosition(pos.Y);
                            dvm.DocumentController.RemoveField(KeyStore.GoToRegionLinkKey);
                        }
                    });
                parent.MainDocument.GetDataDocument()
                    .GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.PinAnnotationsKey)
                    .Add(dvm.DocumentController);
            }
        }

        public  void Render()
        {
            var point = DocumentController.GetPosition() ?? new Point(0, 0);
            point.X -= 10;
            point.Y -= 10;
            var pin = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = new SolidColorBrush(Colors.OrangeRed),
                IsDoubleTapEnabled = false
            };
            Canvas.SetLeft(pin, point.X - pin.Width / 2);
            Canvas.SetTop(pin, point.Y - pin.Height / 2);
            _parentOverlay.XAnnotationCanvas.Children.Add(pin);

            var vm = new NewAnnotationOverlay.SelectionViewModel(DocumentController, new SolidColorBrush(Color.FromArgb(128, 255, 0, 0)), new SolidColorBrush(Colors.OrangeRed));
            pin.DataContext = vm;

            var tip = new ToolTip
            {
                Placement = PlacementMode.Bottom
            };
            ToolTipService.SetToolTip(pin, tip);

            pin.PointerExited += (s, e) => tip.IsOpen = false;
            pin.PointerEntered += (s, e) =>
            {
                tip.IsOpen = true;
                //update tag content based on current tags of region
                var tags = new ObservableCollection<string>();

                foreach (var link in DocumentController.GetDataDocument().GetLinks(null))
                {
                    var currTags = link.GetDataDocument().GetLinkTags()?.TypedData ?? new List<TextController>();
                    foreach (var text in currTags)
                    {
                        tags.Add(text.Data);
                    }
                }

                var content = tags.Count == 0 ? "" : tags[0];
                if (tags.Count > 0)
                    tags.Remove(tags[0]);
                foreach (var str in tags)
                {
                    content = content + ", " + str;
                }
                tip.Content = content;
            };
            pin.Tapped += (sender, args) =>
            {
                if (this.IsCtrlPressed() && this.IsAltPressed())
                {
                    _parentOverlay.XAnnotationCanvas.Children.Remove(pin);
                    _parentOverlay.RegionDocsList.Remove(DocumentController);
                }
                _parentOverlay.SelectRegion(vm, args.GetPosition(this));
                args.Handled = true;
            };

            pin.PointerPressed += (s, e) => e.Handled = true;

            //handlers for moving pin
            pin.ManipulationMode = ManipulationModes.All;
            pin.ManipulationStarted += (s, e) =>
            {
                pin.ManipulationMode = ManipulationModes.All;
                e.Handled = true;
            };
            pin.ManipulationDelta += (s, e) =>
            {
                DocumentController.SetPosition(new Point(Canvas.GetLeft(pin) + e.Delta.Translation.X, Canvas.GetTop(pin) + e.Delta.Translation.Y));
                var p = Util.DeltaTransformFromVisual(e.Delta.Translation, s as UIElement);
                Canvas.SetLeft(pin, Canvas.GetLeft(pin) + p.X);
                Canvas.SetTop(pin, Canvas.GetTop(pin) + p.Y);
                e.Handled = true;
            };

            _parentOverlay.FormatRegionOptionsFlyout(DocumentController, pin);

            //formatting bindings
            pin.SetBinding(Shape.FillProperty, new Binding
            {
                Path = new PropertyPath(nameof(vm.SelectionColor)),
                Mode = BindingMode.OneWay
            });
            pin.SetBinding(VisibilityProperty, new Binding
            {
                Source = this,
                Path = new PropertyPath(nameof(NewAnnotationOverlay.AnnotationVisibility)),
                Converter = new BoolToVisibilityConverter(),
                Mode = BindingMode.OneWay
            });

            _parentOverlay.Regions.Add(vm);
        }

        public  void StartAnnotation(Point p)
        {
        }

        public  void UpdateAnnotation(Point p)
        {
        }

        public  void EndAnnotation(Point p)
        {
        }

        public double AddSubregionToRegion(DocumentController region)
        {
            throw new NotImplementedException();
        }
    }
}
