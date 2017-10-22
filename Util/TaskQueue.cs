using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Dash
{
    public class TaskQueue
    {
        private SemaphoreSlim semaphore;

        private object lockObject = new object();

        private int taskCount = 0;

        public TaskQueue(int numSimultaneousTasks)
        {
            semaphore = new SemaphoreSlim(numSimultaneousTasks);
        }

        public async Task<T> Enqueue<T>(Func<Task<T>> taskGenerator)
        {
            await semaphore.WaitAsync();
            try
            {
                return await taskGenerator();
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task Enqueue(Func<Task> taskGenerator)
        {
            //lock (lockObject)
            //{
            //    Debug.WriteLine("taskCount: " + taskCount++);
            //}

            await semaphore.WaitAsync();
            lock (lockObject)
            {
                Debug.WriteLine("Starting task: " + taskCount++);
            }
            try
            {
                await taskGenerator();
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}