using System;
using System.Collections.Generic;
using DashShared;
using Microsoft.Extensions.DependencyInjection;

namespace Dash
{
    public class RESTClient
    {

        private static readonly Lazy<RESTClient> lazy = new Lazy<RESTClient>(() => new RESTClient());

        public static RESTClient Instance { get { return lazy.Value; } }

        private Dictionary<Type, object> _dict = new Dictionary<Type, object>();

        public IModelEndpoint<T> GetEndpoint<T>() where T:EntityBase
        {
            if (!_dict.ContainsKey(typeof(T)))
            {
                _dict[typeof(T)] = new ContentUpdatingEndpointWrapper<T>((BaseModelEndpoint<T>)App.Instance.Container.GetRequiredService<IModelEndpoint<T>>());
            }
            return (_dict[typeof(T)]) as IModelEndpoint<T>;
        }
        public IModelEndpoint<FieldModel> Fields => GetEndpoint<FieldModel>();   
    }
}
