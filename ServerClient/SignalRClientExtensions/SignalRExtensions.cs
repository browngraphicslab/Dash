﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Client
{
    /// <summary>
    ///     Provides several extension methods.
    /// </summary>
    public static class SignalRExtensions
    {
        /// <summary>
        ///     Creates a strongly typed hub proxy with the specified hub name.
        /// </summary>
        /// <param name="connection">The <see cref="T:Microsoft.AspNet.SignalR.Client.HubConnection" />HubConnection.</param>
        /// <param name="hubName">The name of the hub.</param>
        /// <typeparam name="TServerHubInterface">The interface of the server hub.</typeparam>
        /// <typeparam name="TClientInterface">The interface of the client events.</typeparam>
        public static IHubProxy<TServerHubInterface, TClientInterface> CreateHubProxy
            <TServerHubInterface, TClientInterface>(this HubConnection connection,
                string hubName)
            where TServerHubInterface : class
            where TClientInterface : class
        {
            return new HubProxy<TServerHubInterface, TClientInterface>(connection, hubName);
        }

        /// <summary>
        ///     Creates a strongly typed hub proxy.
        /// </summary>
        /// <param name="hubProxy">IHubProxy.</param>
        /// <typeparam name="TServerHubInterface">The interface of the server hub.</typeparam>
        /// <typeparam name="TClientInterface">The interface of the client events.</typeparam>
        public static IHubProxy<TServerHubInterface, TClientInterface> AsHubProxy
            <TServerHubInterface, TClientInterface>(this IHubProxy hubProxy)
            where TServerHubInterface : class
            where TClientInterface : class
        {
            return new HubProxy<TServerHubInterface, TClientInterface>(hubProxy);
        }
    }
}
