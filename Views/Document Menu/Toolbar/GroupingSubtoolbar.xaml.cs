﻿using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class GroupingSubtoolbar : ICommandBarBased
    {
        //INSTANCE VARIABLES
        private DocumentController _currentDocController;
        private Windows.UI.Color _currentColor;
	    private DocumentView _groupView;

        //ORIENTATION property registration and declaration
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
            "Orientation", typeof(Orientation), typeof(GroupingSubtoolbar), new PropertyMetadata(default(Orientation)));

        public Orientation Orientation
        {
            get => (Orientation) GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value); 
        }

        //CONSTRUCTOR
        public GroupingSubtoolbar()
        {
            InitializeComponent();

            //Initial values
           // xOpacitySlider.Value = 128; //Effectively an opacity of 0.5
			xGroupForegroundColorPicker.SetOpacity(128);

            _currentColor = Windows.UI.Color.FromArgb(0x80, 0xff, 0x00, 0x00); //Red with an opacity of 0.5

            FormatDropdownMenu();

            SetUpToolTips();

            //Sets orientation binding when the actual command bar UI element is loaded. 
            //Potential bug fix: if visibility of command bar is collapsed, will never load and will fail to add event handler
            xGroupCommandbar.Loaded += delegate
            {
                xGroupCommandbar.SetBinding(StackPanel.OrientationProperty, new Binding
                {
                    Source = this,
                    Path = new PropertyPath(nameof(Orientation)),
                    Mode = BindingMode.OneWay
                });
                Visibility = Visibility.Collapsed;
            };

	        xGroupForegroundColorPicker.ParentFlyout = xColorFlyout;
        }

        //SETUP AND HELPER METHODS

        /*
         * Draws on ToolbarConstants to set width, height and margin of the ComboBox (shape selection dropdown menu)
         */
        private void FormatDropdownMenu()
        {
            //xShapeOptionsDropdown.Width = ToolbarConstants.ComboBoxWidth;
            //xShapeOptionsDropdown.Height = ToolbarConstants.ComboBoxHeight;
            //xShapeOptionsDropdown.Margin = new Thickness(ToolbarConstants.ComboBoxMarginOpen);
        }

        /*
         * Runs the current ARGB color through the "filter" of the current opacity slider value by replacing default alpha prefix with the desired substitution
         
        private Windows.UI.Color GetColorWithUpdatedOpacity()
        {
            if (_currentColor == null)
                return Windows.UI.Color.FromArgb(0x80, 0x00, 0x00, 0x00); //A fallback during startup (edge case) where current color string is null
            var alpha = (byte)(xOpacitySlider.Value / xOpacitySlider.Maximum * 255); //Ratio of current value to maximum determines the relative desired opacity
            return Windows.UI.Color.FromArgb(alpha, _currentColor.R, _currentColor.G, _currentColor.B);
        }
		*/
    //ACCESSORS AND MUTATORS

        /*
        * Whenever a group is clicked, it receives the document view associated with the click for editing, etc, stored in this mutator. 
        */
        public void SetGroupBinding(DocumentView selection)
        {
	        _currentDocController = selection.ViewModel.DocumentController;
	        _groupView = selection;
        } 

        /*
         * Determines whether or not to hide or display the combo box: in context, this applies only to toggling rotation which is not currently supported
         */
        public void SetComboBoxVisibility(Visibility visibility) => xShapeOptionsDropdown.Visibility = visibility;

        /*
         * Opens and or closes (i.e. hides/displays text labels on) the subtoolbar while maintaining overall visibility and adjusting the combo box margin for the new dimensions
         */
        public void CommandBarOpen(bool status)
        {
            //Whether or not open or closed, should always be visible if some content is selected
            xGroupCommandbar.Visibility = Visibility.Visible;
            //Updates combo box dimensions
            //xShapeOptionsDropdown.Margin = status ? new Thickness(ToolbarConstants.ComboBoxMarginOpen) : new Thickness(ToolbarConstants.ComboBoxMarginClosed);

        }

        /*
        * A key piece in allowing the group to be made editable on control click (might be some other modifier down the road). If not an adornment group, cannot edit via toolbar, with the converse being true.
        */
        public void TryMakeGroupEditable(bool makeAdornmentGroup)
        {
            _currentDocController?.GetDataDocument().SetIsAdornment(makeAdornmentGroup);
        }

        

    //EVENT HANDLERS

        /*
         * The shape-selection logic for the combo box
         */
        private void ShapeOptionsDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                var switchList = new List<string>
                {
                    BackgroundShape.AdornmentShape.Rectangular.ToString(),
                    BackgroundShape.AdornmentShape.Elliptical.ToString(),
                    BackgroundShape.AdornmentShape.RoundedRectangle.ToString(),
                    BackgroundShape.AdornmentShape.RoundedFrame.ToString(),
                    BackgroundShape.AdornmentShape.Pentagonal.ToString(),
                    BackgroundShape.AdornmentShape.Hexagonal.ToString(),
                    BackgroundShape.AdornmentShape.Octagonal.ToString(),
                    BackgroundShape.AdornmentShape.CustomPolygon.ToString(),
                    BackgroundShape.AdornmentShape.CustomStar.ToString(),
                    BackgroundShape.AdornmentShape.Clover.ToString(),
                };

                var index = xShapeOptionsDropdown.SelectedIndex;
                var selectedLabel = index < switchList.Count ? switchList[index] : BackgroundShape.AdornmentShape.Rectangular.ToString();
                _currentDocController?.GetDataDocument().SetField<TextController>(KeyStore.DataKey, selectedLabel, true);
                if (xSideToggleButtonGrid != null) xSideToggleButtonGrid.Visibility = Visibility.Collapsed;

                if (!(index == GroupGeometryConstants.CustomPolyDropdownIndex || index == GroupGeometryConstants.CustomStarDropdownIndex)) return;

                if (xSideToggleButtonGrid != null) xSideToggleButtonGrid.Visibility = Visibility.Visible;
                var safeSideCount = _currentDocController?.GetDataDocument().GetSideCount() ?? GroupGeometryConstants.DefaultCustomPolySideCount;
                _currentDocController?.GetDataDocument().SetSideCount(safeSideCount);
                xSideCounter.Text = safeSideCount.ToString("G");
            }
        }

   
        /*
         * Create a new group with the current selections
         */
        private void XGroup_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            // TODO: when multiselect is eventually implemented, group all selected elements with as small a group as possible

        }

        /*
         * In effect, delete current group
         */
        private void XUngroup_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            // TODO: Delete groups on tapped

        }

        /*
         * Called whenever a group is clicked, the subtoolbar extracts its shape, color and opacity
         */
        public void AcknowledgeAttributes()
        {
            //SHAPE: If it's present, retrieves the stored shape associated with this group and reflects it in the combo box field
            var shape = _currentDocController?.GetDataDocument().GetDereferencedField<TextController>(KeyStore.DataKey, null)?.Data;

            var switchDictionary = new Dictionary<string, int>
            {
                [BackgroundShape.AdornmentShape.Rectangular.ToString()] = 0,
                [BackgroundShape.AdornmentShape.Elliptical.ToString()] = 1,
                [BackgroundShape.AdornmentShape.RoundedRectangle.ToString()] = 2,
                [BackgroundShape.AdornmentShape.RoundedFrame.ToString()] = 3,
                [BackgroundShape.AdornmentShape.Pentagonal.ToString()] = 4,
                [BackgroundShape.AdornmentShape.Hexagonal.ToString()] = 5,
                [BackgroundShape.AdornmentShape.Octagonal.ToString()] = 6,
                [BackgroundShape.AdornmentShape.CustomPolygon.ToString()] = 7,
                [BackgroundShape.AdornmentShape.CustomStar.ToString()] = 8,
                [BackgroundShape.AdornmentShape.Clover.ToString()] = 9,
            };

            xShapeOptionsDropdown.SelectedIndex = switchDictionary.ContainsKey(shape) ? switchDictionary[shape] : 0;

            //COLOR: If it's present, retrieves the stored color associated with this group and assigns it to the current color... 
            //...doesn't interact with color picker, but changing opacity will do so in the context of the proper color
            _currentColor = _currentDocController?.GetDataDocument().GetBackgroundColor() ?? Windows.UI.Colors.Red;

            //OPACITY: If it's present, retrieves the stored slider value (double stored as a string) associated with this group and...
           // xOpacitySlider.Value = _currentDocController?.GetDataDocument().GetDereferencedField<NumberController>(KeyStore.OpacitySliderValueKey, null)?.Data ?? 128;

            //NUM SIDES
            xSideCounter.Text = (_currentDocController?.GetDataDocument().GetSideCount() ?? GroupGeometryConstants.DefaultCustomPolySideCount).ToString("G");
            
        }
		
        private void XAddSide_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var inc = (MainPage.Instance.IsShiftPressed()) ? GroupGeometryConstants.MassiveInc : GroupGeometryConstants.SmallInc;
            IncrementCounterByStep(inc);
        }

        private void XAddSide_OnRightTapped(object sender, RightTappedRoutedEventArgs e) { IncrementCounterByStep(GroupGeometryConstants.LargeInc); }

        private void XRemoveSide_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var inc = (MainPage.Instance.IsShiftPressed()) ? GroupGeometryConstants.MassiveInc : GroupGeometryConstants.SmallInc;
            IncrementCounterByStep(-1 * inc);
        }

        private void XRemoveSide_OnRightTapped(object sender, RightTappedRoutedEventArgs e) { IncrementCounterByStep(-1 * GroupGeometryConstants.LargeInc); }

        private void IncrementCounterByStep(int step)
        {
            int.TryParse(xSideCounter.Text, out var numSides);

            var isStar = xShapeOptionsDropdown.SelectedIndex == GroupGeometryConstants.CustomStarDropdownIndex;
            var lowerBound = isStar ? 5 : 3;
            var upperBound = isStar ? float.PositiveInfinity : 99;

            if (numSides + step < lowerBound || numSides + step > upperBound) return;
            numSides += step;
            xSideCounter.Text = numSides.ToString();
            _currentDocController?.GetDataDocument().SetSideCount(numSides);
        }

	    /*
         * Updates the value of the current color (as string) and updates color/opacity bindings
         */
		private void XGroupForegroundColorPicker_OnSelectedColorChanged(object sender, Color e)
	    {
			_currentColor = xGroupForegroundColorPicker.SelectedColor;
			//have to use a different key so that background color is 
		    _currentDocController?.SetField(KeyStore.GroupBackgroundColorKey, new TextController(e.ToString()), true);
		   
	    }

        private ToolTip _group;
        private ToolTip _ungroup;
        private ToolTip _color;

        private void SetUpToolTips()
        {
            var placementMode = PlacementMode.Bottom;
            const int offset = 5;

            _group = new ToolTip()
            {
                Content = "Group Items",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xGroup, _group);

            _ungroup = new ToolTip()
            {
                Content = "Ungroup Items",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xUngroup, _ungroup);

            _color = new ToolTip()
            {
                Content = "Font Color",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xFontColor, _color);


        }

        private void ShowAppBarToolTip(object sender, PointerRoutedEventArgs e)
        {
            if (sender is AppBarButton button && ToolTipService.GetToolTip(button) is ToolTip tip) tip.IsOpen = true;
            else if (sender is AppBarToggleButton toggleButton && ToolTipService.GetToolTip(toggleButton) is ToolTip toggleTip) toggleTip.IsOpen = true;
        }

        private void HideAppBarToolTip(object sender, PointerRoutedEventArgs e)
        {
            if (sender is AppBarButton button && ToolTipService.GetToolTip(button) is ToolTip tip) tip.IsOpen = false;
            else if (sender is AppBarToggleButton toggleButton && ToolTipService.GetToolTip(toggleButton) is ToolTip toggleTip) toggleTip.IsOpen = false;
        }
    }
}
