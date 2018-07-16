using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Zu.TypeScript.TsTypes;

namespace Dash
{
    /// <summary>
    /// These EventArgs are used for regioning-related events.
    /// </summary>
    public class RegionEventArgs : EventArgs
    {
        public DocumentController Link { get; set; }
    }

    public class VisualAnnotationManager : AnnotationManager
    {
        private Point _anchorPoint;
        private bool _isDragging;
        private DocumentView _lastNearest;
        private RegionBox _selectedRegion;
        private List<RegionBox> _visualRegions;
        private ListController<DocumentController> _dataRegions;
        private RegionVisibilityState _regionState;
        private bool _isLinkMenuOpen = false;
        private bool _isPreviousRegionSelected;
        private DocumentController _docCtrl;
        private IVisualAnnotatable _element;
        private AnnotationOverlay _overlay;
        public event EventHandler<RegionEventArgs> NewRegionMade;
        public event EventHandler<RegionEventArgs> RegionRemoved;

        private enum RegionVisibilityState
        {
            Visible,
            Hidden
        }

        /// <summary>
        /// To instantiate a VisualAnnotationManager, note that you must supply an AnnotationOverlay UserControl where the annotation fields go. This is usually in the same Grid as the view you're annotating.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="dc"></param>
        /// <param name="overlay"></param>
        public VisualAnnotationManager(IVisualAnnotatable element, DocumentController dc, AnnotationOverlay overlay) : base(element.Self())
        {
            _visualRegions = new List<RegionBox>();
            _regionState = RegionVisibilityState.Hidden;
            _docCtrl = dc;
            _element = element;
	        var fe = element as FrameworkElement;
	        fe.PointerPressed += Element_OnNewRegionStarted;
	        fe.PointerMoved += Element_OnNewRegionMoved;
	        fe.PointerReleased += Element_OnNewRegionEnded;
	        fe.PointerExited += Element_OnNewRegionEnded;
			//_element.ContentLoaded += Element_OnLoaded;
			_overlay = overlay;

            _dataRegions = _docCtrl.GetDataDocument().GetField<ListController<DocumentController>>(KeyStore.RegionsKey);
            if (_dataRegions == null) return;
            foreach (var region in _dataRegions.TypedData)
            {
                var topLeft = region.GetDataDocument().GetField<PointController>(KeyStore.VisualRegionTopLeftPercentileKey).Data;
                var bottomRight = region.GetDataDocument().GetField<PointController>(KeyStore.VisualRegionBottomRightPercentileKey).Data;

                MakeNewRegionBox(topLeft, bottomRight, region).Hide();
            }
        }
        public double Zoom = 1;
        private void Element_OnNewRegionStarted(object sender, PointerRoutedEventArgs e)
        {
            var rawpos = e.GetCurrentPoint(_element.GetPositionReference()).Position;
            var pos = new Point(rawpos.X / Zoom, rawpos.Y / Zoom);
            _anchorPoint = pos;
            _isDragging = true;

            //reset and get rid of the region preview
            _overlay.SetDuringPreviewSize(new Size(0, 0));

            // what do the following two lines accomplish
            _overlay.DuringVisibility = Visibility.Collapsed;
            _overlay.DuringVisibility = Visibility.Visible;
			
            DeselectRegions();
            
            //delete if control is pressed
            if (MainPage.Instance.IsAltPressed())
            {
                DeleteRegion(_selectedRegion);
                _isPreviousRegionSelected = false;
                return;
            }
            //select otherwise
            //if (xLinkStack.Visibility == Visibility.Collapsed)
            if (!_isLinkMenuOpen) RegionSelected(_selectedRegion, e.GetCurrentPoint(MainPage.Instance).Position);
            
        }

        private void Element_OnNewRegionMoved(object sender, PointerRoutedEventArgs e)
        {
            var properties = e.GetCurrentPoint(_element.Self()).Properties;

            if (_isDragging && properties.IsRightButtonPressed == false && properties.IsLeftButtonPressed == true)
            {
                //update size of preview region box according to mouse movement
                
                var rawpos = e.GetCurrentPoint(_element.GetPositionReference()).Position;
                var pos = new Point(rawpos.X / Zoom, rawpos.Y / Zoom);

                var x = Math.Min(pos.X, _anchorPoint.X);
                var y = Math.Min(pos.Y, _anchorPoint.Y);
                _overlay.DuringMargin = new Thickness(x, y, 0, 0);

                _overlay.SetDuringPreviewSize(new Size(Math.Abs(pos.X - _anchorPoint.X), Math.Abs(pos.Y - _anchorPoint.Y)));
            }
        }

