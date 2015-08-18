using System;
using System.IO;
using System.Security.Cryptography;

namespace Utils.Aws.App.Helpers
{
    public static class HashHelper
    {
        public static string SHA256HashString(Stream stream)
        {
            var sha256 = SHA256.Create();
            var treeHash = sha256.ComputeHash(stream);
            var treeHashString =
                BitConverter
                    .ToString(treeHash)
                    .Replace("-", "")
                    .ToLower();

            stream.Position = 0;

            return treeHashString;
        }
    }
}
