using ResumeFunctions.Auth.Security;
using Xunit;

namespace ResumeFunctions.Tests.Auth;

public class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Hash_NeverReturnsThePlaintextPassword()
    {
        var hash = _hasher.Hash("correct horse battery staple");
        Assert.DoesNotContain("correct horse battery staple", hash);
    }

    [Fact]
    public void Verify_ReturnsTrue_ForCorrectPassword()
    {
        var hash = _hasher.Hash("correct horse battery staple");
        Assert.True(_hasher.Verify("correct horse battery staple", hash));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForWrongPassword()
    {
        var hash = _hasher.Hash("correct horse battery staple");
        Assert.False(_hasher.Verify("wrong password", hash));
    }

    [Fact]
    public void Hash_ProducesDifferentOutput_ForSamePasswordDueToRandomSalt()
    {
        var hash1 = _hasher.Hash("same password");
        var hash2 = _hasher.Hash("same password");
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Verify_ReturnsFalse_ForMalformedHash()
    {
        Assert.False(_hasher.Verify("anything", "not-a-valid-hash"));
    }
}
