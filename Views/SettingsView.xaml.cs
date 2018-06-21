using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using DashShared;

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

        private bool _markdownEditOn = false;
        public bool MarkdownEditOn
        {
            get => _markdownEditOn;
            private set
            {
                _markdownEditOn = value;
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

        private bool _isGrid = true;

        public bool IsGrid
        {
            get => _isGrid;
            private set
            {
                _isGrid = value; 
                if (value)
                    CollectionFreeformView.BackgroundImage = "ms-appx:///Assets/transparent_grid_tilable.png";
            }
        }

        private bool _isLine = false;

        public bool IsLine
        {
            get => _isLine;
            private set
            {
                _isLine = value; 
                if (value)
                    CollectionFreeformView.BackgroundImage = "ms-appx:///Assets/transparent_line_tilable.png";
            }
        }

        private bool _isDot = false;

        public bool IsDot
        {
            get => _isDot;
            private set
            {
                _isDot = value;
                if (value)
                    CollectionFreeformView.BackgroundImage = "ms-appx:///Assets/transparent_dot_tilable.png";
            }
        }
        private bool _isBlank = false;

        public bool IsBlank
        {
            get => _isBlank;
            private set
            {
                _isBlank = value;
                if (value)
                    CollectionFreeformView.BackgroundImage = "ms-appx:///Assets/transparent_blank_tilable.png";
            }
        }

        private float _bgopacity = 1.0f;
        public float BackgroundOpacity
        {
            get => _bgopacity;
            private set
            {
                _bgopacity = value;
                CollectionFreeformView.BackgroundOpacity = value;
            }
        }
        public bool NoUpperLimit { get; private set; } = false;
        #endregion

        public SettingsView()
        {
            InitializeComponent();

            Debug.Assert(Instance == null);
            Instance = this;

            _dbPath = ApplicationData.Current.LocalFolder.Path + "\\" + "dash.db";
            _pathToRestore = _dbPath + ".toRestore";

            xCustomizeButton.Tapped += async delegate
            {
                var path = await GetPath();
                if (path != null)
                    CollectionFreeformView.BackgroundImage = path;
            };
        }

        private async void Restore_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            //opens file picker and limits search by listed image extensions
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
                Debug.WriteLine($"Successfully parsed {numSelected} from path!");
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
        }

        private async Task<IRandomAccessStreamWithContentType> GetPath()
        {
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                var stream = await file.OpenReadAsync();
                return stream;
            }
            else
            {
                return null;
            }
        }
    }
}
