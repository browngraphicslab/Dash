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

    public enum XamlDereferenceLevel
    {
        DereferenceToRoot,
        DereferenceOneLevel,
        DontDereference
    };

    public class FieldBinding<T, U> where T : FieldControllerBase where U : FieldControllerBase, new()
    {
        public BindingMode Mode;
        public DocumentController Document;
        public KeyController Key;
        public GetConverter<T> GetConverter;
        public XamlDereferenceLevel XamlAssignmentDereferenceLevel = XamlDereferenceLevel.DereferenceToRoot;
        public XamlDereferenceLevel FieldAssignmentDereferenceLevel = XamlDereferenceLevel.DereferenceOneLevel;
        public Object FallbackValue;

        public Context Context;

        public IValueConverter Converter;
        public object ConverterParameter;

        //Debug stuff
        //Tag that can be set on a binding that will be printed if the binding fails
        //so that you can know which exact binding is failing
        public String Tag;

        public void ConvertToXaml(FrameworkElement element, DependencyProperty property, Context context)
        {
            var refField = Document.GetField(Key) as ReferenceController;
            if (XamlAssignmentDereferenceLevel == XamlDereferenceLevel.DereferenceOneLevel && refField?.GetDocumentController(context)?.GetField(refField.FieldKey) is ReferenceController)
            {
                element.SetValue(property, refField.GetDocumentController(context).GetField(refField.FieldKey).GetValue(context));
            }
            else
            {
                var field = Document.GetDereferencedField<T>(Key, context);
                if (field != null)
                {
                    var converter = Converter;
                    if (GetConverter != null)
                    {
                        converter = GetConverter(field);
                    }
                    var fieldData = field.GetValue(context);
                    var xamlData = converter == null || fieldData == null
                        ? fieldData
                        : converter.Convert(fieldData, typeof(object), ConverterParameter, string.Empty);
                    if (xamlData != null)
                    {
                        element.SetValue(property, xamlData);
                    }
                    else
                    {
#if PRINT_BINDING_ERROR_DETAILED
                        Debug.WriteLine(
                            $"Error evaluating binding: Error with converter or GetValue\n" +
                            $"  Key         = {Key.Name}\n" +
                            $"  Document ID = {Document.Id}\n" +
                            $"  Field Data  = {fieldData}\n" +
                            $"  Converter   = {converter?.GetType().Name ?? "null"}\n" +
                            $"  Tag         = {(string.IsNullOrWhiteSpace(Tag) ? "<empty>" : Tag)}");
#else
                        Debug.WriteLine($"Error evaluating binding: Error with converter or GetValue of {Key.Name}, #define PRINT_BINDING_ERROR_DETAILED to print more detailed");
#endif
                    }
                }
                else if (FallbackValue != null)
                {
                    element.SetValue(property, FallbackValue);
                }
                else
                {
#if PRINT_BINDING_ERROR_DETAILED
                    Debug.WriteLine(
                        $"Error evaluating binding: Field was missing and there was no fallback value\n" +
                        $"  Key         = {Key.Name}\n" + 
                        $"  Document ID = {Document.Id}" +
                        $"  Tag         = {(string.IsNullOrWhiteSpace(Tag) ? "<empty>" : Tag)}");
#else
                    Debug.WriteLine($"Error evaluating binding: Field {Key.Name} was missing and there was no fallback value, #define PRINT_BINDING_ERROR_DETAILED to print more detailed");
#endif

                    element.ClearValue(property);
                }
            }
        }
        public bool ConvertFromXaml(object xamlData)
        {
            var field = (FieldAssignmentDereferenceLevel == XamlDereferenceLevel.DereferenceOneLevel || FieldAssignmentDereferenceLevel == XamlDereferenceLevel.DontDereference) ? Document.GetField(Key) : Document.GetDereferencedField<T>(Key, Context);
            if (FieldAssignmentDereferenceLevel == XamlDereferenceLevel.DontDereference)
            {
                field = field as T;
            }
            if (field is ReferenceController)
            {
                xamlData = new Tuple<Context, object>(Context, xamlData);
            }

            var converter = GetConverter != null ? GetConverter((T)field) : Converter;
            var fieldData = converter == null || field is ReferenceController ? xamlData : converter.ConvertBack(xamlData, typeof(object), ConverterParameter, string.Empty);

            if (field == null)
            {
                U f = new U();
                if (!f.SetValue(fieldData))
                {
                    return false;
                }

                return Document.SetField(Key, f, true);
            }
            else
            {
                return field.SetValue(fieldData);
            }
        }
    }

    public class FieldBinding<T> : FieldBinding<T, TextController> where T : FieldControllerBase { }

    public static class BindingExtension
    {
        private static Dictionary<UIElement, Dictionary<DependencyProperty,
            Action>> _bindingMap =
            new Dictionary<UIElement, Dictionary<DependencyProperty, Action>>();

        public static void AddFieldBinding<T, U, V>(this T element, DependencyProperty property, FieldBinding<U, V> binding) where T : FrameworkElement where U : FieldControllerBase where V : FieldControllerBase, new()
        {
            TryRemoveOldBinding(element, property);
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

        private static bool TryRemoveOldBinding(FrameworkElement element, DependencyProperty property)
        {
            if (!_bindingMap.ContainsKey(element))
            {
                return false;
            }
            var dict = _bindingMap[element];
            if (!dict.ContainsKey(property))
            {
                return false;
            }
            var t = dict[property];
            t();
            dict.Remove(property);
            return true;
        }

        private static void AddRemoveBindingAction(FrameworkElement element, DependencyProperty property, Action removeBinding)
        {
            if (!_bindingMap.ContainsKey(element))
            {
                _bindingMap[element] = new Dictionary<DependencyProperty, Action>();
            }

            Debug.Assert(!_bindingMap[element].ContainsKey(property));
            _bindingMap[element][property] = removeBinding;
        }

        private static void AddOneTimeBinding<T, U, V>(T element, DependencyProperty property, FieldBinding<U, V> binding) where T : FrameworkElement where U : FieldControllerBase where V : FieldControllerBase, new()
        {
            binding.ConvertToXaml(element, property, binding.Context);
        }

        private static void AddOneWayBinding<T, U, V>(T element, DependencyProperty property, FieldBinding<U, V> binding) where T : FrameworkElement where U : FieldControllerBase where V : FieldControllerBase, new()
        {
            FieldControllerBase.FieldUpdatedHandler handler =
                (sender, args, context) =>
                {
                    if (binding.Context == null)
                    {
                        binding.ConvertToXaml(element, property, context);

                    }
                    else
                    if (binding.Context.IsCompatibleWith(context.DocContextList))
                    {
                        var equals = binding.Context.DocContextList.Where((d) => (d.DocumentType.Type == null || (!d.DocumentType.Type.Contains("Box") && !d.DocumentType.Type.Contains("Layout"))) && !context.DocContextList.Contains(d));
                        binding.ConvertToXaml(element, property, equals.Count() == 0 ? context : binding.Context);
                    }
                };

            bool loaded = false;
            if (element.IsInVisualTree())
            {
                binding.ConvertToXaml(element, property, binding.Context);
                binding.Document.AddFieldUpdatedListener(binding.Key, handler);
                loaded = true;
            }

            void OnElementOnUnloaded(object sender, RoutedEventArgs args)
            {
                if (loaded)
                {
                    binding.Document.RemoveFieldUpdatedListener(binding.Key, handler);
                    loaded = false;
                }
            }

            void OnElementOnLoaded(object sender, RoutedEventArgs args)
            {
                if (!loaded)
                {
                    binding.ConvertToXaml(element, property, binding.Context);
                    binding.Document.AddFieldUpdatedListener(binding.Key, handler);
                    loaded = true;
                }
            }

            element.Unloaded += OnElementOnUnloaded;

            element.Loaded += OnElementOnLoaded;

            void RemoveBinding()
            {
                if (loaded)
                {
                    element.Loaded -= OnElementOnLoaded;
                    element.Unloaded -= OnElementOnUnloaded;
                    binding.Document.RemoveFieldUpdatedListener(binding.Key, handler);
                    loaded = false;
                }
            }

            AddRemoveBindingAction(element, property, RemoveBinding);
        }

        private static void AddTwoWayBinding<T, U, V>(T element, DependencyProperty property, FieldBinding<U, V> binding)
            where T : FrameworkElement where U : FieldControllerBase where V : FieldControllerBase, new()
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
                        var equals = binding.Context.DocContextList.Where((d) => (d.DocumentType.Type == null || (!d.DocumentType.Type.Contains("Box") && !d.DocumentType.Type.Contains("Layout"))) && !context.DocContextList.Contains(d));
                        binding.ConvertToXaml(element, property, equals.Count() == 0 ? context : binding.Context);
                    }
                    updateUI = true;
                };
            DependencyPropertyChangedCallback callback =
                (sender, dp) =>
                {
                    if (updateUI)
                    {
                        if (!binding.ConvertFromXaml(sender.GetValue(dp)))
                            binding.ConvertToXaml(element, property, binding.Context);
                    }
                };

            bool loaded = false;
            long token = -1;

            if (element.IsInVisualTree())
            {
                binding.ConvertToXaml(element, property, binding.Context);
                binding.Document.AddFieldUpdatedListener(binding.Key, handler);
                token = element.RegisterPropertyChangedCallback(property, callback);
                loaded = true;
            }

            void OnElementOnUnloaded(object sender, RoutedEventArgs args)
            {
                if (loaded)
                {
                    binding.Document.RemoveFieldUpdatedListener(binding.Key, handler);
                    element.UnregisterPropertyChangedCallback(property, token);
                    token = -1;
                    loaded = false;
                }
            }

            void OnElementOnLoaded(object sender, RoutedEventArgs args)
            {
                if (!loaded)
                {
                    binding.ConvertToXaml(element, property, binding.Context);
                    binding.Document.AddFieldUpdatedListener(binding.Key, handler);
                    token = element.RegisterPropertyChangedCallback(property, callback);
                    loaded = true;
                }
            }

            element.Unloaded += OnElementOnUnloaded;

            element.Loaded += OnElementOnLoaded;

            void RemoveBinding()
            {
                element.Loaded -= OnElementOnLoaded;
                element.Unloaded -= OnElementOnUnloaded;
                if (loaded)
                {
                    binding.Document.RemoveFieldUpdatedListener(binding.Key, handler);
                    if (token != -1)
                    {
                        element.UnregisterPropertyChangedCallback(property, token);
                    }
                    loaded = false;
                }
            }

            AddRemoveBindingAction(element, property, RemoveBinding);
        }
    }
}
