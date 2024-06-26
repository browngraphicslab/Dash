﻿using System;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class Ruler : UserControl
    {
        public Orientation Orientation { get; set; }

        public Color Fill
        {
            get => (xGrid.Background as SolidColorBrush).Color;
            set => xGrid.Background = new SolidColorBrush(value);
        }

        public double Length
        {
            get
            {
                switch (Orientation)
                {
                    case Orientation.Vertical:
                        return xGrid.Height;
                    case Orientation.Horizontal:
                        return xGrid.Width;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            set
            {
                switch (Orientation)
                {
                    case Orientation.Vertical:
                        xGrid.Height = value;
                        break;
                    case Orientation.Horizontal:
                        xGrid.Width = value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public double Thickness
        {
            get
            {
                switch (Orientation)
                {
                    case Orientation.Vertical:
                        return xGrid.Width;
                    case Orientation.Horizontal:
                        return xGrid.Height;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            set
            {
                switch (Orientation)
                {
                    case Orientation.Vertical:
                        xGrid.Width = value;
                        break;
                    case Orientation.Horizontal:
                        xGrid.Height = value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public double Ticks
        {
            get => xGrid.Children.Count;
            set
            {
                // calculate the offset between each tick
                var offset = Length / (value + 1);
                var extraOffset = offset / 2;
                for (var i = 0; i <= value; i++)
                {
                    // every 5th offset, let the line be longer, every tenth, even longer so
                    var length = i % 5 == 0 ? i % 10 == 0 ? Thickness * 0.75 : Thickness / 2 : Thickness / 3;
                    Line line;
                    switch (Orientation)
                    {
                        // create either a horizontal or vertical line based on the orientation
                        case Orientation.Vertical:
                            line = new Line
                            {
                                X1 = 0,
                                X2 = length,
                                Y1 = i * offset + extraOffset,
                                Y2 = i * offset + extraOffset
                            };
                            break;
                        case Orientation.Horizontal:
                            line = new Line
                            {
                                X1 = i * offset + extraOffset,
                                X2 = i * offset + extraOffset,
                                Y1 = 0,
                                Y2 = length
                            };
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    line.Stroke = new SolidColorBrush(Colors.Black);
                    // let the stroke be thicker every 5th tick and thicker still every 10th
                    line.StrokeThickness = i % 5 == 0 ? i % 10 == 0 ? offset / 5 : offset / 7.5 : offset / 10;
                    xGrid.Children.Add(line);
                }
            }
        }

        public Ruler()
        {
            this.InitializeComponent();
        }
    }
}
