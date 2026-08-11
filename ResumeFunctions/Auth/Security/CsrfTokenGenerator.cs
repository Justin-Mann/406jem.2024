using System.Security.Cryptography;

namespace ResumeFunctions.Auth.Security
{
    public static class CsrfTokenGenerator
    {
        public static string Generate() => RandomNumberGenerator.GetHexString(64);
    }
}
