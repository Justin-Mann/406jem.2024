using Microsoft.Extensions.Logging;
using ResumeFunctions.Auth.Models;

namespace ResumeFunctions.Auth.Storage
{
    /// <summary>
    /// Shared by every write path that can change what a featured resume serves (#39):
    /// ResumeAdminApi's create/update, and ResumeParsingApi's AI-extraction update. A snapshot
    /// write failure is logged and swallowed here so it can never fail the caller's response.
    /// </summary>
    public static class ResumeSnapshotHelper
    {
        public static async Task TrySaveSnapshotAsync(IResumeSnapshotStore snapshotStore, ILogger logger, ResumeEntity resume)
        {
            if (!resume.IsFeatured || string.IsNullOrWhiteSpace(resume.PayloadJson))
            {
                return;
            }

            try
            {
                await snapshotStore.SaveAsync(resume.OwnerUserId, resume.PayloadJson);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to write fallback snapshot for owner '{Owner}'; the triggering call itself succeeded.", resume.OwnerUserId);
            }
        }

        /// <summary>Best-effort snapshot removal — see ResumeAdminApi.Delete (#39 review: a
        /// deleted featured resume must not keep being served via a stale snapshot).</summary>
        public static async Task TryDeleteSnapshotAsync(IResumeSnapshotStore snapshotStore, ILogger logger, string ownerUserId)
        {
            try
            {
                await snapshotStore.DeleteAsync(ownerUserId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete fallback snapshot for owner '{Owner}'; the triggering call itself succeeded.", ownerUserId);
            }
        }
    }
}
