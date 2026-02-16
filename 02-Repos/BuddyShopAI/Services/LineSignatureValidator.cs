using System.Security.Cryptography;
using System.Text;

namespace BuddyShopAI.Services;

public class LineSignatureValidator
{
    private readonly string _channelSecret;

    public LineSignatureValidator(string channelSecret)
    {
        _channelSecret = channelSecret ?? throw new ArgumentNullException(nameof(channelSecret));
    }

    public bool ValidateSignature(string requestBody, string? signature)
    {
        if (string.IsNullOrEmpty(signature))
        {
            return false;
        }

        try
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_channelSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(requestBody));
            var computedSignature = Convert.ToBase64String(hash);
            
            return signature == computedSignature;
        }
        catch
        {
            return false;
        }
    }
}
