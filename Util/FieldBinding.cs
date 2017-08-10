using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Data;
using DashShared;
using Windows.UI.Xaml;

namespace Dash
{
    public delegate void SetHandler<in T>(T field, object value) where T : FieldModelController;
    public delegate object GetHandler<in T>(T field) where T : FieldModelController;

    public class FieldBinding<T> where T : FieldModelController
    {
        public BindingMode Mode;
        public DocumentController Document;
        public KeyController Key;
        public SetHandler<T> SetHandler;
        public GetHandler<T> GetHandler;

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
            return binding.Converter == null ? value : binding.Converter.Convert(value, typeof(object), binding.ConverterParameter, string.Empty);
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
            bool update = true;
            DocumentController.OnDocumentFieldUpdatedHandler handler =
                delegate
                {
                    if (update)
                    {
                        element.SetValue(property, EvaluateBinding(binding));
                    }
                };
            DependencyPropertyChangedCallback callback =
                delegate (DependencyObject sender, DependencyProperty dp)
                {
                    var value = sender.GetValue(dp);
                    if (binding.Converter != null)
                    {
                        value = binding.Converter.ConvertBack(value, typeof(object), binding.ConverterParameter, String.Empty);
                    }
                    update = false;
                    binding.SetHandler(binding.Document.GetDereferencedField<U>(binding.Key, binding.Context), value);
                    update = true;
                };

            long token = -1;
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
