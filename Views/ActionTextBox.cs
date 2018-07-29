using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Dash
{
    public class ActionTextBox : TextBox
    {
        private readonly Dictionary<VirtualKey, Action<KeyRoutedEventArgs>> _actions = new Dictionary<VirtualKey, Action<KeyRoutedEventArgs>>();

        public void AddKeyHandler(VirtualKey key, Action<KeyRoutedEventArgs> action)
        {
            if (_actions.ContainsKey(key))
            {
                _actions[key] += action;
            }
            else
            {
                _actions[key] = action;
            }
        }

        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            if (_actions.ContainsKey(e.Key))
            {
                _actions[e.Key].Invoke(e);
            }
            if(!e.Handled)
            {
                base.OnKeyDown(e);
            }
        }
    }
}
