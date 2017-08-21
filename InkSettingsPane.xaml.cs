using System;
using System.Collections.Generic;
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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class InkSettingsPane : UserControl
    {
        public InkSettingsPane()
        {
            this.InitializeComponent();
            var list = new List<SolidColorBrush>();
            var rand = new Random();
            for(int i = 0; i < 9; i++)
            {
                list.Add(new SolidColorBrush(Color.FromArgb((byte) rand.Next(0, 255), (byte)rand.Next(0, 255), (byte)rand.Next(0, 255), (byte)rand.Next(0, 255))));
            }
            ColorPickerGridView.ItemsSource = list;
        }
    }
}
