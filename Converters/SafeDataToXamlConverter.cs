﻿using System;
using System.Diagnostics;
using System.Reflection;
using Windows.UI.Xaml.Data;

namespace Dash
{
    /// <summary>
    ///     The safe Data to Xaml Converter is used to convert a data type into a xaml type and back again
    /// </summary>
    /// <typeparam name="TData">The data type which is going to be converted into xaml</typeparam>
    /// <typeparam name="TXaml">The xaml type which is going to be converted into data</typeparam>
    public abstract class SafeDataToXamlConverter<TData, TXaml> : IValueConverter 
    {
        /// <summary>
        ///     A method which is called by XAML and binding. You should use <see cref="ConvertDataToXaml" /> instead
        /// </summary>
        /// <param name="value">The data which is going to be converted into xaml</param>
        /// <param name="targetType">The xaml type this converter is trying to produce</param>
        /// <param name="parameter"></param>
        /// <param name="language"></param>
        /// <returns>The xaml equivalent to the data</returns>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Debug.Assert(value is TData, "You are trying to convert data into xaml, but the data is of the wrong type");

            // TODO somehow make sure that this assert works. it started failing on me
            Debug.Assert(targetType.IsAssignableFrom(typeof(TXaml)), 
                "You are trying to get a xaml type which this converter does not produce");
            return ConvertDataToXaml((TData) value);
        }

        /// <summary>
        ///     A method which is called by XAML and binding. You should use <see cref="ConvertXamlToData" /> instead
        /// </summary>
        /// <param name="value">The xaml which is going to be converted into data</param>
        /// <param name="targetType">The data type this converter is trying to produce</param>
        /// <param name="parameter"></param>
        /// <param name="language"></param>
        /// <returns>The data equivalent to the xaml</returns>
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            Debug.Assert(value is TXaml, "You are trying to convert xaml into data, but the xaml is of the wrong type");
            Debug.Assert(targetType.IsAssignableFrom(typeof(TData)),
                "You are trying to get a data type which this converter does not produce");
            return ConvertXamlToData((TXaml) value);
        }

        /// <summary>
        ///     Implement this method if you want to convert a data type into a xaml type, you almost always want to do this
        /// </summary>
        /// <param name="data">The data type that is going to be converted</param>
        /// <returns>The xaml type which is going to be rendered</returns>
        public abstract TXaml ConvertDataToXaml(TData data);

        /// <summary>
        ///     Implement this method if you want to convert xaml back into data. This method is unnecessary in some cases
        ///     such as one way binding. So it can just throw a NotImplementedException if that is the case
        /// </summary>
        /// <param name="xaml">The xaml type which is going to be converted</param>
        /// <returns>The data type which is the equivalent to the xaml</returns>
        public abstract TData ConvertXamlToData(TXaml xaml);
    }
}