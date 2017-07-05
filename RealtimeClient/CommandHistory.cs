using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    /// <summary>
    /// https://stackoverflow.com/a/10299662
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FixedSizedStack<T> : ConcurrentQueue<T>
    {
        private readonly object syncObject = new object();

        public int Size { get; private set; }

        public FixedSizedStack(int size)
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

    class CommandHistory : FixedSizedStack<Command>
    {
        public CommandHistory(int size) : base(size)
        {
        }

        public List<Command> SinceTime(long timeInMiliseconds)
        {
            var commands = new List<Command>();
            foreach (var command in this)
            {
                if (command.Time > timeInMiliseconds)
                {
                    commands.Add(command);
                }
            }

            return commands;
        }
    }
}