        private void Element_OnNewRegionEnded(object sender, PointerRoutedEventArgs pointerRoutedEventArgs)
        {
	        if (!_isDragging) return;
            _overlay.DuringVisibility = Visibility.Collapsed;

            if (MainPage.Instance.IsCtrlPressed())
            {
                return;
            }

            _isDragging = false;

            // the box only sticks around if it's of a large enough size
            if (_overlay.GetDuringPreviewSize().Width < 30 && _overlay.GetDuringPreviewSize().Height < 30) return;

            _overlay.SetRegionBoxPosition(new Size(_element.GetPositionReference().ActualWidth, _element.GetPositionReference().ActualHeight));
            _overlay.PostVisibility = Visibility.Visible;
        }

        private void xRegion_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = false;
            this.RegionSelected((RegionBox) sender, e.GetPosition(MainPage.Instance));
            e.Handled = true;
        }

        //shows region when user hovers over it
        private void Region_OnPointerEntered(object sender, RoutedEventArgs e)
        {
            if (sender is RegionBox region && !_isDragging)
            {
                region.Show();
            }
        }

        //hides region when user's cursor leaves it if region visibility mode is hidden
        private void Region_OnPointerExited(object sender, RoutedEventArgs e)
        {
            if (sender is RegionBox region && !_isDragging)
            {
                if (_regionState == RegionVisibilityState.Hidden) region.Hide();
            }
        }

        public void RegionSelected(object region, Point pos, DocumentController chosenDC = null)
        {
            if (region == null) return;

            DocumentController theDoc;

            if (region is RegionBox regionBox)
            {
                //get the linked doc of the selected region
                theDoc = regionBox.LinkTo;
                if (theDoc == null) return;

                SelectRegion(regionBox);
            }
            else
            {
                theDoc = _docCtrl;
            }

            //delete if control is pressed
            if (MainPage.Instance.IsAltPressed() && region is RegionBox)
            {
                DeleteRegion((RegionBox) region);
                _isPreviousRegionSelected = false;
                return;
            }

            if (Math.Abs(pos.X) < 0.5 && Math.Abs(pos.Y) < 0.5) pos = _docCtrl.GetField<PointController>(KeyStore.PositionFieldKey).Data;

            RegionPressed(theDoc, pos, chosenDC);
        }

        //delete passed-in region
        public void DeleteRegion(RegionBox region)
        {
            //collapse any open selection box & links
            _overlay.PostVisibility = Visibility.Collapsed;

            //remove actual region
            if (region != null)
            {
                _overlay.RemoveRegion(region);
                _visualRegions?.Remove(region);
                _dataRegions?.Remove(region.LinkTo);
                OnRegionRemoved(region.LinkTo);
            }

            //if region is selected, unhighlight the linked doc
            if (region == _selectedRegion && _lastNearest?.ViewModel?.DocumentController != null)
            {
                MainPage.Instance.HighlightDoc(_lastNearest.ViewModel.DocumentController, false, 2);
                _lastNearest = null;
                //TODO: Remove annotation from workspace?
            }

            DeselectRegions();
        }

        //called when the selected region changes
        public void UpdateHighlight(DocumentView nearestOnCollection)
        {
            //unhighlight last doc
            if (_lastNearest?.ViewModel != null)
            {
                MainPage.Instance.HighlightDoc(_lastNearest.ViewModel.DocumentController, false, 2);
            }

	        if (nearestOnCollection == null) return;
            //highlight this linked doc
            _lastNearest = nearestOnCollection;
            MainPage.Instance.HighlightDoc(nearestOnCollection.ViewModel.DocumentController, false, 1);
        }

        //deselects all regions on a view
        private void DeselectRegions()
        {
            _isPreviousRegionSelected = false;
            _overlay.PostVisibility = Visibility.Collapsed;
            //xRegionPostManipulationPreview.xCloseRegionButton.Visibility = Visibility.Collapsed;

            //unhighlight last selected regions' link
            if (_lastNearest?.ViewModel?.DocumentController != null)
            {
                MainPage.Instance.HighlightDoc(_lastNearest.ViewModel.DocumentController, false, 2);
            }
        }

