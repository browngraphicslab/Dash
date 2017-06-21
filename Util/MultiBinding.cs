using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Dash
{
    class MultiBinding<T> : ViewModelBase
    {
        private T _property;
        public T Property
        {
            get { return _property; }
            set { SetProperty(ref _property, value); }
        }

        private IValueConverter _converter;
        private object _converterParameter;

        public MultiBinding(IValueConverter converter, object converterParameter)
        {
            _converter = converter;
            _converterParameter = converterParameter;
        }

        public void AddBinding(UIElement element, DependencyProperty prop)
        {
            element.RegisterPropertyChangedCallback(prop, Callback);
            Callback(element, prop);
        }

        private void Callback(DependencyObject sender, DependencyProperty dp)
        {
            if (_converter == null)
            {
                var newValue = (T) sender.GetValue(dp);
                Property = newValue;
            }
            else
            {
                var newValue = (T)_converter.Convert(sender.GetValue(dp), typeof(T), _converterParameter, "en");
                Property = newValue;
            }
        }
    }
}
