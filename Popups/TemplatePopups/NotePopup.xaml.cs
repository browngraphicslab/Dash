﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Popups.TemplatePopups
{
    public sealed partial class NotePopup : UserControl, ICustomTemplate
    {
        private ObservableCollection<string> fields = new ObservableCollection<string>();
        public NotePopup(DocumentController doc)
        {
            this.InitializeComponent();
            foreach (var field in doc.GetDataDocument().EnumDisplayableFields())
            {
                fields.Add(field.Key.Name);
            }
        }

        public Task<List<string>> GetLayout()
        {
            var tcs = new TaskCompletionSource<List<string>>();
            xLayoutPopup.IsOpen = true;
            xConfirmButton.Tapped += XConfirmButton_OnClick;

            void XConfirmButton_OnClick(object sender, RoutedEventArgs e)
            {
                var input = new List<string>
                {
                    fields.ElementAtOrDefault(xTextFieldTitle.SelectedIndex),
                    fields.ElementAtOrDefault(xTextFieldAuthor.SelectedIndex),
                    fields.ElementAtOrDefault(xTextFieldDateCreated.SelectedIndex)
                };

                xLayoutPopup.IsOpen = false;
                tcs.SetResult(input);
                xConfirmButton.Tapped -= XConfirmButton_OnClick;
            }

            return tcs.Task;
        }


        private void Popup_OnOpened(object sender, object e)
        {
        }

        public void SetHorizontalOffset(double offset)
        {
            xLayoutPopup.HorizontalOffset = offset;
        }

        public void SetVerticalOffset(double offset)
        {
            xLayoutPopup.VerticalOffset = offset;
        }

        public FrameworkElement Self()
        {
            return this;
        }
    }

}
