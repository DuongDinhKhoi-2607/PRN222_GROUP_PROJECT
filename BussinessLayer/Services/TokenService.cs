using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using BussinessLayer.Interfaces;

namespace BussinessLayer.Services
{
    public class TokenService : ITokenService
    {
        private readonly string _secret;

        public TokenService(IConfiguration configuration)
        {
            var secret = configuration["EmailSettings:TokenSecret"];
            _secret = string.IsNullOrEmpty(secret) ? "RagChatbotSecretKeyForTokensChangeMe2026!" : secret;
        }

        public string GenerateVerificationToken(string email, DateTime expiry)
        {
            var expiryUnix = ((DateTimeOffset)expiry).ToUnixTimeSeconds();
            var payload = $"{email.Trim().ToLower()}|{expiryUnix}";
            var payloadBytes = Encoding.UTF8.GetBytes(payload);
            var payloadBase64 = Convert.ToBase64String(payloadBytes);

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secret));
            var hashBytes = hmac.ComputeHash(payloadBytes);
            var hashBase64 = Convert.ToBase64String(hashBytes);

            // Replace standard base64 characters that are unsafe in URLs
            return $"{payloadBase64.Replace('+', '-').Replace('/', '_').Replace('=', '~')}.{hashBase64.Replace('+', '-').Replace('/', '_').Replace('=', '~')}";
        }

        public bool ValidateVerificationToken(string token, out string email)
        {
            email = string.Empty;
            try
            {
                var parts = token.Split('.');
                if (parts.Length != 2) return false;

                var payloadBase64 = parts[0].Replace('-', '+').Replace('_', '/').Replace('~', '=');
                var hashBase64 = parts[1].Replace('-', '+').Replace('_', '/').Replace('~', '=');

                var payloadBytes = Convert.FromBase64String(payloadBase64);
                var payload = Encoding.UTF8.GetString(payloadBytes);

                var payloadParts = payload.Split('|');
                if (payloadParts.Length != 2) return false;

                var tokenEmail = payloadParts[0];
                var expiryUnixStr = payloadParts[1];

                if (!long.TryParse(expiryUnixStr, out var expiryUnix)) return false;

                var expiry = DateTimeOffset.FromUnixTimeSeconds(expiryUnix).DateTime;
                if (DateTime.UtcNow > expiry.ToUniversalTime()) return false;

                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secret));
                var computedHashBytes = hmac.ComputeHash(payloadBytes);
                var computedHashBase64 = Convert.ToBase64String(computedHashBytes);

                if (hashBase64 == computedHashBase64)
                {
                    email = tokenEmail;
                    return true;
                }
            }
            catch
            {
                // Return false on any decoding/parsing failures
            }
            return false;
        }
    }
}
