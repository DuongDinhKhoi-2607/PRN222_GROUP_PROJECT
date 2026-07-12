using BussinessLayer.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BussinessLayer.Interfaces
{
    public interface IUserService
    {
        Task<UserDto?> GetByIdAsync(long id);
        Task<UserDto?> AuthenticateAsync(string email, string password);
        Task<bool> EmailExistsAsync(string email);
        Task<UserDto> RegisterUserAsync(RegisterDto dto);
        Task<bool> ActivateUserAsync(string email);
        Task<IEnumerable<UserDto>> GetAllLecturersAsync();
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<bool> DeleteUserAsync(long userId);
        Task<bool> ChangePasswordAsync(long userId, string currentPassword, string newPassword);
        Task<bool> IsUsingTempPasswordAsync(long userId);
    }
}
