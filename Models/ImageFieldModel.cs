using System;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;
using Dash.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Dash
{
    /// <summary>
    /// A Field Model which holds image data
    /// </summary>
    public class ImageFieldModel : FieldModel
    {
        /// <summary>
        /// A <see cref="Uri"/> which points to the <see cref="BitmapImage.UriSource"/>
        /// </summary>
        public Uri Data;

        /// <summary>
        /// Create a new Image Field Model which represents the image pointed to by the <paramref name="data"/>
        /// </summary>
        /// <param name="data">The uri that the image this field model encapsulates is sourced from</param>
        public ImageFieldModel(Uri data)
        {
            Data = data;
        }
    }
}
