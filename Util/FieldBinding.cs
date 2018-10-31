using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Dash
{
    public delegate IValueConverter GetConverter<in T>(T field) where T : FieldControllerBase;

    public enum XamlDereferenceLevel
    {
        DereferenceToRoot,
        DereferenceOneLevel,
        DontDereference
    };

    public enum BindingValueType
    {
        Value,
        Field
    }

    public interface IFieldBinding
    {
        String Tag { get; set; }
        BindingMode Mode { get; set; }
        Context Context { get; set; }
        void ConvertToXaml(FrameworkElement element, DependencyProperty property, Context context);
        bool ConvertFromXaml(object xamlData);

        void Add(DocumentController.DocumentUpdatedHandler handler);
        void Remove(DocumentController.DocumentUpdatedHandler handler);
    }

    public class FieldBinding<TField, TDefault> : IFieldBinding where TField : FieldControllerBase where TDefault : FieldControllerBase, new()
    {
        public String Tag { get; set; }
        public BindingMode Mode { get; set; }
        public DocumentController Document;
        public KeyController Key;
        public GetConverter<TField> GetConverter;
        public XamlDereferenceLevel XamlAssignmentDereferenceLevel = XamlDereferenceLevel.DereferenceToRoot;
        public XamlDereferenceLevel FieldAssignmentDereferenceLevel = XamlDereferenceLevel.DereferenceOneLevel;
        public BindingValueType ValueType = BindingValueType.Value;
        public Object FallbackValue;

        public Context Context { get; set; }

        public IValueConverter Converter;
        public object ConverterParameter;

        public FieldBinding([CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = "", [CallerFilePath] string path = "")
        {
            Tag = "Binding set at line " + lineNumber + " from " + caller + " in file " + path;
        }

        //Debug stuff
        //Tag that can be set on a binding that will be printed if the binding fails
        //so that you can know which exact binding is failing

        public void ConvertToXaml(FrameworkElement element, DependencyProperty property, Context context)
        {
            var refField = Document.GetField(Key) as ReferenceController;
            if (XamlAssignmentDereferenceLevel == XamlDereferenceLevel.DereferenceOneLevel && refField?.GetDocumentController(context)?.GetField(refField.FieldKey) is ReferenceController)
            {
                element.SetValue(property, refField.Dereference(context).GetValue(context));
            }
            else
            {
                var field = XamlAssignmentDereferenceLevel == XamlDereferenceLevel.DontDereference ? Document.GetField<TField>(Key) : Document.GetDereferencedField<TField>(Key, context);
                if (field != null)
                {
                    var converter = Converter;
                    if (GetConverter != null)
                    {
                        converter = GetConverter(field);
                        Debug.WriteLine("CONVERTER: " + GetConverter(field) + "FIELD: " + field);
                    }
                    var fieldData = ValueType == BindingValueType.Value ? field.GetValue(context) : field;
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
            var field = (FieldAssignmentDereferenceLevel == XamlDereferenceLevel.DereferenceOneLevel ||
                         FieldAssignmentDereferenceLevel == XamlDereferenceLevel.DontDereference) ?
                Document.GetField(Key) : Document.GetDereferencedField<TField>(Key, Context);
            if (FieldAssignmentDereferenceLevel == XamlDereferenceLevel.DontDereference)
            {
                field = field as TField;
            }
            if (field is ReferenceController)
            {
                xamlData = new Tuple<Context, object>(Context, xamlData);
            }

            var converter = GetConverter != null ? GetConverter((TField)field) : Converter;
            var fieldData = converter == null || field is ReferenceController ? xamlData : converter.ConvertBack(xamlData, typeof(object), ConverterParameter, string.Empty);

            //TODO Make converters have out parameter and bool return value so they can indicate if a conversion was unsuccessful
            if (fieldData == null)
            {
                return true;
            }

            if (field == null)
            {
                TDefault f = new TDefault();
                if (!f.TrySetValue(fieldData))
                {
                    return false;
                }

                return Document.SetField(Key, f, true);
            }
            else
            {
                return field.TrySetValue(fieldData);
            }
        }

        public void Add(DocumentController.DocumentUpdatedHandler handler)
        {
            Document.AddFieldUpdatedListener(Key, handler);
        }

        public void Remove(DocumentController.DocumentUpdatedHandler handler)
        {
            Document.RemoveFieldUpdatedListener(Key, handler);
        }
    }

    public class FieldBinding<T> : FieldBinding<T, T> where T : FieldControllerBase, new()
    {
        public FieldBinding([CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = "",
            [CallerFilePath] string path = "") : base(lineNumber, caller, path)
        {
        }
    }
    public class BindingMap : DependencyObject
    {
        public static readonly DependencyProperty BindingMapProperty =
        DependencyProperty.RegisterAttached( "BindingMap",
          typeof(Dictionary<DependencyProperty, Action>),
          typeof(BindingMap),
          new PropertyMetadata(null)
        );
        public static void SetBindingMap(UIElement element, Dictionary<DependencyProperty, Action> value)
        {
            element.SetValue(BindingMapProperty, value);
        }
        public static Dictionary<DependencyProperty, Action> GetBindingMap(UIElement element)
        {
            return (Dictionary<DependencyProperty, Action>)element.GetValue(BindingMapProperty);
        }
    }

    public static class BindingExtension
    {
        public static void AddFieldBinding<T>(this T element, DependencyProperty property, IFieldBinding binding) where T : FrameworkElement
        {
            TryRemoveOldBinding(element, property);
            if (binding == null) return;
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
            if (BindingMap.GetBindingMap(element) == null)
            {
                return false;
            }
            var dict = BindingMap.GetBindingMap(element);
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
            if (BindingMap.GetBindingMap(element) == null)
            {
                BindingMap.SetBindingMap(element, new Dictionary<DependencyProperty, Action>());
            }

            BindingMap.GetBindingMap(element)[property] = removeBinding;
        }

        private static void AddOneTimeBinding<T>(T element, DependencyProperty property, IFieldBinding binding) where T : FrameworkElement
        {
            binding.ConvertToXaml(element, property, binding.Context);
        }

        private static void AddOneWayBinding<T>(T element, DependencyProperty property, IFieldBinding binding) where T : FrameworkElement
        {
            DocumentController.DocumentUpdatedHandler handler =
                (sender, args) =>
                {
                    if (binding.Context == null)
                    {
                        binding.ConvertToXaml(element, property, null);

                    }
                    else
                    //if (binding.Context.IsCompatibleWith(context))
                    {
                        binding.ConvertToXaml(element, property, null);
                    }
                };

            //int id = ID++;
            int refCount = 0;
            bool loading = false;

            element.Unloaded += OnElementOnUnloaded;
                element.Loaded += OnElementOnLoading;
            //if (element.ActualWidth != 0 || element.ActualHeight != 0) // element.IsInVisualTree())
            if (element.IsInVisualTree())
            {
                loading = true;
                element.Loaded -= OnElementOnLoading;
                element.Loading += OnElementOnLoading;
                AddBinding();
                //Debug.WriteLine($"Binding {id,-5} in visual tree : RefCount = {refCount,5}, {element.GetType().Name}");
            }
            else
            {
                //Debug.WriteLine($"Binding {id,-5} not in visual tree : RefCount = {refCount,5}, {element.GetType().Name}");
            }

            void AddBinding()
            {
                if (refCount++ == 0)
                {
                    binding.ConvertToXaml(element, property, binding.Context);
                    binding.Add(handler);
                }

                //tfs: This should not get hit now, with the new splitting. We should be able to remove all refcount stuff
                //Debug.Assert(refCount == 1);
            }

            void OnElementOnUnloaded(object sender, RoutedEventArgs args)
            {
                if (--refCount == 0)
                {
                    binding.Remove(handler);
                }

                //Debug.WriteLine($"Binding {id,-5} Unloaded :       RefCount = {refCount,5}, {element.GetType().Name}");

                //TODO tfs: This assert fails when splitting, but it doesn't keep going negative, so it might not be an issue, but it shouldn't fail and I have no idea why/how it's failing
                //tfs: the assert fails because Loaded and Unloaded can get called out of order
                //     so it is possible for element to not be in the visual tree, but still be unloaded before being loaded.
                //     I'm pretty sure that in this case we end up with a net zero anyway, so I don't think it is actually causing issues,
                //     but it does kinda mess with how the reference counting should work...

                //tfs: This should not get hit now, with the new splitting. We should be able to remove all refcount stuff
                //Debug.Assert(refCount == 0);
            }

            void OnElementOnLoading(object frameworkElement, object o)
            {
                if (loading && element.IsInVisualTree())
                {
                    return;
                }
                AddBinding();

                //Debug.WriteLine($"Binding {id,-5} {(loading ? "Loading" : "Loaded")} :         RefCount = {refCount,5}, {element.GetType().Name}");
            }

            void RemoveBinding()
            {
                element.Loading -= OnElementOnLoading;
                element.Loaded -= OnElementOnLoading;
                element.Unloaded -= OnElementOnUnloaded;
                binding.Remove(handler);
                refCount = 0;
            }

            AddRemoveBindingAction(element, property, RemoveBinding);
        }

        //private static int ID = 0;
        private static void AddTwoWayBinding<T>(T element, DependencyProperty property, IFieldBinding binding)
            where T : FrameworkElement
        {
            //int id = ID++;
            bool updateUI = true;
            DocumentController.DocumentUpdatedHandler handler =
                (sender, args) =>
                {
                    updateUI = false;
                    if (binding.Context == null)
                    {
                        binding.ConvertToXaml(element, property, null);
                    }
                    else
                    //if (binding.Context.IsCompatibleWith(context))
                    {
                        binding.ConvertToXaml(element, property, binding.Context);
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

            long token = -1;
            int refCount = 0;
            bool loading = false;
            element.Unloaded += OnElementOnUnloaded;
                element.Loaded += OnElementOnLoaded;

            //if (element.ActualWidth != 0 || element.ActualHeight != 0) // element.IsInVisualTree())
            if (element.IsInVisualTree())
            {
                loading = true;
                element.Loaded -= OnElementOnLoaded;
                element.Loading += OnElementOnLoaded;
                AddBinding();
                //Debug.WriteLine($"Binding {id,-5} in visual tree : RefCount = {refCount,5}, {element.GetType().Name}");
            }
            else
            {
                //Debug.WriteLine($"Binding {id,-5} not in visual tree : RefCount = {refCount,5}, {element.GetType().Name}");
            }

            void AddBinding()
            {
                if (refCount++ == 0)
                {
                    binding.ConvertToXaml(element, property, binding.Context);
                    binding.Add(handler);
                    token = element.RegisterPropertyChangedCallback(property, callback);
                    //Debug.WriteLine($"Binding {id,-5} Add :            RefCount = {refCount,5}, {element.GetType().Name}");
                }

                //tfs: This should not get hit now, with the new splitting. We should be able to remove all refcount stuff
                //Debug.Assert(refCount == 1);
            }

            void OnElementOnUnloaded(object sender, RoutedEventArgs args)
            {

                if (--refCount == 0)
                {
                    binding.Remove(handler);
                    element.UnregisterPropertyChangedCallback(property, token);
                    token = -1;
                    //Debug.WriteLine($"Binding {id,-5} Remove :         RefCount = {refCount,5}, {element.GetType().Name}");
                }

                //Debug.WriteLine($"Binding {id,-5} Unloaded :       RefCount = {refCount,5}, {element.GetType().Name}");

                //TODO tfs: This assert fails when splitting, but it doesn't keep going negative, so it might not be an issue, but it shouldn't fail and I have no idea why/how it's failing
                //tfs: the assert fails because Loaded and Unloaded can get called out of order
                //     so it is possible for element to not be in the visual tree, but still be unloaded before being loaded.
                //     I'm pretty sure that in this case we end up with a net zero anyway, so I don't think it is actually causing issues,
                //     but it does kinda mess with how the reference counting should work...

                //tfs: This should not get hit now, with the new splitting. We should be able to remove all refcount stuff
                //Debug.Assert(refCount == 0);
            }

            void OnElementOnLoaded(object frameworkElement, object o)
            {
                if (loading && element.IsInVisualTree())
                {
                    return;
                }
                AddBinding();
                //Debug.WriteLine($"Binding {id,-5} {(loading ? "Loading" : "Loaded")} :         RefCount = {refCount,5}, {element.GetType().Name}");
            }

            void RemoveBinding()
            {
                element.Loading -= OnElementOnLoaded;
                element.Loaded -= OnElementOnLoaded;
                element.Unloaded -= OnElementOnUnloaded;
                binding.Remove(handler);
                if (token != -1)
                {
                    element.UnregisterPropertyChangedCallback(property, token);
                }
                refCount = 0;
            }

            AddRemoveBindingAction(element, property, RemoveBinding);
        }
    }
}
