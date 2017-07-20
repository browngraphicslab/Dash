﻿using System;
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
        public TextBlock Block { get; }

        //public string Data { get; set; } 

        //public event RoutedEventHandler GotFocus;
        //public event RoutedEventHandler LostFocus; 

        public EditableTextBlock()
        {
            Block = new TextBlock();
            Box = new TextBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                TextWrapping = TextWrapping.Wrap, 
                //Visibility = Visibility.Collapsed
            };
            Block.Visibility = Visibility.Collapsed; 

            Box.LostFocus += (s, e) =>
            {
                Box.Visibility = Visibility.Collapsed;
                Block.Visibility = Visibility.Visible;
            };

            Block.Tapped += (s, e) =>
            {
                Block.Visibility = Visibility.Collapsed;
                Box.Visibility = Visibility.Visible;
            };

            Box.GotFocus += (s, e) => Box.ManipulationMode = ManipulationModes.None;
            Box.LostFocus += (s, e) => Box.ManipulationMode = ManipulationModes.All;
        }

        public FrameworkElement MakeView()
        {
            Grid container = new Grid();
            container.Children.Add(Block);
            container.Children.Add(Box);
            return container; 
        }
    }
}
