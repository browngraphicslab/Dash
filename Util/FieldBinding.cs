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
    public delegate IValueConverter GetConverter<in T>(T field) where T : FieldControllerBase;

    public enum XamlDerefernceLevel {
        DereferenceToRoot,
        DereferenceOneLevel
    };

    public class FieldBinding<T> where T : FieldControllerBase
    {
        public BindingMode Mode;
        public DocumentController Document;
        public KeyController Key;
        public GetConverter<T> GetConverter;
        public XamlDerefernceLevel XamlAssignmentDereferenceLevel = XamlDerefernceLevel.DereferenceToRoot;
        public XamlDerefernceLevel FieldAssignmentDereferenceLevel = XamlDerefernceLevel.DereferenceOneLevel;
        public Object FallbackValue;

        public Context Context;

        public IValueConverter Converter;
        public object ConverterParameter;
        
        public void ConvertToXaml(FrameworkElement element, DependencyProperty property, Context context)
        {
            var refField = Document.GetField(Key) as ReferenceController;
            if (XamlAssignmentDereferenceLevel == XamlDerefernceLevel.DereferenceOneLevel && refField?.GetDocumentController(context)?.GetField(refField.FieldKey) is ReferenceController)
            {
                element.SetValue(property, refField.GetDocumentController(context).GetField(refField.FieldKey).GetValue(context));
            }
            else
            {
                var field = Document.GetDereferencedField<T>(Key, context);
                if (field != null)
                {
                    var converter = GetConverter != null ? GetConverter(field) : Converter;
                    var fieldData = field.GetValue(context);
                    var xamlData = converter == null || fieldData == null
                        ? fieldData
                        : converter.Convert(fieldData, typeof(object), ConverterParameter, string.Empty);
                    if (xamlData != null)
                    {
                        element.SetValue(property, xamlData);
                    }
                    Debug.WriteLine($"Error evaluating binding: Error with converter, Document ID = {Document.Id}, Key = {Key.Name}, Converter = {converter?.GetType().Name ?? "null"}");
                }
                else if (FallbackValue != null)
                {
                    element.SetValue(property, FallbackValue);
                }
                else
                {
                    Debug.WriteLine($"Error evaluating binding: Field was missing and there was no fallback value, Document ID = {Document.Id}, Key = {Key.Name}");

                    element.ClearValue(property);
                }
            }
        }
        public bool ConvertFromXaml(object xamlData)
        {
            var field = FieldAssignmentDereferenceLevel == XamlDerefernceLevel.DereferenceOneLevel ? Document.GetField(Key) : Document.GetDereferencedField<T>(Key,Context);
            if (field is ReferenceController) {
                xamlData = new Tuple<Context, object>(Context, xamlData);
            }
            
            var converter = GetConverter != null ? GetConverter((T)field) : Converter;
            var fieldData = converter == null ? xamlData : converter.ConvertBack(xamlData, typeof(object), ConverterParameter, String.Empty);

            return field.SetValue(fieldData);
        }
    }

    public static class BindingExtension
    {
        public static void AddFieldBinding<T, U>(this T element, DependencyProperty property, FieldBinding<U> binding) where T : FrameworkElement where U : FieldControllerBase
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

        private static void AddOneTimeBinding<T, U>(T element, DependencyProperty property, FieldBinding<U> binding) where T : FrameworkElement where U : FieldControllerBase
        {
            binding.ConvertToXaml(element, property, binding.Context);
        }

        private static void AddOneWayBinding<T, U>(T element, DependencyProperty property, FieldBinding<U> binding) where T : FrameworkElement where U : FieldControllerBase
        {
            FieldControllerBase.FieldUpdatedHandler handler =
                (sender, args, context) =>
                {
                    if (binding.Context.IsCompatibleWith(context.DocContextList))
                    {
                        var equals = binding.Context.DocContextList.Where((d) => !d.DocumentType.Type.Contains("Box") && !d.DocumentType.Type.Contains("Layout") && !context.DocContextList.Contains(d));
                        binding.ConvertToXaml(element, property, equals.Count() == 0 ? context: binding.Context);
                    }
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
            where T : FrameworkElement where U : FieldControllerBase
        {
            bool updateUI = true;
            FieldControllerBase.FieldUpdatedHandler handler =
                (sender, args, context) =>
                {
                    updateUI = false;
                if (binding.Context == null)
                {
                    binding.ConvertToXaml(element, property, context);

                }
                else
                if (binding.Context.IsCompatibleWith(context.DocContextList))
                {
                    var equals = binding.Context.DocContextList.Where((d) => (d.DocumentType.Type == null || ( !d.DocumentType.Type.Contains("Box") && !d.DocumentType.Type.Contains("Layout"))) && !context.DocContextList.Contains(d));
                        binding.ConvertToXaml(element, property, equals.Count() == 0 ? context : binding.Context);
                    }
                    updateUI = true;
                };
            DependencyPropertyChangedCallback callback =
                (sender, dp)   =>
                {
                    if (updateUI)
                    {
                        if (!binding.ConvertFromXaml(sender.GetValue(dp)))
                            binding.ConvertToXaml(element, property, binding.Context);
                    }
                };

            long token = -1;
            if (element.IsInVisualTree())
            {
                handler(null,new DocumentController.DocumentFieldUpdatedEventArgs(null, null, DocumentController.FieldUpdatedAction.Add, null, null, false), binding.Context);
            }
            element.Loaded += delegate (object sender, RoutedEventArgs args)
            {
                binding.Document.AddFieldUpdatedListener(binding.Key, handler);
                binding.ConvertToXaml(element, property, binding.Context);
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
