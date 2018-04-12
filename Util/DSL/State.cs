using DashShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public abstract class State<T> : EntityBase
    {
        private readonly Dictionary<T, FieldControllerBase> _dictionary;

        public State(IEnumerable<KeyValuePair<T, FieldControllerBase>> existingState = null)
        {
            _dictionary = existingState?.ToDictionary(k => k.Key, v => v.Value) ?? new Dictionary<T, FieldControllerBase>();
        }

        public State(State<T> existingState = null) : this(existingState?._dictionary?.ToArray()) { }

        protected Dictionary<T, FieldControllerBase> GetCopy()
        {
            return _dictionary.ToArray().ToDictionary(k => k.Key, v => v.Value);
        }

        /// <summary>
        /// Gets the specified value from this state object.
        /// </summary>
        /// <param name="variableName"></param>
        /// <returns></returns>
        public FieldControllerBase this[T variableName]
        {
            get { return _dictionary.ContainsKey(variableName) ? _dictionary[variableName] : null; }
        }

        protected abstract State<T> CreateNew(IEnumerable<KeyValuePair<T, FieldControllerBase>> existingState = null);

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
            return CreateNew(d);
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
                return CreateNew(d);
            }
            _dictionary.Remove(variableName);
            return this;
        }
    }
}
