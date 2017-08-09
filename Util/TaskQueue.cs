using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dash
{
    public class TaskQueue
    {
        private SemaphoreSlim semaphore;

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
            await semaphore.WaitAsync();
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