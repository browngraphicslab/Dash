using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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
    public sealed partial class SettingsView : Page
    {
        public static SettingsView Instance { get; private set; }
        private DocumentController _settingsDoc;
        private readonly string _dbPath;
        private readonly string _pathToRestore;
        private BackupClearSafetyConfidence _clearConfidence;
        private DbEraseSafetyConfidence _eraseConfidence;
        private readonly IModelEndpoint<FieldModel> _endpoint;
        private int _newNumBackups;

        #region ENUMS

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

        public enum MouseFuncMode
        {
            Scroll,
            Zoom,
            Null,
        }

        #endregion

        #region BINDING VARIABLES 

        public bool NightModeOn
        {
            get => _settingsDoc.GetField<BoolController>(KeyStore.SettingsNightModeKey).Data; 
            private set
            {
                _settingsDoc.SetField<BoolController>(KeyStore.SettingsNightModeKey, value, true);
                MainPage.Instance.ThemeChange(value);
            }
        }

        public int NoteFontSize
        {
            get => (int) _settingsDoc.GetField<NumberController>(KeyStore.SettingsFontSizeKey).Data; 
            private set => _settingsDoc.SetField<NumberController>(KeyStore.SettingsFontSizeKey, value, true);
        }

        public MouseFuncMode MouseScrollOn
        {
            get => Enum.Parse<MouseFuncMode>(_settingsDoc.GetField<TextController>(KeyStore.SettingsMouseFuncKey).Data);
            private set => _settingsDoc.SetField<TextController>(KeyStore.SettingsMouseFuncKey, value.ToString(), true);
        }

        public int NumBackups
        {
            get => (int) _settingsDoc.GetField<NumberController>(KeyStore.SettingsNumBackupsKey).Data;
            set
            {
                var test1 = NumBackups;
                var prevNumBackups = (int) _settingsDoc.GetField<NumberController>(KeyStore.SettingsNumBackupsKey).Data;

                _settingsDoc.SetField<NumberController>(KeyStore.SettingsNumBackupsKey, value, true);

                var test2 = NumBackups;
                if (prevNumBackups <= value) return;

                //CONFIRM DELETE PARTIAL LIST OF BACKUPS
                for (var i = prevNumBackups; i > NumBackups; i--)
                {
                    var pathToDelete = _dbPath + ".bak" + i;
                    if (File.Exists(pathToDelete)) { File.Delete(pathToDelete); }
                }

                var suffix = NumBackups == 1 ? "" : "s";
                xNumBackupsSlider.Header = $"System is storing the {NumBackups} most recent backup" + suffix;
            }
        }

        public int BackupInterval
        {
            get => (int) _settingsDoc.GetField<NumberController>(KeyStore.SettingsBackupIntervalKey).Data;
            set => _settingsDoc.SetField<NumberController>(KeyStore.SettingsBackupIntervalKey, value, true);
        }

        #endregion

        #region CONSTRUCTOR

        public SettingsView()
        {
            InitializeComponent();

            Debug.Assert(Instance == null);
            Instance = this;
            _dbPath = ApplicationData.Current.LocalFolder.Path + "\\" + "dash.db";
            _pathToRestore = _dbPath + ".toRestore";
            _clearConfidence = BackupClearSafetyConfidence.Unconfident;
            _eraseConfidence = DbEraseSafetyConfidence.Unconfident;
            _endpoint = App.Instance.Container.GetRequiredService<IModelEndpoint<FieldModel>>();

            SetupSliderBounds();
        }

        #endregion

        #region SETTINGS AND BINDING PROCESSING

        //TODO Maybe handler should be removed in favor of having SettingsView have events for when the settings are changed.
        private void AddSettingsBinding<T>(FrameworkElement element, DependencyProperty prop, KeyController key, IValueConverter converter = null, string tag = null, DependencyPropertyChangedCallback handler = null, BindingMode mode = BindingMode.TwoWay) where T : FieldControllerBase
        {
            if (handler != null) element.RegisterPropertyChangedCallback(prop, handler);
            var binding = new FieldBinding<T>
            {
                Document = _settingsDoc,
                Key = key,
                Mode = mode,
                Converter = converter,
                Tag = tag
            };
            element.AddFieldBinding(prop, binding);
        }

        public void LoadSettings(DocumentController settingsDoc)
        {
            _settingsDoc = settingsDoc;

            Debug.WriteLine(settingsDoc.GetField<BoolController>(KeyStore.SettingsMouseFuncKey));

            AddSettingsBinding<BoolController>(xNightModeToggle, ToggleSwitch.IsOnProperty, KeyStore.SettingsNightModeKey, tag:"Settings Night Mode", handler: (sender, dp) => MainPage.Instance.ThemeChange(NightModeOn));
            AddSettingsBinding<NumberController>(xFontSizeSlider, RangeBase.ValueProperty, KeyStore.SettingsFontSizeKey, tag:"Settings Font Size");
            AddSettingsBinding<TextController>(xScrollRadio, ToggleButton.IsCheckedProperty, KeyStore.SettingsMouseFuncKey, new MouseModeEnumToBoolConverter(MouseFuncMode.Scroll), "Settings Scroll Radio");
            AddSettingsBinding<TextController>(xZoomRadio, ToggleButton.IsCheckedProperty, KeyStore.SettingsMouseFuncKey, new MouseModeEnumToBoolConverter(MouseFuncMode.Zoom), "Settings Zoom Radio");
            AddSettingsBinding<NumberController>(xNumBackupsSlider, RangeBase.ValueProperty, KeyStore.SettingsNumBackupsKey, handler: OnNumBackupsChanged, mode: BindingMode.OneWay);
            AddSettingsBinding<NumberController>(xBackupIntervalSlider, RangeBase.ValueProperty, KeyStore.SettingsBackupIntervalKey, handler: (sender, dp) =>
            {
                _endpoint.SetBackupInterval((int)xBackupIntervalSlider.Value * 1000);

                var interval = (int) xBackupIntervalSlider.Value;
                var numSec = interval % 60;
                var numMin = (interval - numSec) / 60;

                var suffix = numMin == 1 ? "" : "s";
                var minToDisplay = numMin == 0 ? "" : $" {numMin} minute" + suffix;
                var secToDisplay = numSec == 0 ? "" : $" {numSec} seconds";

                xBackupIntervalSlider.Header = "Backups overwritten every" + minToDisplay + secToDisplay;
            });
        }

        private void OnNumBackupsChanged(DependencyObject dependencyObject, DependencyProperty dependencyProperty)
        {
            _newNumBackups = (int)xNumBackupsSlider.Value;
            if (_newNumBackups < NumBackups)
            {
                xCorrectionPrompt.Text = (_newNumBackups == NumBackups - 1) ? $"Delete backup {NumBackups}?" : $"Delete backups {_newNumBackups + 1} through {NumBackups}?";
                SetPromptVisibility(Visibility.Visible);
            }
            else
            {
                NumBackups = _newNumBackups;
                SetPromptVisibility(Visibility.Collapsed);
            }

            var suffix = NumBackups == 1 ? "" : "s";
            xNumBackupsSlider.Header = $"System is storing the {NumBackups} most recent backup" + suffix;
        }

        #endregion

        #region HELPER METHODS

        private void SetupSliderBounds()
        {
            xBackupIntervalSlider.Minimum = DashConstants.MinBackupInterval;
            xBackupIntervalSlider.Value = DashConstants.DefaultBackupInterval;
            xBackupIntervalSlider.Maximum = DashConstants.MaxBackupInterval;

            xNumBackupsSlider.Minimum = DashConstants.MinNumBackups;
            xNumBackupsSlider.Value = DashConstants.DefaultNumBackups;
            xNumBackupsSlider.Maximum = DashConstants.MaxNumBackups;
        }

        private void SetPromptVisibility(Visibility status)
        {
            xCorrectDelete.Visibility = status;
            xCorrectionPrompt.Visibility = status;
            xCorrectReturnToSafetyIcon.Visibility = status;
        }

        #endregion

        #region RESTORE FROM BACKUP

        private async void Restore_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var backupPicker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.HomeGroup
            };
            for (var i = 1; i <= NumBackups; i++) { backupPicker.FileTypeFilter.Add(".bak" + i); }

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

        #endregion

        #region CLEAR ALL BACKUPS

        private void XClearButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (_clearConfidence == BackupClearSafetyConfidence.Unconfident)
            {
                _clearConfidence = BackupClearSafetyConfidence.Intermediate;
                xReturnToSafetyIcon.Visibility = Visibility.Visible;
                xSafety.Visibility = Visibility.Visible;
            }
            //CONFIRM DELETE ALL BACKUPS
            else if (_clearConfidence == BackupClearSafetyConfidence.Confident)
            {
                for (var i = 1; i <= NumBackups; i++)
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

        #endregion

        #region ERASE DATABASE

        private async void XEraseDbButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (_eraseConfidence == DbEraseSafetyConfidence.Unconfident)
            {
                _eraseConfidence = DbEraseSafetyConfidence.Intermediate;
                xEraseReturnToSafetyIcon.Visibility = Visibility.Visible;
                xEraseSafety.Visibility = Visibility.Visible;
            }
            //CONFIRM ERASE DATABASE
            else if (_eraseConfidence == DbEraseSafetyConfidence.Confident)
            {
                _endpoint.DeleteAllDocuments(null, null);
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

        #endregion

        #region UPDATE NUM BACKUPS

        private void XCorrectReturnToSafetyIcon_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            SetPromptVisibility(Visibility.Collapsed);
            xNumBackupsSlider.Value = NumBackups;
        }

        private void XCorrectDelete_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            NumBackups = _newNumBackups;
            _newNumBackups = 0;
            SetPromptVisibility(Visibility.Collapsed);
        }

        #endregion

    }
}
