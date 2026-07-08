using System.Security.Cryptography;
using System.Text;
using PlanCatalog.Core.Ports;

namespace PlanCatalog.Infrastructure.Hashing;

public sealed class Sha256ContentHasher : IContentHasher
{
    public string ComputeHash(string canonicalJsonWithoutContentHash)
    {
        var bytes = Encoding.UTF8.GetBytes(canonicalJsonWithoutContentHash);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }
}
