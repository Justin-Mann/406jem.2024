using ResumeFunctions.Auth.Models;

namespace ResumeFunctions.Auth.Storage
{
    public interface IUserStore
    {
        Task<UserAccountEntity?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default);

        /// <returns>false if the username is already taken.</returns>
        Task<bool> CreateAsync(UserAccountEntity user, CancellationToken cancellationToken = default);

        Task UpdateAsync(UserAccountEntity user, CancellationToken cancellationToken = default);
    }
}
