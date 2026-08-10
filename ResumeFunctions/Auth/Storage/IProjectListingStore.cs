using ResumeFunctions.Auth.Models;

namespace ResumeFunctions.Auth.Storage
{
    public interface IProjectListingStore
    {
        Task<IReadOnlyList<ProjectListingEntity>> ListByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default);

        Task<ProjectListingEntity?> FindByIdAsync(string id, CancellationToken cancellationToken = default);

        Task<ProjectListingEntity?> FindFeaturedByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default);

        Task AddAsync(ProjectListingEntity listing, CancellationToken cancellationToken = default);

        Task UpdateAsync(ProjectListingEntity listing, CancellationToken cancellationToken = default);

        /// <returns>false if no listing with that id existed.</returns>
        Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    }
}
