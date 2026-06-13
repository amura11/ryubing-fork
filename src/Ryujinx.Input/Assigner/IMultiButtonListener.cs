using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ryujinx.Input.Assigner
{
    /// <summary>
    /// Listens for one or more simultaneously held buttons and returns them as a set.
    /// </summary>
    public interface IMultiButtonListener
    {
        Task<Button[]> ListenAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default);
    }
}
