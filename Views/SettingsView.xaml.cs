using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using DashShared;
using Visibility = Windows.UI.Xaml.Visibility;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Dash
{
    /// <summary>
    /// Settings pane 
    /// </summary>
    public sealed partial class SettingsView : Page, INotifyPropertyChanged
    {
        private readonly string _dbPath;
        private readonly string _pathToRestore;
        private BackupClearSafetyConfidence _confidence;

        public enum BackupClearSafetyConfidence
        {
            Unconfident,
            Confident
        }

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
                MainPage.Instance.ThemeChange(value);
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
            _dbPath = ApplicationData.Current.LocalFolder.Path + "\\" + "dash.db";
            _pathToRestore = _dbPath + ".toRestore";
            _confidence = BackupClearSafetyConfidence.Unconfident;
        }

        private async void Restore_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var backupPicker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.HomeGroup
            };
            for (var i = 1; i <= DashConstants.NumBackupsToSave; i++) { backupPicker.FileTypeFilter.Add(".bak" + i); }

            var selectedBackup = await backupPicker.PickSingleFileAsync();
            if (selectedBackup == null) return;

            var backupPath = _dbPath + ".bak";

            var selectedPath = selectedBackup.Path;
            File.Copy(selectedPath, _pathToRestore);
            
            if (int.TryParse(selectedPath.Last().ToString(), out var numSelected))
            {
                for (var i = numSelected - 1; i >= 1; i--)
                {
                    var source = backupPath + i;
                    var destination = backupPath + (i + 1);
                    if (File.Exists(source)) { File.Copy(source, destination, true); }
                }
                File.Copy(_dbPath, backupPath + 1, true);
            }

            File.Copy(_pathToRestore, _dbPath, true);
            File.Delete(_pathToRestore);

            await CoreApplication.RequestRestartAsync("");
        }

        private void XClearButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (_confidence == BackupClearSafetyConfidence.Unconfident)
            {
                xClearIcon.Source = new BitmapImage(new Uri("ms-appx:///Assets/seriousdelete.png"));
                _confidence = BackupClearSafetyConfidence.Confident;
                xReturnToSafetyIcon.Visibility = Visibility.Visible;
            } else if (_confidence == BackupClearSafetyConfidence.Confident && MainPage.Instance.IsCtrlPressed())
            {
                for (var i = 1; i <= DashConstants.NumBackupsToSave; i++)
                {
                    var pathToDelete = _dbPath + ".bak" + i;
                    if (File.Exists(pathToDelete)) { File.Delete(pathToDelete); }
                }

                ResetDeleteButton();
            }
        }

        private void XReturnToSafetyIcon_OnTapped(object sender, TappedRoutedEventArgs e) { ResetDeleteButton(); }

        private void ResetDeleteButton()
        {
            _confidence = BackupClearSafetyConfidence.Unconfident;
            xClearIcon.Source = new BitmapImage(new Uri("ms-appx:///Assets/delete.png"));
            xReturnToSafetyIcon.Visibility = Visibility.Collapsed;
        }
    }
}
