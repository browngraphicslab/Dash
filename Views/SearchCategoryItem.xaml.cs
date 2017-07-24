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
        public string ListDisplayMemberPath { get; set; }
        public string Icon { get; }
        public string Title { get; }
        public Color ContentBackGround { get { return xGridBackground.Color; } set { xGridBackground.Color = value; } }
        
        public SearchCategoryItem(string icon, string title, ObservableCollection<object> content, Action<object> action)
        {
            this.InitializeComponent();
            Icon = icon;
            Title = title;
            OriginalContent = content;
            NewContent = content;
            ListDisplayMemberPath = xList.DisplayMemberPath;
            xList.Tapped += delegate
            {
                action.Invoke(xList.SelectedItem);
            };
        }
    }
}
