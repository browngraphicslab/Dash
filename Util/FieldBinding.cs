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
    public delegate void SetHandler<in T>(object binding, T field, object value) where T : FieldModelController;
    public delegate object GetHandler<in T>(T field) where T : FieldModelController;
    public delegate IValueConverter GetConverter<in T>(T field) where T : FieldModelController;

    public class FieldBinding<T> where T : FieldModelController
    {
        public BindingMode Mode;
        public DocumentController Document;
        public KeyController Key;
        public SetHandler<T> SetHandler;
        public GetHandler<T> GetHandler;
        public GetConverter<T> GetConverter;
        public bool EvalBindingOnSet = false;

        public Context Context;

        public IValueConverter Converter;
        public object ConverterParameter;
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
        
        private static object EvaluateBinding<T>(FieldBinding<T> binding) where T : FieldModelController
        {
            var field = binding.Document.GetDereferencedField<T>(binding.Key, binding.Context);
            if (field == null)
                return null;
            var value = binding.GetHandler(field);
            var converter = binding.GetConverter != null ? binding.GetConverter(field) : binding.Converter;
            return converter == null ? value : converter.Convert(value, typeof(object), binding.ConverterParameter, string.Empty);
        }

        private static void AddOneTimeBinding<T, U>(T element, DependencyProperty property, FieldBinding<U> binding) where T : FrameworkElement where U : FieldModelController
        {
            element.SetValue(property, EvaluateBinding(binding));
        }

        private static void AddOneWayBinding<T, U>(T element, DependencyProperty property, FieldBinding<U> binding) where T : FrameworkElement where U : FieldModelController
        {
            DocumentController.OnDocumentFieldUpdatedHandler handler =
                delegate
                {
                    element.SetValue(property, EvaluateBinding(binding));
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
                        element.SetValue(property, EvaluateBinding(binding));
                        updateField = true;
                    }
                };
            DependencyPropertyChangedCallback callback =
                delegate (DependencyObject sender, DependencyProperty dp)
                {
                    if (updateField)
                    {
                        var value = sender.GetValue(dp);
                        updateUI = false;
                        var refField = binding.Document.GetField(binding.Key) as ReferenceFieldModelController;
                        if (binding.Converter != null)
                        {
                            value = binding.Converter.ConvertBack(value, typeof(object), binding.ConverterParameter, String.Empty);
                        }
                        binding.SetHandler(binding, binding.Document.GetDereferencedField<U>(binding.Key, binding.Context), value);
                        if (binding.EvalBindingOnSet)
                            element.SetValue(property, EvaluateBinding(binding));
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
                var value = EvaluateBinding(binding);
                if (value != null)
                {
                    element.SetValue(property, value);
                }
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
