using DashShared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public abstract class State<T> : EntityBase
    {
        /// <summary>
        /// dictionary used to track the evolution of states.
        ///  Some states can be followed as they add and remove values and this dictionary will allow us to folow them
        /// </summary>
        private static Dictionary<string, State<T>> _trackingDictionary = new Dictionary<string, State<T>>();

        protected readonly Dictionary<T, FieldControllerBase> _dictionary;
        private string _trackingId = null;

        protected State(IEnumerable<KeyValuePair<T, FieldControllerBase>> existingState = null, string trackingId = null)
        {
            _dictionary = existingState?.ToDictionary(k => k.Key, v => v.Value) ?? new Dictionary<T, FieldControllerBase>();
            _trackingId = trackingId;
            if (_trackingId != null)
            {
                _trackingDictionary[_trackingId] = this;
            }
        }

        public string TrackingId => _trackingId;

        protected State(State<T> existingState = null, string trackingId = null) : this(existingState?._dictionary?.ToArray(), trackingId) { }

        public static State<T> GetTrackedState(string trackingId)
        {
            Debug.Assert(trackingId != null);
            return _trackingDictionary[trackingId];
        }

        public static bool RemoveTrackedState(string trackingId)
        {
            Debug.Assert(trackingId != null);
            return _trackingDictionary.Remove(trackingId);
        }

        protected Dictionary<T, FieldControllerBase> GetCopy()
        {
            return _dictionary.ToArray().ToDictionary(k => k.Key, v => v.Value);
        }

        public bool IsTracked => _trackingId != null && _trackingDictionary.ContainsKey(_trackingId);

        /// <summary>
        /// Gets the specified value from this state object.
        /// </summary>
        /// <param name="variableName"></param>
        /// <returns></returns>
        public FieldControllerBase this[T variableName]
        {
            get { return _dictionary.ContainsKey(variableName) ? _dictionary[variableName] : null; }
        }

        protected abstract State<T> CreateNew(IEnumerable<KeyValuePair<T, FieldControllerBase>> existingState = null, string trackingId = null);

        /// <summary>
        /// public method to add or update a value and return the new state including that value. 
        ///  The returned state will be copied and not affect 'this' state
        /// </summary>
        /// <param name="variableName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual State<T> AddOrUpdateValue(T variableName, FieldControllerBase value)
        {
            var d = GetCopy();
            d[variableName] = value;
            return CreateNew(d, _trackingId);
        }

        /// <summary>
        /// public method to return a state without the specified key.
        /// bool returnNewState specifies if the function should modify and return this State, or create a new one and return it.
        /// bool throwExceptionIfNotExists specifies if this function should throw an exception if the specified key isnt found;
        /// </summary>
        /// <param name="variableName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual State<T> RemoveValue(T variableName, bool returnNewState = true, bool throwExceptionIfNotExists = false)
        {
            if (throwExceptionIfNotExists && !_dictionary.ContainsKey(variableName))
            {
                throw new KeyNotFoundException($"The key {variableName.ToString()} didn't exist in the state so it couldn't be removed");
            }
            if (returnNewState)
            {
                var d = GetCopy();
                d.Remove(variableName);
                return CreateNew(d, _trackingId);
            }
            _dictionary.Remove(variableName);
            return this;
        }
    }
}
