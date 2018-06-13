﻿using System;
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

    public interface IFieldBinding
    {
        String Tag { get; set; }
        BindingMode Mode { get; set; }
        Context Context { get; set; }
        void ConvertToXaml(FrameworkElement element, DependencyProperty property, Context context);
        bool ConvertFromXaml(object xamlData);

        void Add(FieldControllerBase.FieldUpdatedHandler handler);
        void Remove(FieldControllerBase.FieldUpdatedHandler handler);
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

        public void Add(FieldControllerBase.FieldUpdatedHandler handler)
        {
            Document.AddFieldUpdatedListener(Key, handler);
        }

        public void Remove(FieldControllerBase.FieldUpdatedHandler handler)
        {
            Document.RemoveFieldUpdatedListener(Key, handler);
        }
    }

    public class FieldBinding<T> : FieldBinding<T, TextController> where T : FieldControllerBase
    {
        public FieldBinding([CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = "",
            [CallerFilePath] string path = "") : base(lineNumber, caller, path)
        {
        }
    }

    public static class BindingExtension
    {
        private static Dictionary<UIElement, Dictionary<DependencyProperty,
            Action>> _bindingMap =
            new Dictionary<UIElement, Dictionary<DependencyProperty, Action>>();

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

        private static void AddOneTimeBinding<T>(T element, DependencyProperty property, IFieldBinding binding) where T : FrameworkElement
        {
            binding.ConvertToXaml(element, property, binding.Context);
        }

        private static void AddOneWayBinding<T>(T element, DependencyProperty property, IFieldBinding binding) where T : FrameworkElement
        {
            FieldControllerBase.FieldUpdatedHandler handler =
                (sender, args, context) =>
                {
                    if (binding.Context == null)
                    {
                        binding.ConvertToXaml(element, property, context);

                    }
                    else
                    //if (binding.Context.IsCompatibleWith(context))
                    {
                        var equals = binding.Context.DocContextList.Where((d) => (d.DocumentType.Type == null || (!d.DocumentType.Type.Contains("Box") && !d.DocumentType.Type.Contains("Layout"))) && !context.DocContextList.Contains(d));
                        binding.ConvertToXaml(element, property, equals.Count() == 0 ? context : binding.Context);
                    }
                };

            int refCount = 0;
            if (element.ActualWidth != 0 || element.ActualHeight != 0) // element.IsInVisualTree())
            {
                binding.ConvertToXaml(element, property, binding.Context);
                binding.Add(handler);
                refCount++;
            }

            void OnElementOnUnloaded(object sender, RoutedEventArgs args)
            {
                if (--refCount == 0)
                {
                    binding.Remove(handler);
                }
            }

            void OnElementOnLoaded(object sender, RoutedEventArgs args)
            {
                if (refCount++ == 0)
                {
                    binding.ConvertToXaml(element, property, binding.Context);
                    binding.Add(handler);
                }
            }

            element.Unloaded += OnElementOnUnloaded;

            element.Loaded += OnElementOnLoaded;

            void RemoveBinding()
            {
                element.Loaded -= OnElementOnLoaded;
                element.Unloaded -= OnElementOnUnloaded;
                binding.Remove(handler);
                refCount = 0;
            }

            AddRemoveBindingAction(element, property, RemoveBinding);
        }

        private static void AddTwoWayBinding<T>(T element, DependencyProperty property, IFieldBinding binding)
            where T : FrameworkElement
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
                    //if (binding.Context.IsCompatibleWith(context))
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
            
            long token = -1;
            int refCount = 0;
            if (element.ActualWidth != 0 || element.ActualHeight != 0) // element.IsInVisualTree())
            {
                binding.ConvertToXaml(element, property, binding.Context);
                binding.Add(handler);
                token = element.RegisterPropertyChangedCallback(property, callback);
                refCount++;
            }

            element.Loaded += OnElementOnLoaded;
            element.Unloaded += OnElementOnUnloaded;
            void OnElementOnUnloaded(object sender, RoutedEventArgs args)
            {
                if (--refCount == 0)
                {
                    binding.Remove(handler);
                    element.UnregisterPropertyChangedCallback(property, token);
                    token = -1;
                }
            }

            void OnElementOnLoaded(object sender, RoutedEventArgs args)
            {
                if (refCount++ == 0)
                {
                    binding.ConvertToXaml(element, property, binding.Context);
                    binding.Add(handler);
                    token = element.RegisterPropertyChangedCallback(property, callback);
                }
            }

            void RemoveBinding()
            {
                element.Loaded   -= OnElementOnLoaded;
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
