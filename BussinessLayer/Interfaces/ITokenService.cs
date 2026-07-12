using System;

namespace BussinessLayer.Interfaces
{
    public interface ITokenService
    {
        string GenerateVerificationToken(string email, DateTime expiry);
        bool ValidateVerificationToken(string token, out string email);
    }
}