        private void SelectRegion(RegionBox region)
        {
            _selectedRegion = region;
            _isPreviousRegionSelected = true;
            //create a preview region to show that this region is selected
            _overlay.SetDuringPreviewSize(new Size(0, 0));
            _overlay.PostColumn1Width = region.Column1.Width;
            _overlay.PostColumn2Width = region.Column2.Width;
            _overlay.PostColumn3Width = region.Column3.Width;
            _overlay.PostRow1Height = region.Row1.Height;
            _overlay.PostRow2Height = region.Row2.Height;
            _overlay.PostRow3Height = region.Row3.Height;
            _overlay.PostVisibility = Visibility.Visible;
            //xRegionPostManipulationPreview.xCloseRegionButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        public void SelectRegion(DocumentController dc)
        {
            foreach (var d in _visualRegions)
            {
                if (!d.LinkTo.Equals(dc)) continue;
                SelectRegion(d);
                return;
            }
        }

        //ensures all regions are visible
        public void ShowRegions()
        {
            _regionState = RegionVisibilityState.Visible;

            if (_visualRegions != null && _visualRegions.Any())
            {
                foreach (RegionBox region in _visualRegions)
                {
                    region.Show();
                }
            }
        }

        //hides all visible regions 
        public void HideRegions()
        {
            _regionState = RegionVisibilityState.Hidden;

            if (_visualRegions != null && _visualRegions.Any())
            {
                //first, deselect any selected regions
                DeselectRegions();

                foreach (RegionBox region in _visualRegions)
                {
                    //region.Visibility = Visibility.Collapsed;
                    region.Hide();
                }
            }
        }

        public bool IsSomethingSelected()
        {
            return _overlay.PostVisibility == Visibility.Visible;
        }

        public DocumentController GetRegionDocument()
        {
            if (!IsSomethingSelected()) return _docCtrl;

            DocumentController note;

            //if region is selected, access the selected region's doc controller
            if (_isPreviousRegionSelected && _selectedRegion != null)
            {
                //add this link to list of links
                note = _selectedRegion.LinkTo;
            }
            else
            {
                note = _element.GetDocControllerFromSelectedRegion();

                //add to regions list
                if (_dataRegions == null)
                {
                    _dataRegions = new ListController<DocumentController>(note);
                    _docCtrl.GetDataDocument().SetField(KeyStore.RegionsKey, _dataRegions, true);
                }
                else
                {
                    _dataRegions.Add(note);
                }

                OnNewRegionMade(note);

                // use During Preview here because it's the one with actual pixel measurements
                var box = MakeNewRegionBox(note, _overlay.GetTopLeftPercentile(), _overlay.GetBottomRightPercentile());
                note.GetDataDocument().SetField<PointController>(KeyStore.VisualRegionTopLeftPercentileKey,
                    box.TopLeftPercentile, true);
                note.GetDataDocument().SetField<PointController>(KeyStore.VisualRegionBottomRightPercentileKey,
                    box.BottomRightPercentile, true);

                _overlay.PostVisibility = Visibility.Collapsed;
            }

            return note;
        }

        // Only adds the region box, visually
        private RegionBox MakeNewRegionBox(DocumentController region, Point topLeft, Point bottomRight)
        {
            var newBox = new RegionBox { LinkTo = region, Manager = this };
            newBox.SetPosition(topLeft, bottomRight);
            return SetUpNewRegionBox(newBox);
        }

        private RegionBox MakeNewRegionBox(Point topLeft, Point bottomRight, DocumentController region)
        {
            var newBox = new RegionBox { LinkTo = region, Manager = this };
            newBox.SetPosition(topLeft, bottomRight);
            return SetUpNewRegionBox(newBox);
        }

        private RegionBox SetUpNewRegionBox(RegionBox newBox)
        {
            _overlay.AddRegion(newBox);
            newBox.Tapped += xRegion_OnTapped;
            newBox.PointerEntered += Region_OnPointerEntered;
            newBox.PointerExited += Region_OnPointerExited;
            _visualRegions.Add(newBox);

            return newBox;
        }

        public void ToggleRegionPreviewVisibility(Visibility vis)
        {
            _overlay.PostVisibility = vis;
            _overlay.DuringVisibility = vis;
        }

        /// <summary>
        /// Fired whenever a new region is made, for viewers like PDFView who then need to do further things.
        /// </summary>
        protected virtual void OnNewRegionMade(DocumentController dc)
        {
            NewRegionMade?.Invoke(this, new RegionEventArgs {Link = dc });
        }

        /// <summary>
        /// Fired whenever a region is removed, for viewers like PDFView who then need to do further things.
        /// </summary>
        /// <param name="dc"></param>
        protected virtual void OnRegionRemoved(DocumentController dc)
        {
            RegionRemoved?.Invoke(this, new RegionEventArgs { Link = dc });
        }
    }
}
