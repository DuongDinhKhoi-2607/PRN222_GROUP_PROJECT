using DataAccessLayer.Repositories;
using DataAccessLayer.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using BussinessLayer.Interfaces;
using BussinessLayer.DTOs;

namespace BussinessLayer.Services
{
    public class UserService : IUserService
    {
        private readonly UserRepository _userRepo;

        public UserService(UserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        private static UserDto MapToDto(User u) => new UserDto
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            Role = u.Role,
            CreatedAt = u.CreatedAt,
            IsActive = u.IsActive
        };

        public async Task<UserDto?> GetByIdAsync(long id)
        {
            var u = await _userRepo.GetByIdAsync(id);
            return u == null ? null : MapToDto(u);
        }

        public async Task<UserDto?> AuthenticateAsync(string email, string password)
        {
            var user = await _userRepo.GetByEmailAsync(email);
            if (user == null || !user.IsActive.GetValueOrDefault(true))
                return null;

            bool isValid = PasswordHelper.VerifyPassword(password, user.PasswordHash);
            if (!isValid)
                return null;

            return MapToDto(user);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _userRepo.AnyEmailAsync(email);
        }

        public async Task<UserDto> RegisterUserAsync(RegisterDto dto)
        {
            var user = new User
            {
                FullName = dto.FullName.Trim(),
                Email = dto.Email.Trim(),
                Role = dto.Role.Trim().ToLower(),
                PasswordHash = PasswordHelper.HashPassword(dto.Password),
                IsActive = dto.IsActive,
                CreatedAt = DateTime.Now
            };

            var created = await _userRepo.AddAsync(user);
            return MapToDto(created);
        }

        public async Task<bool> ActivateUserAsync(string email)
        {
            var user = await _userRepo.GetByEmailAsync(email);
            if (user == null) return false;

            user.IsActive = true;
            await _userRepo.UpdateAsync(user);
            return true;
        }

        public async Task<IEnumerable<UserDto>> GetAllLecturersAsync()
        {
            var users = await _userRepo.GetAllByRoleAsync("lecturer");
            return users.Select(MapToDto);
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _userRepo.GetAllAsync();
            return users.Select(MapToDto);
        }

        public async Task<bool> DeleteUserAsync(long userId)
        {
            return await _userRepo.SoftDeleteUserAsync(userId);
        }

        public async Task<bool> ChangePasswordAsync(long userId, string currentPassword, string newPassword)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return false;

            if (!PasswordHelper.VerifyPassword(currentPassword, user.PasswordHash))
                return false;

            user.PasswordHash = PasswordHelper.HashPassword(newPassword);
            await _userRepo.UpdateAsync(user);
            return true;
        }

        public async Task<bool> IsUsingTempPasswordAsync(long userId)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return false;

            return PasswordHelper.VerifyPassword("1234@AbcD", user.PasswordHash);
        }

        private async Task CheckAndRegenerateTokensAsync(User user)
        {
            if (user.IsPro) return; // Pro doesn't need regeneration limits

            var now = DateTime.UtcNow;
            var elapsedMinutes = (now - user.LastTokenUpdateTime).TotalMinutes;
            if (elapsedMinutes >= 20)
            {
                int tokensToAdd = (int)(elapsedMinutes / 20);
                user.AvailableTokens += tokensToAdd;
                
                // Soft cap at 20 tokens for free users
                if (user.AvailableTokens > 20)
                {
                    user.AvailableTokens = 20;
                }
                
                // Update the last token update time by adding the chunks of 20 mins
                user.LastTokenUpdateTime = user.LastTokenUpdateTime.AddMinutes(tokensToAdd * 20);
                await _userRepo.UpdateAsync(user);
            }
        }

        public async Task<(int AvailableTokens, bool IsPro)> GetUserTokenInfoAsync(long userId)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return (0, false);

            await CheckAndRegenerateTokensAsync(user);
            return (user.AvailableTokens, user.IsPro);
        }

        public async Task<bool> DeductTokenAsync(long userId, int amount = 4)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return false;

            if (user.IsPro) return true; // Pro users have unlimited access

            await CheckAndRegenerateTokensAsync(user);

            if (user.AvailableTokens >= amount)
            {
                user.AvailableTokens -= amount;
                await _userRepo.UpdateAsync(user);
                return true;
            }

            return false;
        }

        public async Task<bool> UpgradeToProAsync(long userId)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return false;

            user.IsPro = true;
            await _userRepo.UpdateAsync(user);
            return true;
        }
    }
}
