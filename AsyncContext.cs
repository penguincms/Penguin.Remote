using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Penguin.Remote
{
    public class AsyncContext<TReturn> : IAsyncContext
    {
        public AsyncContext(Func<Task<TReturn>> toExecute)
        {
            this.ToExecute = toExecute;
        }

        public void Execute()
        {
            this.ExecutingTask = Task.Run(async () =>
            {
                TReturn returnVal = await this.ToExecute();
                this.TaskCompletionSource.SetResult(returnVal);
            });
        }

        [SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "I want to make sure this task doesn't disppear")]
        private Task ExecutingTask { get; set; }

        public void SetResult(TReturn result) => this.TaskCompletionSource.SetResult(result);

        private readonly Func<Task<TReturn>> ToExecute;

        private readonly TaskCompletionSource<TReturn> TaskCompletionSource = new();

        public Task<TReturn> Return => this.TaskCompletionSource.Task;

        Task IAsyncContext.Task => this.Return;

        int IAsyncContext.QueuePosition { get; set; }
    }
}