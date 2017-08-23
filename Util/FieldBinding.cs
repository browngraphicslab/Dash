using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Data;
using DashShared;
using Windows.UI.Xaml;
using System.Diagnostics;
using Dash.Converters;

namespace Dash
{
    public delegate IValueConverter GetConverter<in T>(T field) where T : FieldModelController;

    public class FieldBinding<T> where T : FieldModelController
    {
        public BindingMode Mode;
        public DocumentController Document;
        public KeyController Key;
        public GetConverter<T> GetConverter;

        public Context Context;

        public IValueConverter Converter;
        public object ConverterParameter;
        
        public void ConvertToXaml(FrameworkElement element, DependencyProperty property)
        {
            var field = Document.GetDereferencedField<T>(Key, Context);
            if (field != null)
            {
                var converter = GetConverter != null ? GetConverter(field) : Converter;
                var fieldData = field.GetValue(); 
                var xamlData = converter == null ? fieldData : converter.Convert(fieldData, typeof(object), ConverterParameter, string.Empty);
                if (xamlData != null)
                {
                    element.SetValue(property, xamlData);
                }
            }
        }
        public void ConvertFromXaml(object xamlData)
        {
            var field     = Document.GetDereferencedField<T>(Key, Context);
            var converter = GetConverter != null ? GetConverter(field) : Converter;
            var fieldData = converter == null ? xamlData : converter.ConvertBack(xamlData, typeof(object), ConverterParameter, String.Empty);
            field.SetValue(fieldData); 
        }
    }

    public static class BindingExtension
    {
        public static void AddFieldBinding<T, U>(this T element, DependencyProperty property, FieldBinding<U> binding) where T : FrameworkElement where U : FieldModelController
        {
            switch (binding.Mode)
            {
                case BindingMode.OneTime:
                    AddOneTimeBinding(element, property, binding);
                    break;
                case BindingMode.OneWay:
                    AddOneWayBinding(element, property, binding);
                    break;
                case BindingMode.TwoWay:
                    AddTwoWayBinding(element, property, binding);
                    break;
            }
        }

        private static void AddOneTimeBinding<T, U>(T element, DependencyProperty property, FieldBinding<U> binding) where T : FrameworkElement where U : FieldModelController
        {
            binding.ConvertToXaml(element, property);
        }

        private static void AddOneWayBinding<T, U>(T element, DependencyProperty property, FieldBinding<U> binding) where T : FrameworkElement where U : FieldModelController
        {
            DocumentController.OnDocumentFieldUpdatedHandler handler =
                delegate
                {
                    binding.ConvertToXaml(element, property);
                };
            if (element.IsInVisualTree())
            {
                binding.Document.AddFieldUpdatedListener(binding.Key, handler);
            }
            element.Loaded += delegate (object sender, RoutedEventArgs args)
            {
                binding.Document.AddFieldUpdatedListener(binding.Key, handler);
            };
            element.Unloaded += delegate (object sender, RoutedEventArgs args)
            {
                binding.Document.RemoveFieldUpdatedListener(binding.Key, handler);
            };
        }

        private static void AddTwoWayBinding<T, U>(T element, DependencyProperty property, FieldBinding<U> binding)
            where T : FrameworkElement where U : FieldModelController
        {
            bool updateUI = true;
            bool updateField = true;
            DocumentController.OnDocumentFieldUpdatedHandler handler =
                delegate
                {
                    if (updateUI)
                    {
                        updateField = false;
                        binding.ConvertToXaml(element, property);
                        updateField = true;
                    }
                };
            DependencyPropertyChangedCallback callback =
                delegate (DependencyObject sender, DependencyProperty dp)
                {
                    if (updateField)
                    {
                        updateUI = false;
                        binding.ConvertFromXaml(sender.GetValue(dp));
                        updateUI = true;
                    }
                };

            long token = -1;
            if (element.IsInVisualTree())
            {
                handler(null,null);
            }
            element.Loaded += delegate (object sender, RoutedEventArgs args)
            {
                binding.Document.AddFieldUpdatedListener(binding.Key, handler);
                binding.ConvertToXaml(element, property);
                token = element.RegisterPropertyChangedCallback(property, callback);
            };
            element.Unloaded += delegate (object sender, RoutedEventArgs args)
            {
                binding.Document.RemoveFieldUpdatedListener(binding.Key, handler);
                element.UnregisterPropertyChangedCallback(property, token);
            };

        }
    }
}
