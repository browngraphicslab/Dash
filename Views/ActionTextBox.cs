using System;
using System.Collections.Generic;
using System.Linq;
using Windows.System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Dash
{
    public class ActionTextBox : TextBox
    {
        private Dictionary<VirtualKey, Action<KeyRoutedEventArgs>> _actions = new Dictionary<VirtualKey, Action<KeyRoutedEventArgs>>();

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

        public void ClearHandlers(params VirtualKey[] keysToClear)
        {
            if (keysToClear.Length == 0)
            {
                _actions.Clear();
                return;
            }
            //foreach (VirtualKey virtualKey in keysToClear)
            //{
            //    _actions.Remove(virtualKey);
            //}
            _actions = new Dictionary<VirtualKey, Action<KeyRoutedEventArgs>>(_actions.Where(k => !keysToClear.Contains(k.Key)));
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
