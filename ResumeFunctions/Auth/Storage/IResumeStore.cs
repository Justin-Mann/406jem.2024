using ResumeFunctions.Auth.Models;

namespace ResumeFunctions.Auth.Storage
{
    public interface IResumeStore
    {
        Task<IReadOnlyList<ResumeEntity>> ListByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default);

        Task<ResumeEntity?> FindByIdAsync(string id, CancellationToken cancellationToken = default);

        Task<ResumeEntity?> FindFeaturedByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default);

        Task AddAsync(ResumeEntity resume, CancellationToken cancellationToken = default);

        Task UpdateAsync(ResumeEntity resume, CancellationToken cancellationToken = default);

        /// <returns>false if no resume with that id existed.</returns>
        Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    }
}
