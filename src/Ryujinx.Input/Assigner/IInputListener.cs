using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ryujinx.Input.Assigner
{
    public interface IInputListener : IDisposable
    {
        /// <summary>
        /// Waits for the first button pressed and returns it.
        /// Returns null if the operation is cancelled or times out.
        /// </summary>
        Task<Button?> ListenForButtonAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Waits for one or more buttons to be held simultaneously, then returns the full set once all are released.
        /// Returns an empty array if the operation is cancelled or times out.
        /// </summary>
        Task<Button[]> ListenForComboAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default);
    }
}
