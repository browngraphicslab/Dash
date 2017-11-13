using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DashShared;
using Newtonsoft.Json;

namespace Dash
{
    /// <summary>
    /// This should be the base class for all view models which allows for easy binding!
    /// <para>
    /// You can use this class as follows
    /// <para>
    /// <code>public string Name { get { return _name; } set { SetProperty(ref _name, value); }}</code>
    /// </para>
    /// </para>
    /// The value is only set when the property has changed. SetProperty can be extended to provide
    /// validation when the property is being changed or to call an event like PropertyChanging
    /// </summary>
    public class ViewModelBase : INotifyPropertyChanged
    {
        /// <summary>
        /// Fired when the property is changed
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Called by SetProperty, you should not be using this directly
        /// </summary>
        /// <param name="propertyName"></param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Sets the property to a new value or does nothing if the property already has the value which is being passed in
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="storage"></param>
        /// <param name="value"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        protected virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
