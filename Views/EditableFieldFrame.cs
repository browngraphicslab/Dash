using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The Templated Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234235

namespace Dash
{

    [TemplatePart(Name = EditableContentName, Type = typeof(ContentControl))]
    [TemplatePart(Name = ContainerName, Type = typeof(UIElement))]
    public class EditableFieldFrame : Control
    {

        // variable names for accessing parts from xaml!
        private const string EditableContentName = "PART_EditableContent";
        private const string ContainerName = "PART_Container";


        /// <summary>
        /// Private variable to get the container which determines the size of the window
        /// so we don't have to look for it on manipulation delta
        /// </summary>
        private FrameworkElement _container;

        public EditableFieldFrame()
        {
            this.DefaultStyleKey = typeof(EditableFieldFrame);
        }

        /// <summary>
        /// On apply template we add events and get parts from xaml
        /// </summary>
        protected override void OnApplyTemplate()
        {
            // get the container private variable
            _container = GetTemplateChild(ContainerName) as FrameworkElement;
            Debug.Assert(_container != null);
        }

        /// <summary>
        /// The inner content of the window can be anything!
        /// </summary>
        public object EditableContent
        {
            get { return (object)GetValue(EditableContentProperty); }
            set { SetValue(EditableContentProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EditableContent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EditableContentProperty =
            DependencyProperty.Register("EditableContent", typeof(object), typeof(EditableFieldFrame), new PropertyMetadata(null));
    }
}
