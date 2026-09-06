using System.Threading;

using UnityEngine;

namespace CrawfisSoftware.Utility
{
    /// <summary>
    /// The waits Unity's Awaitable does not ship. Every await in this project takes a
    /// CancellationToken - a MonoBehaviour's destroyCancellationToken, or a controller's own
    /// CancellationTokenSource - so an async method dies with its object the way a coroutine
    /// did. See CLAUDE.md, "Async and coroutines".
    /// </summary>
    public static class Wait
    {
        /// <summary>
        /// Awaitable equivalent of WaitForSecondsRealtime - ignores Time.timeScale.
        /// Awaitable.WaitForSecondsAsync is scaled, so it never completes while the game is
        /// paused (timeScale = 0); use this for anything that must keep running through a pause.
        /// </summary>
        public static async Awaitable ForSecondsRealtime(float seconds, CancellationToken token)
        {
            float remaining = seconds;
            while (remaining > 0f)
            {
                await Awaitable.NextFrameAsync(token);
                remaining -= Time.unscaledDeltaTime;
            }
        }
    }
}
