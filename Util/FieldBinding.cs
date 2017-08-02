using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Data;
using DashShared;

namespace Dash
{
    public delegate void SetHandler<in T>(T field, object value) where T : FieldModelController;
    public delegate object GetHandler<in T>(T field) where T : FieldModelController;

    public class FieldBinding<T> where T : FieldModelController
    {
        public BindingMode Mode;
        public DocumentController Document;
        public Key Key;
        public SetHandler<T> SetHandler;
        public GetHandler<T> GetHandler;

        public Context Context;

        public IValueConverter Converter;
        public object ConverterParameter;
    }
}
