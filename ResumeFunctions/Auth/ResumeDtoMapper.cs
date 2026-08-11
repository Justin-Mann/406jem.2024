using System.Text.Json;
using ResumeFunctions.Auth.Models;
using ResumeFunctions.Models;

namespace ResumeFunctions.Auth.Dtos
{
    /// <summary>Shared ResumeEntity -> ResumeDto mapping, used by both ResumeAdminApi and
    /// ResumeParsingApi (#30) so the two don't drift.</summary>
    internal static class ResumeDtoMapper
    {
        public static ResumeDto ToDto(ResumeEntity entity)
        {
            DigitalResumeModel? payload = null;
            try
            {
                payload = JsonSerializer.Deserialize<DigitalResumeModel>(entity.PayloadJson);
            }
            catch (JsonException)
            {
                // Leave payload null rather than fail the whole response over one bad row.
            }

            return new ResumeDto(
                entity.RowKey,
                entity.OwnerUserId,
                entity.IsFeatured,
                payload,
                entity.CreatedAtUtc,
                entity.UpdatedAtUtc,
                entity.Status,
                entity.OriginalFileName,
                entity.ContentType,
                entity.FileSizeBytes);
        }
    }
}
