﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public class ActionFuncParams {
        public Point Where;
        public IEnumerable<string> Params;
    }

    public class ActionViewModel : ViewModelBase
    {
        private ImageSource _thumbnailSource;
        private string _title;
        private string _helpText;

        public ImageSource ThumbnailSource
        {
            get => _thumbnailSource;
            set => SetProperty(ref _thumbnailSource, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string HelpText
        {
            get => _helpText;
            set => SetProperty(ref _helpText, value);
        }

        public Func<ActionFuncParams, Task<bool>> Action { get; set; }

        public ActionViewModel(string title, string helpText, Func<ActionFuncParams, Task<bool>> action, ImageSource thumbnailSource)
        {
            _title = title;
            _helpText = helpText;
            Action = action;
            _thumbnailSource = thumbnailSource;//TODO If imageSource is null, use fallback value
        }
    }

    public sealed partial class ActionMenuItem : UserControl
    {
        public ActionViewModel ViewModel => DataContext as ActionViewModel;

        public ActionMenuItem()
        {
            this.InitializeComponent();
        }
    }
}
