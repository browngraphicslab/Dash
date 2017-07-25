using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;

namespace Dash
{
    public class EditableTextBlock
    {
        public TextBox Box { get; }
        public TextBlock Block { get; } = new TextBlock();

        public Canvas Container { get; } = new Canvas(); 

        public EditableTextBlock()
        {
            Box = new TextBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                TextWrapping = TextWrapping.Wrap,
                Visibility = Visibility.Collapsed
            };

            Box.ManipulationDelta += (s, e) => e.Handled = true; 

            Box.LostFocus += (s, e) =>
            {
                Box.Visibility = Visibility.Collapsed;
                Block.Visibility = Visibility.Visible;
            };

            Block.Tapped += (s, e) =>
            {
                e.Handled = true;
                Block.Visibility = Visibility.Collapsed;
                Box.Visibility = Visibility.Visible;
                Box.Focus(FocusState.Pointer);
            };

            Container.Children.Add(Block);
            Container.Children.Add(Box);
        }
    }
}
