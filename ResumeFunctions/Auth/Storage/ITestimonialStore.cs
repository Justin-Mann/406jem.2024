using ResumeFunctions.Auth.Models;

namespace ResumeFunctions.Auth.Storage
{
    public interface ITestimonialStore
    {
        Task<IReadOnlyList<TestimonialEntity>> ListAsync(CancellationToken cancellationToken = default);

        Task AddAsync(TestimonialEntity testimonial, CancellationToken cancellationToken = default);

        /// <returns>false if no testimonial with that id existed.</returns>
        Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    }
}
