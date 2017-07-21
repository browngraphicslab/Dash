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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class SearchCategoryItem : UserControl
    {
        public ObservableCollection<object> OriginalContent { get; }
        public ObservableCollection<object> NewContent { get; set; }
        public PivotItem Item { get { return xPivot; } }
        public Brush HeaderBackground { get { return xBorder.Background; } set { xBorder.Background = value; } }
        public Color ContentBackGround { get { return xGridBackground.Color; } set { xGridBackground.Color = value; } }
        public Brush HeaderForeground { get; set; }
        public SearchCategoryItem(string icon, string title, ObservableCollection<object> content, Action<object> action)
        {
            this.InitializeComponent();
            this.SetHeader(icon, title);
            OriginalContent = content;
            NewContent = content;
            xList.Tapped += delegate
            {
                action.Invoke(xList.SelectedItem);
            };
        }

        private void SetHeader(string icon, string title)
        {
            // text part of the title
            var pivotTitle = new TextBlock();
            pivotTitle.Text = title;
            pivotTitle.Foreground = new SolidColorBrush(Colors.White);
            pivotTitle.HorizontalAlignment = HorizontalAlignment.Center;

            // icon part of the title (can be empty)
            var pivotIcon = new TextBlock();
            pivotIcon.Text = icon;
            pivotIcon.Foreground = new SolidColorBrush(Colors.White);
            pivotIcon.HorizontalAlignment = HorizontalAlignment.Center;

            // arrange layout of the header according to whether or not icon is empty
            if (icon == string.Empty)
            {
                xBorder.Child = pivotTitle;
                pivotTitle.FontSize = 12;
            }
            else
            {
                var stack = new StackPanel();
                stack.Orientation = Orientation.Vertical;
                pivotIcon.FontSize = 20;
                pivotTitle.FontSize = 10;
                pivotTitle.VerticalAlignment = VerticalAlignment.Center;
                stack.Children.Add(pivotIcon);
                stack.Children.Add(pivotTitle);
                xBorder.Child = stack;
            }
        }
    }
}
