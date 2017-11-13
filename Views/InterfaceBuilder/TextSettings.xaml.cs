using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using DashShared;
using FontWeights = Windows.UI.Text.FontWeights;
using System.Collections.Generic;
using Dash.Converters;
using Windows.UI;
using System.Reflection;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;
using Visibility = Windows.UI.Xaml.Visibility;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class TextSettings : UserControl
    {
        private ObservableCollection<string> _fontWeights = new ObservableCollection<string>();
        ObservableCollection<NamedColor> colors { get; set; }
        public TextSettings()
        {
            this.InitializeComponent();
            this.AddColors();

        }

        public TextSettings(DocumentController editedLayoutDocument, Context context) : this()
        {
            if (editedLayoutDocument.GetField(TextingBox.BackgroundColorKey) == null)
                editedLayoutDocument.SetField(TextingBox.BackgroundColorKey, new TextFieldModelController("white"), true);
            xSizeRow.Children.Add(new SizeSettings(editedLayoutDocument, context));
            xPositionRow.Children.Add(new PositionSettings(editedLayoutDocument, context));
            xAlignmentRow.Children.Add(new AlignmentSettings(editedLayoutDocument,context));
            BindFontWeight(editedLayoutDocument, context);
            BindFontSize(editedLayoutDocument, context);
            BindFontAlignment(editedLayoutDocument, context);
            BindBackgroundColor(editedLayoutDocument, context);
        }

        private void BindFontAlignment(DocumentController docController, Context context)
        {
            var textAlignmentController =
                docController.GetDereferencedField(TextingBox.TextAlignmentKey, context) as NumberFieldModelController;
            Debug.Assert(textAlignmentController != null);

            var fontAlignmentBinding = new Binding()
            {
                Source = textAlignmentController,
                Path = new PropertyPath(nameof(textAlignmentController.Data)),
                Mode = BindingMode.TwoWay,
                // Converter = new IntToTextAlignmentConverter()
            };

            xAlignmentListView.SetBinding(ListView.SelectedIndexProperty, fontAlignmentBinding);
            xAlignmentListView.SelectionChanged += delegate (object sender, SelectionChangedEventArgs args) { Debug.WriteLine(xAlignmentListView.SelectedIndex); };
        }


        private void BindBackgroundColor(DocumentController docController, Context context)
        {
            var backColorController =
                    docController.GetDereferencedField(TextingBox.BackgroundColorKey, context) as TextFieldModelController;
            Debug.Assert(backColorController != null);
            var backgroundBinding = new FieldBinding<TextFieldModelController>()
            {
                Key = TextingBox.BackgroundColorKey,
                Document = docController,
                Converter = new StringToNamedColorConverter(),
                Mode = BindingMode.TwoWay,
                Context = context
            };
            xBackgroundColorComboBox.AddFieldBinding(ComboBox.SelectedItemProperty, backgroundBinding);
        }

        private void BindFontWeight(DocumentController docController, Context context)
        {
            var fontWeightController =
                    docController.GetDereferencedField(TextingBox.FontWeightKey, context) as TextFieldModelController;
            Debug.Assert(fontWeightController != null);

            _fontWeights = new ObservableCollection<string>()
            {
                "Black", //FontWeights.Black.Weight,
                "Bold", //FontWeights.Bold.Weight,
                "Normal", // FontWeights.Normal.Weight,
                "Light" //FontWeights.Light.Weight
            };
            xFontWeightBox.ItemsSource = _fontWeights;
            var FontWeightBinding = new Binding()
            {
                Source = fontWeightController,
                Path = new PropertyPath(nameof(fontWeightController.Data)),
                Mode = BindingMode.TwoWay,
            };

            xFontWeightBox.SetBinding(ComboBox.SelectedValueProperty, FontWeightBinding);
        }

        private void BindFontSize(DocumentController docController, Context context)
        {
            var fontSizeBinding = new FieldBinding<NumberFieldModelController>()
            {
                Key = TextingBox.FontSizeKey,
                Document = docController,
                Mode = BindingMode.TwoWay,
                Context = context,
                Converter = new StringToDoubleConverter(1)
            };
            xFontSizeTextBox.AddFieldBinding(TextBox.TextProperty, fontSizeBinding);
        }

        private void ColorSelectionChanged(DocumentController docController, Context context)
        {
            var field = docController.GetDereferencedField<TextFieldModelController>(TextingBox.BackgroundColorKey, context);
            var col = (xBackgroundColorComboBox.SelectedItem as NamedColor).Color;
            field.SetValue(col.ToString());
        }


        private void AddColors()
        {
            if (colors == null) colors = new ObservableCollection<NamedColor>();
            foreach (var color in typeof(Colors).GetRuntimeProperties())
            {
                colors.Add(new NamedColor() { Name = color.Name, Color = (Color)color.GetValue(null) });
            }
        }

        #region ValueSlider

        private void XMovementDetectionGrid_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender == xFontSizeMovementDetectionGrid)
            {
                xFontSizeTextBox.Focus(FocusState.Programmatic);
                xFontSizeMovementDetectionGrid.IsHitTestVisible = false;
                xFontSizeMovementDetectionGrid.Visibility = Visibility.Collapsed;

            }
        }

        private void XMovementDetectionGrid_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var grid = sender as Panel;
            var children = grid?.Children;
            if (children != null)
            {
                foreach (var child in children)
                {
                    this.CreateAndRunOpacityAnimation(child, 0, 0.5);
                    if ((string)(child as Border)?.Tag == "Deduct")
                    {
                        this.CreateAndRunRepositionAnimation(child, 100);
                    }
                    else if ((string)(child as Border)?.Tag == "Increment")
                    {
                        this.CreateAndRunRepositionAnimation(child, -100);
                    }
                }
            }
            e.Handled = true;
        }


        private void XMovementDetectionGrid_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            this.HideDeductAndIncrement(sender);
            e.Handled = true;
        }

        private void XMovementDetectionGrid_OnPointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            // event not firing?
            this.HideDeductAndIncrement(sender);
            e.Handled = true;
        }

        private void XFontSizeTextBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (sender == xFontSizeTextBox)
            {
                xFontSizeMovementDetectionGrid.IsHitTestVisible = true;
                xFontSizeMovementDetectionGrid.Visibility = Visibility.Visible;
            }
        }

        private void CreateAndRunOpacityAnimation(UIElement target, double from, double to)
        {
            Duration duration = new Duration(TimeSpan.FromSeconds(0.3));

            DoubleAnimation opacityAnimation = new DoubleAnimation()
            {
                SpeedRatio = 2,
                From = from,
                To = to,
                Duration = duration,
                EnableDependentAnimation = true
            };
            Storyboard opacityStoryboard = new Storyboard()
            {
                Duration = duration
            };
            opacityStoryboard.Children.Add(opacityAnimation);
            Storyboard.SetTarget(opacityAnimation, target);
            Storyboard.SetTargetProperty(opacityAnimation, "Opacity");
            opacityStoryboard.Begin();
        }

        private void CreateAndRunRepositionAnimation(UIElement target, double horizontalOffset)
        {
            Duration duration = new Duration(TimeSpan.FromSeconds(0.5));

            RepositionThemeAnimation repositionAnimation = new RepositionThemeAnimation()
            {
                SpeedRatio = 1.3,
                FromHorizontalOffset = horizontalOffset,
                Duration = duration
            };
            Storyboard repositionStoryboard = new Storyboard()
            {
                Duration = duration
            };
            repositionStoryboard.Children.Add(repositionAnimation);
            Storyboard.SetTarget(repositionAnimation, target);
            repositionStoryboard.Begin();
        }

        private void XMovementDetectionGrid_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var deltaX = e.Delta.Translation.X;
            if (deltaX > 0)
            {
                if (sender == xFontSizeMovementDetectionGrid)
                {
                    (xDeduct.Child as TextBlock).FontSize = 20;
                    this.CreateAndRunOpacityAnimation(xDeduct, xDeduct.Opacity, 0.5);
                    (xIncrement.Child as TextBlock).FontSize = 26;
                    this.CreateAndRunOpacityAnimation(xIncrement, xIncrement.Opacity, 1);
                    double currentValue = 0;
                    if (!xFontSizeTextBox.Text.Equals(string.Empty))
                    {
                        currentValue = double.Parse(xFontSizeTextBox.Text);
                    }
                    xFontSizeTextBox.SetValue(TextBox.TextProperty,
                        (currentValue + 1).ToString());
                }
            }
            if (deltaX < 0)
            {
                if (sender == xFontSizeMovementDetectionGrid)
                {
                    (xIncrement.Child as TextBlock).FontSize = 20;
                    this.CreateAndRunOpacityAnimation(xIncrement, xIncrement.Opacity, 0.5);
                    (xDeduct.Child as TextBlock).FontSize = 26;
                    this.CreateAndRunOpacityAnimation(xDeduct, xDeduct.Opacity, 1);
                    double currentValue = 0;
                    if (!xFontSizeTextBox.Text.Equals(string.Empty))
                    {
                        currentValue = double.Parse(xFontSizeTextBox.Text);
                    }
                    if (currentValue > 4)
                    {
                        xFontSizeTextBox.SetValue(TextBox.TextProperty,
                            (currentValue - 1).ToString());
                    }
                }
            }
            e.Handled = true;
        }

        private void XMovementDetectionGrid_OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            this.HideDeductAndIncrement(sender);
            e.Handled = true;
        }

        private void HideDeductAndIncrement(object sender)
        {
            var grid = sender as Panel;
            var children = grid?.Children;
            if (children != null)
            {
                foreach (var child in children)
                {
                    this.CreateAndRunOpacityAnimation(child, child.Opacity, 0);
                }
            }
            (xIncrement.Child as TextBlock).FontSize = 20;
            (xDeduct.Child as TextBlock).FontSize = 20;
        }

        #endregion

        private void SettingsPaneBlock_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
