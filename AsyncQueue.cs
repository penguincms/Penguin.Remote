using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Penguin.Remote
{
    public class AsyncQueue
    {
        public AsyncQueue(int maxExecutingTasks = 10)
        {
            this.CurrentlyExecuting = new IAsyncContext[maxExecutingTasks];
        }

        private Task Worker;
        private readonly SemaphoreSlim WorkerSemaphore = new SemaphoreSlim(1);
        private readonly SemaphoreSlim ProcessSemaphore = new SemaphoreSlim(1);

        private bool disposedValue;

        private bool WorkerRunning = false;
        private readonly IAsyncContext[] CurrentlyExecuting;

        private readonly ConcurrentQueue<IAsyncContext> Queue = new ConcurrentQueue<IAsyncContext>();

        /// <summary>
        /// Disposes of the class, and flushes the queue
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void FillSlot(int slot)
        {
            if (this.Queue.TryDequeue(out IAsyncContext thisContext))
            {
                thisContext.QueuePosition = slot;

                this.CurrentlyExecuting[slot] = thisContext;

                thisContext.Execute();
            }
            else
            {
                this.CurrentlyExecuting[slot] = null;
            }
        }

        private async Task LoopProcess()
        {
            await this.ProcessSemaphore.WaitAsync();

            await this.WorkerSemaphore.WaitAsync();

            while (!this.Queue.IsEmpty)
            {
                this.WorkerSemaphore.Release();

                for (int i = 0; i < this.CurrentlyExecuting.Length; i++)
                {
                    if (this.CurrentlyExecuting[i] is null)
                    {
                        this.FillSlot(i);
                    }
                }

                do
                {
                    List<IAsyncContext> realTasks = this.CurrentlyExecuting.Where(c => c != null).ToList();

                    List<Task> taskObjects = realTasks.Select(c => c.Task).ToList();

                    Task completedTask = await Task.WhenAny(taskObjects);

                    int completed = taskObjects.IndexOf(completedTask);

                    if (completed != -1)
                    {
                        this.FillSlot(realTasks[completed].QueuePosition);
                    }
                } while (this.CurrentlyExecuting.Any(t => t != null));

                this.WorkerSemaphore.Wait();
            }

            this.WorkerRunning = false;

            _ = this.WorkerSemaphore.Release();

            _ = this.ProcessSemaphore.Release();
        }

        /// <summary>
        /// Disposes of the class, and flushes the queue
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.LoopProcess().Wait();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                this.disposedValue = true;
            }
        }

        /// <summary>
        /// Adds a new line of text to the internal queue, to be flushed by the background thread
        /// </summary>
        /// <param name="toEnque">The string to Enqueue</param>
        public async Task<TResult> Execute<TResult>(Func<Task<TResult>> toExecute)
        {
            //Dont let the worker check its state while we're mucking with it
            await this.WorkerSemaphore.WaitAsync();

            AsyncContext<TResult> asyncContext = new(toExecute);

            //Add the line to print
            this.Queue.Enqueue(asyncContext);

            //Set internally by the worker before it exits, so more accurate than IsBusy
            if (this.Worker is null || !this.WorkerRunning || this.Worker.IsCompleted)
            {
                //Now that its exited we start it again
                this.Worker = Task.Run(this.LoopProcess);

                //Set this so we dont immediately try and start it again
                this.WorkerRunning = true;
            }

            //Now the worker can do things
            this.WorkerSemaphore.Release();

            return await asyncContext.Return;
        }
    }
}