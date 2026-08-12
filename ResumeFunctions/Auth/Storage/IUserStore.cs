using ResumeFunctions.Auth.Models;

namespace ResumeFunctions.Auth.Storage
{
    public interface IUserStore
    {
        Task<UserAccountEntity?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default);

        /// <returns>false if the username is already taken.</returns>
        Task<bool> CreateAsync(UserAccountEntity user, CancellationToken cancellationToken = default);

        Task UpdateAsync(UserAccountEntity user, CancellationToken cancellationToken = default);

        /// <summary>All registered accounts. A full partition scan - acceptable at this site's
        /// account volume (used by #45's resume-poster directory, filtered to admin roles by
        /// the caller); revisit if the Users table ever grows large enough for this to matter.</summary>
        Task<IReadOnlyList<UserAccountEntity>> ListAsync(CancellationToken cancellationToken = default);
    }
}
