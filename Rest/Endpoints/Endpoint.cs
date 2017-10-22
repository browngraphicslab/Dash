using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Gaming.Input;
using DashShared;

namespace Dash.Rest.Endpoints
{
    public abstract class Endpoint<T, T2> where T : EntityBase where T2 : EntityBase
    {
        protected class EndpointBatch
        {
            public ConcurrentDictionary<Action<List<Tuple<T, Action<T2>, Action<Exception>>>>, ConcurrentBag<Tuple<T, Action<T2>, Action<Exception>>>> Batches =
                new ConcurrentDictionary<Action<List<Tuple<T, Action<T2>, Action<Exception>>>>, ConcurrentBag<Tuple<T, Action<T2>, Action<Exception>>>>();
        }

        private readonly Timer _tick;
        private readonly EndpointBatch batches = new EndpointBatch();
        private Object l = new Object();

        protected Endpoint()
        {
            int numSecondsBetweenBatches = 1;
            _tick = new Timer(ProcessBatches, null, 0, numSecondsBetweenBatches * 1000);
        }

        protected void ProcessBatches(Object o)
        {
            lock (l)
            {
                foreach (var k in batches.Batches.Keys)
                {
                    var bag = batches.Batches[k];
                    if (bag.IsEmpty) continue;
                    k(bag.ToList());
                    batches.Batches[k] = new ConcurrentBag<Tuple<T, Action<T2>, Action<Exception>>>();
                }
            }
        }

        protected void AddBatchHandler(Action<List<Tuple<T, Action<T2>, Action<Exception>>>> handler)
        {
            lock (l)
            {
                batches.Batches[handler] = new ConcurrentBag<Tuple<T, Action<T2>, Action<Exception>>>();
            }
        }

        protected void AddRequest(Action<List<Tuple<T, Action<T2>, Action<Exception>>>> handler, Tuple<T, Action<T2>, Action<Exception>> request)
        {
            lock (l)
            {
                var bag = batches.Batches[handler];
                bag.Add(request);
                if (bag.Count > 700)
                {
                    handler(bag.ToList());
                    batches.Batches[handler] = new ConcurrentBag<Tuple<T, Action<T2>, Action<Exception>>>();
                }
            }
        }
    }
}
