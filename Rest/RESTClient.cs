using System;
using System.Collections.Generic;
using DashShared;
using Microsoft.Extensions.DependencyInjection;

namespace Dash
{
    public class RESTClient
    {

        private static readonly Lazy<RESTClient> lazy = new Lazy<RESTClient>(() => new RESTClient());

        public static RESTClient Instance => lazy.Value;

        public IModelEndpoint<T> GetEndpoint<T>() where T : EntityBase
        {
            return App.Instance.Container.GetRequiredService<IModelEndpoint<T>>();
        }
        public IModelEndpoint<FieldModel> Fields => GetEndpoint<FieldModel>();
    }
}
