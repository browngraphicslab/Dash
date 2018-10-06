using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Dash.Annotations;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class InkSubToolbar : UserControl
    {
        //public InkManager InkManager { get => DataContext as InkManager; set => DataContext = value; }

        public InkCanvas Canvas
        {
            get => xInkToolbar.TargetInkCanvas;
            set => xInkToolbar.TargetInkCanvas = value;
        }

        public InkToolbarToolButton ActiveTool
        {
            get => xInkToolbar.ActiveTool;
            set => xInkToolbar.ActiveTool = value;
        }

        public event EventHandler ActiveToolChanged;

        public InkSubToolbar()
        {
            InitializeComponent();
        }

        private void InkToolbar_OnActiveToolChanged(InkToolbar sender, object args)
        {
            ActiveToolChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
