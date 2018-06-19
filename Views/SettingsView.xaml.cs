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
using Microsoft.Extensions.DependencyInjection;
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
        private BackupClearSafetyConfidence _clearConfidence;
        private DbEraseSafetyConfidence _eraseConfidence;
        private int _numBackups = DashConstants.DefaultNumBackups;
        private int _newNumBackups = 0;
        private bool _showPrompt = false;

        public enum BackupClearSafetyConfidence
        {
            Unconfident,
            Intermediate,
            Confident
        }
    
        public enum DbEraseSafetyConfidence
        {
            Unconfident,
            Intermediate,
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
            _clearConfidence = BackupClearSafetyConfidence.Unconfident;
            _eraseConfidence = DbEraseSafetyConfidence.Unconfident;

            SetupSliderBounds();
        }

        private void SetupSliderBounds()
        {
            xBackupIntervalSlider.Minimum = DashConstants.MinBackupInterval;
            xBackupIntervalSlider.IntermediateValue = DashConstants.DefaultBackupInterval;
            xBackupIntervalSlider.Value = DashConstants.DefaultBackupInterval;
            xBackupIntervalSlider.Maximum = DashConstants.MaxBackupInterval;

            xNumBackupsSlider.Minimum = DashConstants.MinNumBackups;
            xNumBackupsSlider.IntermediateValue = DashConstants.DefaultNumBackups;
            xNumBackupsSlider.Value = DashConstants.DefaultNumBackups;
            xNumBackupsSlider.Maximum = DashConstants.MaxNumBackups;
        }

        private async void Restore_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var backupPicker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.HomeGroup
            };
            for (var i = 1; i <= _numBackups; i++) { backupPicker.FileTypeFilter.Add(".bak" + i); }

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
            if (_clearConfidence == BackupClearSafetyConfidence.Unconfident)
            {
                _clearConfidence = BackupClearSafetyConfidence.Intermediate;
                xReturnToSafetyIcon.Visibility = Visibility.Visible;
                xSafety.Visibility = Visibility.Visible;
            } else if (_clearConfidence == BackupClearSafetyConfidence.Confident)
            {
                for (var i = 1; i <= _numBackups; i++)
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
            _clearConfidence = BackupClearSafetyConfidence.Unconfident;
            xClearIcon.Source = new BitmapImage(new Uri("ms-appx:///Assets/delete.png"));
            xReturnToSafetyIcon.Visibility = Visibility.Collapsed;
            xSafety.Visibility = Visibility.Collapsed;
            xSafety.IsOn = true;
        }

        private void ToggleSwitch_OnToggled(object sender, RoutedEventArgs e)
        {
            if (_clearConfidence == BackupClearSafetyConfidence.Intermediate)
            {
                xClearIcon.Source = new BitmapImage(new Uri("ms-appx:///Assets/seriousdelete.png"));
                _clearConfidence = BackupClearSafetyConfidence.Confident;
            }
            else if (_clearConfidence == BackupClearSafetyConfidence.Confident)
            {
                xClearIcon.Source = new BitmapImage(new Uri("ms-appx:///Assets/delete.png"));
                _clearConfidence = BackupClearSafetyConfidence.Intermediate;
            }
        }

        private async void XEraseDbButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (_eraseConfidence == DbEraseSafetyConfidence.Unconfident)
            {
                _eraseConfidence = DbEraseSafetyConfidence.Intermediate;
                xEraseReturnToSafetyIcon.Visibility = Visibility.Visible;
                xEraseSafety.Visibility = Visibility.Visible;
            }
            else if (_eraseConfidence == DbEraseSafetyConfidence.Confident)
            {
                App.Instance.Container.GetRequiredService<IModelEndpoint<FieldModel>>().DeleteAllDocuments(null, null);
                ResetEraseButton();

                await CoreApplication.RequestRestartAsync("");
            }
        }

        private void XEraseReturnToSafetyIcon_OnTapped(object sender, TappedRoutedEventArgs e) { ResetEraseButton(); }

        private void ResetEraseButton()
        {
            _eraseConfidence = DbEraseSafetyConfidence.Unconfident;
            xEraseDbIcon.Source = new BitmapImage(new Uri("ms-appx:///Assets/eraser.png"));
            xEraseReturnToSafetyIcon.Visibility = Visibility.Collapsed;
            xEraseSafety.Visibility = Visibility.Collapsed;
            xEraseSafety.IsOn = true;
        }

        private void XEraseSafety_OnToggled(object sender, RoutedEventArgs e)
        {
            if (_eraseConfidence == DbEraseSafetyConfidence.Intermediate)
            {
                xEraseDbIcon.Source = new BitmapImage(new Uri("ms-appx:///Assets/seriousdelete.png"));
                _eraseConfidence = DbEraseSafetyConfidence.Confident;
            }
            else if (_eraseConfidence == DbEraseSafetyConfidence.Confident)
            {
                xEraseDbIcon.Source = new BitmapImage(new Uri("ms-appx:///Assets/eraser.png"));
                _eraseConfidence = DbEraseSafetyConfidence.Intermediate;
            }
        }

        private void XBackupIntervalSlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            App.Instance.Container.GetRequiredService<IModelEndpoint<FieldModel>>().SetBackupInterval((int) xBackupIntervalSlider.Value * 1000);
        }

        private void XNumBackupsSlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (!_showPrompt)
            {
                _showPrompt = true;
                return;
            }
            _newNumBackups = (int) xNumBackupsSlider.Value;
            if (_newNumBackups < _numBackups)
            {
                xCorrectionPrompt.Text = $"Delete backups {_newNumBackups + 1} through {_numBackups}?";
                SetPromptVisibility(Visibility.Visible);
            }
            else
            {
                App.Instance.Container.GetRequiredService<IModelEndpoint<FieldModel>>().SetNumBackups(_newNumBackups);
                _numBackups = _newNumBackups;
                SetPromptVisibility(Visibility.Collapsed);
            }
        }

        private void XCorrectReturnToSafetyIcon_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            SetPromptVisibility(Visibility.Collapsed);
            _showPrompt = false;
            xNumBackupsSlider.Value = _numBackups;
        }

        private void XCorrectDelete_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            for (var i = _numBackups; i > _newNumBackups; i--)
            {
                var pathToDelete = _dbPath + ".bak" + i;
                if (File.Exists(pathToDelete)) { File.Delete(pathToDelete); }
            }
            App.Instance.Container.GetRequiredService<IModelEndpoint<FieldModel>>().SetNumBackups(_newNumBackups);
            _numBackups = _newNumBackups;
            _newNumBackups = 0;
            SetPromptVisibility(Visibility.Collapsed);
        }

        private void SetPromptVisibility(Visibility status)
        {
            xCorrectDelete.Visibility = status;
            xCorrectionPrompt.Visibility = status;
            xCorrectReturnToSafetyIcon.Visibility = status;
        }
    }
}
