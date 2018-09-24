using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Dash.Annotations;
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
        public static SettingsView Instance { get; private set; }
        private DocumentController _settingsDoc;
        private readonly string _dbPath;
        private readonly string _pathToRestore;
        private BackupClearSafetyConfidence _clearConfidence;
        private DbEraseSafetyConfidence _eraseConfidence;
        private BackgroundImageState _lastNonCustom = BackgroundImageState.Grid;
        private readonly IModelEndpoint<FieldModel> _endpoint;
        private int _newNumBackups;

        private const string Grid = "ms-appx:///Assets/transparent_grid_tilable.png";
        private const string Line = "ms-appx:///Assets/transparent_line_tilable.png";
        private const string Dot = "ms-appx:///Assets/transparent_dot_tilable.png";
        private const string Blank = "ms-appx:///Assets/transparent_blank_tilable.png";

        private List<StackPanel> _mainPanels;

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

        public enum WebpageLayoutMode
        {
           RTF,
           HTML,
           Default,
           Null
        }

        public enum BackgroundImageState
        {
            Grid,
            Line,
            Dot,
            Blank,
            Custom
        }

        public readonly Dictionary<BackgroundImageState, string> EnumToPathDict = new Dictionary<BackgroundImageState, string>
        {
            [BackgroundImageState.Grid] = Grid,
            [BackgroundImageState.Line] = Line,
            [BackgroundImageState.Dot] = Dot,
            [BackgroundImageState.Blank] = Blank,
        };

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
            set => _settingsDoc.SetField<TextController>(KeyStore.SettingsMouseFuncKey, value.ToString(), true);
        }

        public WebpageLayoutMode WebpageLayout
        {
            get => Enum.Parse<WebpageLayoutMode>(_settingsDoc.GetField<TextController>(KeyStore.SettingsWebpageLayoutKey).Data);
            set => _settingsDoc.SetField<TextController>(KeyStore.SettingsWebpageLayoutKey, value.ToString(), true);
        }

        public BackgroundImageState ImageState
        {
            get => Enum.Parse<BackgroundImageState>(_settingsDoc.GetField<TextController>(KeyStore.BackgroundImageStateKey).Data);
            set => _settingsDoc.SetField<TextController>(KeyStore.BackgroundImageStateKey, value.ToString(), true);
        }

        public string CustomImagePath
        {
            get => _settingsDoc.GetField<TextController>(KeyStore.CustomBackgroundImagePathKey)?.Data;
            set => _settingsDoc.SetField<TextController>(KeyStore.CustomBackgroundImagePathKey, value, true);
        }

        public float BackgroundImageOpacity
        {
            get => (float) _settingsDoc.GetField<NumberController>(KeyStore.BackgroundImageOpacityKey).Data;
            set => _settingsDoc.SetField<NumberController>(KeyStore.BackgroundImageOpacityKey, value, true);
        }

        public bool NoUpperLimit
        {
            get => _settingsDoc.GetField<BoolController>(KeyStore.SettingsUpwardPanningKey).Data;
            set => _settingsDoc.SetField<BoolController>(KeyStore.SettingsUpwardPanningKey, value, true);
        }

        public bool MarkdownEditOn
        {
            get => _settingsDoc.GetField<BoolController>(KeyStore.SettingsMarkdownModeKey).Data;
            set => _settingsDoc.SetField<BoolController>(KeyStore.SettingsMarkdownModeKey, value, true);
        }

        public int NumBackups
        {
            get => (int) _settingsDoc.GetField<NumberController>(KeyStore.SettingsNumBackupsKey).Data;
            set
            {
                var prevNumBackups = (int) _settingsDoc.GetField<NumberController>(KeyStore.SettingsNumBackupsKey).Data;

                _settingsDoc.SetField<NumberController>(KeyStore.SettingsNumBackupsKey, value, true);
                if (prevNumBackups <= value) return;

                //CONFIRM DELETE PARTIAL LIST OF BACKUPS
                for (var i = prevNumBackups; i > NumBackups; i--)
                {
                    var pathToDelete = _dbPath + ".bak" + i;
                    if (File.Exists(pathToDelete)) { File.Delete(pathToDelete); }
                }

                xNumBackupDisplay.Text = NumBackups.ToString();
            }
        }

        public int BackupInterval
        {
            get => (int) _settingsDoc.GetField<NumberController>(KeyStore.SettingsBackupIntervalKey).Data;
            set => _settingsDoc.SetField<NumberController>(KeyStore.SettingsBackupIntervalKey, value, true);
        }

        public string UserName
        {
            get => _settingsDoc.GetField<TextController>(KeyStore.AuthorKey).Data;
            set => _settingsDoc.SetField<TextController>(KeyStore.AuthorKey, value, true);
        }

        #endregion

        #region CONSTRUCTOR

        public SettingsView()
        {
            InitializeComponent();

            Debug.Assert(Instance == null);
            Instance = this;

            //WebpageLayout = WebpageLayoutMode.Default;

            _dbPath = ApplicationData.Current.LocalFolder.Path + "\\" + "dash.db";
            _pathToRestore = _dbPath + ".toRestore";
            _clearConfidence = BackupClearSafetyConfidence.Unconfident;
            _eraseConfidence = DbEraseSafetyConfidence.Unconfident;
            _endpoint = App.Instance.Container.GetRequiredService<IModelEndpoint<FieldModel>>();

            _mainPanels = new List<StackPanel>
            {
                xCustomizeControlsContent,
                xCustomizeDisplayContent,
                xManageBackupsContent
            };

            SetupSliderBounds();
        }

        private async Task<bool> TrySetUserPath()
        {
            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            var file = await picker.PickSingleFileAsync();
            if (file == null)
            {
                if (CustomImagePath != null) { CollectionFreeformView.BackgroundImage = CustomImagePath; return false; }
                ImageState = _lastNonCustom;
                return false;
            }

            CollectionFreeformView.BackgroundImage = file.Path;
            CustomImagePath = file.Path;

            return true;
        }

        #endregion

        #region SETTINGS AND BINDING PROCESSING

        //TODO Maybe handler should be removed in favor of having SettingsView have events for when the settings are changed.
        private void AddSettingsBinding<T>(FrameworkElement element, DependencyProperty prop, KeyController key, IValueConverter converter = null, string tag = null, DependencyPropertyChangedCallback handler = null, BindingMode mode = BindingMode.TwoWay) where T : FieldControllerBase
        {
            var binding = new FieldBinding<T>
            {
                Document = _settingsDoc,
                Key = key,
                Mode = mode,
                Converter = converter,
                Tag = tag
            };
            element.AddFieldBinding(prop, binding);

            if (handler != null) element.RegisterPropertyChangedCallback(prop, handler);
            if (element != xCustomRadio) handler?.Invoke(element, prop);
        }

        public void LoadSettings(DocumentController settingsDoc)
        {
            _settingsDoc = settingsDoc;

            var binding = new FieldBinding<TextController>
            {
                Document = _settingsDoc,
                Key = KeyStore.BackgroundImageStateKey,
                Mode = BindingMode.OneWay,
                Converter = new RadioEnumToVisibilityConverter(BackgroundImageState.Custom)
            };
            xCustomizeButton.AddFieldBinding(VisibilityProperty, binding);

            AddSettingsBinding<BoolController>(xNightModeToggle, ToggleSwitch.IsOnProperty, KeyStore.SettingsNightModeKey, handler: (sender, dp) => MainPage.Instance.ThemeChange(NightModeOn));
            AddSettingsBinding<NumberController>(xFontSizeSlider, RangeBase.ValueProperty, KeyStore.SettingsFontSizeKey);

            AddSettingsBinding<TextController>(xScrollRadio, ToggleButton.IsCheckedProperty, KeyStore.SettingsMouseFuncKey, new RadioEnumToBoolConverter(MouseFuncMode.Scroll));
            AddSettingsBinding<TextController>(xZoomRadio, ToggleButton.IsCheckedProperty, KeyStore.SettingsMouseFuncKey, new RadioEnumToBoolConverter(MouseFuncMode.Zoom));


            AddSettingsBinding<TextController>(xHTMLImport, ToggleButton.IsCheckedProperty, KeyStore.SettingsWebpageLayoutKey, new RadioEnumToBoolConverter(WebpageLayoutMode.HTML));
            AddSettingsBinding<TextController>(xRTFImport, ToggleButton.IsCheckedProperty, KeyStore.SettingsWebpageLayoutKey, new RadioEnumToBoolConverter(WebpageLayoutMode.RTF));
            AddSettingsBinding<TextController>(xDefaultImport, ToggleButton.IsCheckedProperty, KeyStore.SettingsWebpageLayoutKey, new RadioEnumToBoolConverter(WebpageLayoutMode.Default));


            AddSettingsBinding<BoolController>(xUpwardPanningToggle, ToggleSwitch.IsOnProperty, KeyStore.SettingsUpwardPanningKey);
            AddSettingsBinding<BoolController>(xTextModeToggle, ToggleSwitch.IsOnProperty, KeyStore.SettingsMarkdownModeKey);

            AddSettingsBinding<TextController>(xGridRadio, ToggleButton.IsCheckedProperty, KeyStore.BackgroundImageStateKey, new RadioEnumToBoolConverter(BackgroundImageState.Grid), handler: (sender, dp) => ProcessEnumsAndImage(BackgroundImageState.Grid));
            AddSettingsBinding<TextController>(xLineRadio, ToggleButton.IsCheckedProperty, KeyStore.BackgroundImageStateKey, new RadioEnumToBoolConverter(BackgroundImageState.Line), handler: (sender, dp) => ProcessEnumsAndImage(BackgroundImageState.Line));
            AddSettingsBinding<TextController>(xDotRadio, ToggleButton.IsCheckedProperty, KeyStore.BackgroundImageStateKey, new RadioEnumToBoolConverter(BackgroundImageState.Dot), handler: (sender, dp) => ProcessEnumsAndImage(BackgroundImageState.Dot));
            AddSettingsBinding<TextController>(xBlankRadio, ToggleButton.IsCheckedProperty, KeyStore.BackgroundImageStateKey, new RadioEnumToBoolConverter(BackgroundImageState.Blank), handler: (sender, dp) => ProcessEnumsAndImage(BackgroundImageState.Blank));
            AddSettingsBinding<TextController>(xCustomRadio, ToggleButton.IsCheckedProperty, KeyStore.BackgroundImageStateKey, new RadioEnumToBoolConverter(BackgroundImageState.Custom), handler: async (sender, dp) =>
            {
                if (ImageState != BackgroundImageState.Custom) return;
                if (CustomImagePath != null) { CollectionFreeformView.BackgroundImage = CustomImagePath; return; }
                await TrySetUserPath();
            });

            AddSettingsBinding<NumberController>(xNumBackupsSlider, RangeBase.ValueProperty, KeyStore.SettingsNumBackupsKey, handler: (sender, dp) => UpdateNumBackups(), mode: BindingMode.OneWay);
            AddSettingsBinding<NumberController>(xBackupIntervalSlider, RangeBase.ValueProperty, KeyStore.SettingsBackupIntervalKey, handler: (sender, dp) => UpdateInterval());
            AddSettingsBinding<NumberController>(xBackgroundOpacitySlider, RangeBase.ValueProperty, KeyStore.BackgroundImageOpacityKey, handler: (sender, dp) => CollectionFreeformView.BackgroundOpacity = BackgroundImageOpacity);

            AddSettingsBinding<TextController>(XAuthorBox, TextBox.TextProperty, KeyStore.AuthorKey);
        }

        private void ProcessEnumsAndImage(BackgroundImageState thisState)
        {
            if (ImageState != thisState) return;
            _lastNonCustom = thisState;
            CollectionFreeformView.BackgroundImage = EnumToPathDict[thisState];
        }

        private void UpdateInterval()
        {
            _endpoint.SetBackupInterval((int)xBackupIntervalSlider.Value * 1000);

            var interval = (int)xBackupIntervalSlider.Value;
            var numSec = interval % 60;
            var numMin = (interval - numSec) / 60;

            var minToDisplay = numMin == 0 ? "" : $" {numMin}\'";
            var secToDisplay = numSec == 0 ? "" : $" {numSec}\"";

            xIntervalDisplay.Text = minToDisplay + secToDisplay;
        }

        private void UpdateNumBackups()
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

            //if (ExcessBackupsPresent()) { }

            xNumBackupDisplay.Text = NumBackups.ToString();
        }

        private bool ExcessBackupsPresent()
        {
            var status = false;
            for (var i = 1; i <= 10; i++) { if (i > NumBackups && File.Exists(_dbPath + ".bak" + i)) status = true; }
            return status;
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

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void XCustomizeButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (ImageState != BackgroundImageState.Custom) return;
            await TrySetUserPath();
        }

        private void SettingsPanel_OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is StackPanel activePanel)
            {
                activePanel.Opacity = 1.0;
                foreach (var panel in _mainPanels) { if (panel != activePanel) panel.Opacity = 0.7; }
            }
        }

        private void SettingsPanel_OnPointerExited(object sender, PointerRoutedEventArgs e) { foreach (var panel in _mainPanels) { panel.Opacity = 1.0; } }
    }
}
