﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Dash.FontIcons
{ /// <summary>
  /// Represents ann icon that uses the FontAwesome font
  /// </summary>
    public class FontAwesome : FontIcon
    {
        private static readonly FontFamily FontAwesomeFontFamily = new FontFamily("ms-appx:/Assets/Fonts/FontAwesome.otf#FontAwesome");

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(FontAwesomeIcon), typeof(FontAwesome),
                new PropertyMetadata(FontAwesomeIcon.None, Icon_PropertyChangedCallback));

        private static void Icon_PropertyChangedCallback(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var fontAwesome = (FontAwesome)dependencyObject;
            var fontToSet = FontAwesomeIcon.None;

            if (dependencyPropertyChangedEventArgs.NewValue != null)
                fontToSet = (FontAwesomeIcon)dependencyPropertyChangedEventArgs.NewValue;

            fontAwesome.SetValue(FontFamilyProperty, FontAwesomeFontFamily);
            fontAwesome.SetValue(GlyphProperty, char.ConvertFromUtf32((int)fontToSet));
        }

        public FontAwesome()
        {
        }

        /// <summary>
        /// Gets or sets the FontAwesome icon
        /// </summary>
        public FontAwesomeIcon Icon
        {
            get { return (FontAwesomeIcon)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }
    }
}