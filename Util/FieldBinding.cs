using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Microsoft.Toolkit.Uwp.Helpers;
using static Dash.BindingMap;

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
        void ConvertToXaml(DependencyObject element, DependencyProperty property, Context context);
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
        public bool CanBeNull { get; set; }

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

        public void ConvertToXaml(DependencyObject element, DependencyProperty property, Context context)
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
                    if (!CanBeNull)
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
                    }

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
        public delegate void RemoveAction(DependencyObject obj);
        public static readonly DependencyProperty BindingMapProperty =
        DependencyProperty.RegisterAttached( "BindingMap",
          typeof(Dictionary<DependencyProperty, RemoveAction>),
          typeof(BindingMap),
          new PropertyMetadata(null)
        );
        public static void SetBindingMap(DependencyObject element, Dictionary<DependencyProperty, RemoveAction> value)
        {
            element.SetValue(BindingMapProperty, value);
        }
        public static Dictionary<DependencyProperty, RemoveAction> GetBindingMap(DependencyObject element)
        {
            return (Dictionary<DependencyProperty, RemoveAction>)element.GetValue(BindingMapProperty);
        }
    }

    public static class BindingExtension
    {
        public static void AddFieldBinding<T>(this T element, DependencyProperty property, IFieldBinding binding) where T : DependencyObject
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

        private static bool TryRemoveOldBinding(DependencyObject element, DependencyProperty property)
        {
            if (GetBindingMap(element) == null)
            {
                return false;
            }
            var dict = GetBindingMap(element);
            if (!dict.ContainsKey(property))
            {
                return false;
            }
            var t = dict[property];
            t(element);
            dict.Remove(property);
            return true;
        }

        private static void AddRemoveBindingAction(DependencyObject element, DependencyProperty property, RemoveAction removeBinding)
        {
            if (GetBindingMap(element) == null)
            {
                SetBindingMap(element, new Dictionary<DependencyProperty, RemoveAction>());
            }

            GetBindingMap(element)[property] = removeBinding;
        }

        private static void AddOneTimeBinding(DependencyObject element, DependencyProperty property, IFieldBinding binding)
        {
            binding.ConvertToXaml(element, property, binding.Context);
        }

        private static void AddOneWayBinding(DependencyObject element, DependencyProperty property, IFieldBinding binding)
        {
            var wHandler = new WeakEventListener<DependencyObject, DocumentController,
                    DocumentController.DocumentFieldUpdatedEventArgs>(element)
                {
                    OnEventAction = (instance, controller, arg3) => {
                    var dargs = arg3.FieldArgs as ListController<DocumentController>.ListFieldUpdatedEventArgs;
                    if (dargs == null || dargs.ListAction != ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Content)
            
                        binding.ConvertToXaml(instance, property, binding.Context);
                        },
                    OnDetachAction = listener => binding.Remove(listener.OnEvent)
                };

            binding.ConvertToXaml(element, property, binding.Context);
            binding.Add(wHandler.OnEvent);


            void RemoveBinding(DependencyObject depObj)
            {
                binding.Remove(wHandler.OnEvent);
            }

            AddRemoveBindingAction(element, property, RemoveBinding);
        }

        private static void AddTwoWayBinding(DependencyObject element, DependencyProperty property, IFieldBinding binding)
        {
            bool updateUI = true;

            var wHandler = new WeakEventListener<DependencyObject, DocumentController,
                    DocumentController.DocumentFieldUpdatedEventArgs>(element);
            wHandler.OnEventAction = (instance, controller, arg3) =>
            {
                updateUI = false;
                binding.ConvertToXaml(instance, property, binding.Context);
                updateUI = true;
            };
            wHandler.OnDetachAction = listener => binding.Remove(listener.OnEvent);

            void Callback(DependencyObject sender, DependencyProperty dp)
            {
                if (updateUI && !binding.ConvertFromXaml(sender.GetValue(dp)))
                {
                    binding.ConvertToXaml(sender, property, binding.Context);
                }
            }

            binding.ConvertToXaml(element, property, binding.Context);
            binding.Add(wHandler.OnEvent);
            var token = element.RegisterPropertyChangedCallback(property, Callback);

            void RemoveBinding(DependencyObject depObj)
            {
                binding.Remove(wHandler.OnEvent);
                if (token != -1)
                {
                    depObj.UnregisterPropertyChangedCallback(property, token);
                }
            }

            AddRemoveBindingAction(element, property, RemoveBinding);
        }
    }
}
