using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Dash
{
    class FieldMultiBinding<TXaml> : IFieldBinding
    {
        public String Tag { get; set; }
        public BindingMode Mode { get; set; }
        public Context Context { get; set; }

        private FieldReference[] _references;

        public XamlDereferenceLevel XamlAssignmentDereferenceLevel = XamlDereferenceLevel.DereferenceToRoot;
        public XamlDereferenceLevel FieldAssignmentDereferenceLevel = XamlDereferenceLevel.DereferenceOneLevel;
        public Object FallbackValue;
        public bool CanBeNull = false;

        public SafeDataToXamlConverter<List<object>, TXaml> Converter;//TODO Should this be a list of objects of of FieldControllerBase?
        public Object ConverterParameter;

        public FieldMultiBinding(params FieldReference[] refs)
        {
            _references = refs;
        }

        public void ConvertToXaml(DependencyObject element, DependencyProperty property, Context context)
        {
            if (Converter == null)//We can't evaluate a multibinding without a converter
            {
                return;
            }
            var fields = new List<object>(_references.Length);
            bool foundNullField = false;
            if (XamlAssignmentDereferenceLevel == XamlDereferenceLevel.DereferenceOneLevel)
            {
                foreach (var fieldReference in _references)
                {
                    var fieldControllerBase = fieldReference.Dereference(context);
                    if (!CanBeNull && fieldControllerBase == null)
                    {
                        foundNullField = true;
                        break;
                    }
                    fields.Add(fieldControllerBase?.GetValue(context));
                }
            }
            else if (XamlAssignmentDereferenceLevel == XamlDereferenceLevel.DereferenceToRoot)
            {
                foreach (var fieldReference in _references)
                {
                    var fieldControllerBase = fieldReference.DereferenceToRoot(context);
                    if (!CanBeNull && fieldControllerBase == null)
                    {
                        foundNullField = true;
                        break;
                    }
                    fields.Add(fieldControllerBase?.GetValue(context));
                }
            }
            if (!foundNullField)
            {
                var data = Converter.ConvertDataToXaml(fields, ConverterParameter);
                if (data != null)
                {
                    element.SetValue(property, data);
                }
                else
                {
#if PRINT_BINDING_ERROR_DETAILED
                        Debug.WriteLine(
                            $"Error evaluating binding: Error with converter or GetValue\n" +
                            $"  Converter   = {Converter?.GetType().Name ?? "null"}\n" +
                            $"  Tag         = {(string.IsNullOrWhiteSpace(Tag) ? "<empty>" : Tag)}");
#else
                    Debug.WriteLine($"Error evaluating binding: Error with converter or GetValue of MultiBinding, #define PRINT_BINDING_ERROR_DETAILED to print more detailed");
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
                        $"  Tag         = {(string.IsNullOrWhiteSpace(Tag) ? "<empty>" : Tag)}");
#else
                Debug.WriteLine($"Error evaluating binding: Field was missing and there was no fallback value, #define PRINT_BINDING_ERROR_DETAILED to print more detailed");
#endif

                element.ClearValue(property);
            }
        }

        public bool ConvertFromXaml(object xamlData)
        {
            if (xamlData is TXaml)
            {
                var fieldData = Converter.ConvertXamlToData((TXaml)xamlData, ConverterParameter);
                if (fieldData.Count != _references.Length)
                {
                    return false;
                }
                if (FieldAssignmentDereferenceLevel == XamlDereferenceLevel.DereferenceOneLevel)
                {
                    for (int i = 0; i < fieldData.Count; ++i)
                    {
                        _references[i].Dereference(Context).TrySetValue(fieldData[i]);
                    }
                } else if (FieldAssignmentDereferenceLevel == XamlDereferenceLevel.DereferenceToRoot)
                {
                    for (int i = 0; i < fieldData.Count; ++i)
                    {
                        _references[i].DereferenceToRoot(Context).TrySetValue(fieldData[i]);
                    }
                }
                return true;
            }
            return false;
        }

        public void Add(DocumentController.DocumentUpdatedHandler handler)
        {
            foreach (var fieldReference in _references)
            {
                fieldReference.GetDocumentController(Context).AddFieldUpdatedListener(fieldReference.FieldKey, handler);
            }
        }

        public void Remove(DocumentController.DocumentUpdatedHandler handler)
        {
            foreach (var fieldReference in _references)
            {
                fieldReference.GetDocumentController(Context).RemoveFieldUpdatedListener(fieldReference.FieldKey, handler);
            }
        }
    }
}
