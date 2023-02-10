using System.Threading.Tasks;

namespace Penguin.Remote
{
    public interface IAsyncContext
    {
        public void Execute();

        Task Task { get; }

        internal int QueuePosition { get; set; }
    }
}
