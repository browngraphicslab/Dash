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
using Windows.UI.Xaml.Controls;
using Dash.Converters;

namespace Dash
{
    public delegate IValueConverter GetConverter<in T>(T field) where T : FieldModelController;

    public enum XamlDerefernceLevel {
        DereferenceToRoot,
        DereferenceOneLevel
    };

    public class FieldBinding<T> where T : FieldModelController
    {
        public BindingMode Mode;
        public DocumentController Document;
        public KeyController Key;
        public GetConverter<T> GetConverter;
        public XamlDerefernceLevel XamlAssignmentDereferenceLevel = XamlDerefernceLevel.DereferenceToRoot;
        public XamlDerefernceLevel FieldAssignmentDereferenceLevel = XamlDerefernceLevel.DereferenceOneLevel;

        public Context Context;

        public IValueConverter Converter;
        public object ConverterParameter;
        
        public void ConvertToXaml(FrameworkElement element, DependencyProperty property)
        {
            var refField = Document.GetField(Key) as ReferenceFieldModelController;
            if (XamlAssignmentDereferenceLevel == XamlDerefernceLevel.DereferenceOneLevel && refField?.GetDocumentController(Context)?.GetField(refField.FieldKey) is ReferenceFieldModelController)
            {
                element.SetValue(property, refField.GetDocumentController(Context).GetField(refField.FieldKey).GetValue(Context));
            }
            else
            {
                var field = Document.GetDereferencedField<T>(Key, Context);
                if (field != null)
                {
                    var converter = GetConverter != null ? GetConverter(field) : Converter;
                    var fieldData = field.GetValue(Context);
                    var xamlData = converter == null ? fieldData : converter.Convert(fieldData, typeof(object), ConverterParameter, string.Empty);
                    if (xamlData != null)
                    {
                        element.SetValue(property, xamlData);
                    }
                }
            }
        }
        public bool ConvertFromXaml(object xamlData)
        {
            var field = FieldAssignmentDereferenceLevel == XamlDerefernceLevel.DereferenceOneLevel ? Document.GetField(Key) : Document.GetDereferencedField<T>(Key,Context);
            var refField = field as ReferenceFieldModelController;
            if (refField != null) {
                if ((string)xamlData == (string)refField.GetValue(Context))
                    return false; // avoid cycles
                xamlData = new Tuple<Context, string>(Context, xamlData as string);
            }
            
            var converter = GetConverter != null ? GetConverter((T)field) : Converter;
            var fieldData = converter == null ? xamlData : converter.ConvertBack(xamlData, typeof(object), ConverterParameter, String.Empty);

            return field.SetValue(fieldData);
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
            DocumentController.OnDocumentFieldUpdatedHandler handler =
                (sender, args) => binding.ConvertToXaml(element, property);
            DependencyPropertyChangedCallback callback =
                (sender, dp)   =>
                {
                    if (!binding.ConvertFromXaml( sender.GetValue(dp)))
                        binding.ConvertToXaml(element, property);
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
