using System.Collections.Concurrent;
using System.Linq;

namespace DashWebServer
{
    /// <summary>
    /// https://stackoverflow.com/a/10299662
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FixedSizedQueue<T> : ConcurrentQueue<T>
    {
        private readonly object syncObject = new object();

        public int Size { get; private set; }

        public FixedSizedQueue(int size)
        {
            Size = size;
        }

        public new void Enqueue(T obj)
        {
            base.Enqueue(obj);
            lock (syncObject)
            {
                while (Count > Size)
                {
                    TryDequeue(out _);
                }
            }
        }
    }

    class ClientSnapshots : FixedSizedQueue<Snapshot>
    {
        private int _count = 1;

        public ClientSnapshots(int size) : base(size)
        {
        }

        public new void Enqueue(Snapshot obj)
        {
            base.Enqueue(obj);
            obj.Id = _count;
            _count++;
        }

        public Snapshot LastAcknowledgedSnapshot()
        {
            foreach (var snapshot in this.Reverse())
            {
                if (snapshot.IsAcknowledged)
                {
                    return snapshot;
                }
            }

            return null;
        }
    }
}
