﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Dash
{
    /// <summary>
    /// Settings pane 
    /// </summary>
    public sealed partial class SettingsView : Page, INotifyPropertyChanged 
    {
        public static SettingsView Instance { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Binding variables 
        private bool _nightModeOn = false; 
        public bool NightModeOn
        {
            get => _nightModeOn; 
            private set {
                _nightModeOn = value;
                //NotifyPropertyChanged();
                MainPage.Instance.ThemeChange();
            }
        }

        private int _fontSize = 16;
        public int NoteFontSize
        {
            get => _fontSize; 
            private set {
                _fontSize = value;
                NotifyPropertyChanged(); 
            }
        }

        private bool _mouseScroll = true;
        public bool MouseScrollOn
        {
            get => _mouseScroll; 
            private set { _mouseScroll = value; }
        }

        #endregion

        public SettingsView()
        {
            InitializeComponent();

            Debug.Assert(Instance == null);
            Instance = this;
        }
    }
}
