﻿using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Dash
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public partial class EditableMarkdownBlock : INotifyPropertyChanged
    {
        #region BINDING PROPERTIES 

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text", typeof(string), typeof(EditableMarkdownBlock), new PropertyMetadata(default(string)));

        public string Text
        {
            get => GetValue(TextProperty)?.ToString();
            set => SetValue(TextProperty, value);
        }

        #endregion
        

        private bool _markdownBoxLoaded = false;

        public bool MarkdownBoxLoaded
        {
            get => _markdownBoxLoaded;
            set
            {
                _markdownBoxLoaded = value;
                OnPropertyChanged();
            }
        }

        public Grid TextBackground => xBackground;

        public FieldControllerBase TargetFieldController { get; set; }
        public Context TargetDocContext { get; set; }

        public EditableMarkdownBlock()
        {
            InitializeComponent();
            RegisterPropertyChangedCallback(TextProperty, TextChangedCallback);
        }

        private void XMarkdownBlock_DoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            e.Handled = false;
            MarkdownBoxLoaded = true;
        }
        
        private void XMarkdownBlock_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = false;
            MarkdownBoxLoaded = true;
        }

        public void MakeEditable()
        {
            SetExpression(Text);
            MarkdownBoxLoaded = true;
        }
        private void TextChangedCallback(DependencyObject sender, DependencyProperty dp)
        {
            if (MarkdownBoxLoaded)
            {
                SetExpression(XMarkdownBox.Text);
            }
        }

        private void XMarkdownBox_OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void XMarkdownBox_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            e.Handled = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void XMarkdownBox_OnLoaded(object sender, RoutedEventArgs e)
        {
            XMarkdownBlock.Visibility = Visibility.Collapsed;
            XMarkdownBox.Focus(FocusState.Programmatic);
            XMarkdownBox.Text = GetExpression() ?? XMarkdownBlock.Text;

            //this makes typing continue at end
            if (XMarkdownBox.Text.Length > 0)
            {
                XMarkdownBox.SelectionStart = XMarkdownBox.Text.Length;
                XMarkdownBox.SelectionLength = 0;
            }
        }

        private string GetExpression()
        {
            var reference = TargetFieldController?.Dereference(TargetDocContext);
            if (reference is DocumentReferenceController dref && (dref.ReferenceFieldModel as DocumentReferenceModel).CopyOnWrite)
                return XMarkdownBlock.Text;
            return reference?.GetValue(TargetDocContext)?.ToString();
        }

        private void SetExpression(string expression)
        {
            Text = expression;
            MarkdownBoxLoaded = false;
            XMarkdownBlock.Visibility = Visibility.Visible;
        }

        private void XMarkdownBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (MarkdownBoxLoaded)
            {
                SetExpression(XMarkdownBox.Text);
            }
        }

        private void XMarkdownBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private async void XMarkdownBlock_LinkClicked(object sender, Microsoft.Toolkit.Uwp.UI.Controls.LinkClickedEventArgs e)
        {
            if (Uri.TryCreate(e.Link, UriKind.Absolute, out Uri link))
            {
                await Launcher.LaunchUriAsync(link);
            }
        }


    }
}
