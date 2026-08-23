using System.Security.Cryptography;
using System.Text;

namespace THMS.Configuration
{
    public static class SecretProtector
    {
        public static byte[] Protect(string plainText)
        {
            var data = Encoding.UTF8.GetBytes(plainText);
            return ProtectedData.Protect(
                data,
                optionalEntropy: null,
                scope: DataProtectionScope.CurrentUser);
        }

        public static string Unprotect(byte[] protectedData)
        {
            var data = ProtectedData.Unprotect(
                protectedData,
                optionalEntropy: null,
                scope: DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(data);
        }
    }
}
