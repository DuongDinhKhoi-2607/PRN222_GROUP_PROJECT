using DataAccessLayer.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories
{
    public class UserRepository
    {
        private readonly RagchatbotDbContext _db;
        public UserRepository(RagchatbotDbContext db) { _db = db; }

        public async Task<User?> GetByIdAsync(long id)
        {
            return await _db.Users.FindAsync(id);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _db.Users.FirstOrDefaultAsync(u => u.Email == email.Trim());
        }

        public async Task<bool> AnyEmailAsync(string email)
        {
            return await _db.Users.AnyAsync(u => u.Email == email.Trim());
        }

        public async Task<User> AddAsync(User user)
        {
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return user;
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _db.Users
                .Where(u => !u.Email.StartsWith("deleted_"))
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetAllByRoleAsync(string role)
        {
            return await _db.Users
                .Where(u => u.Role == role && u.IsActive == true && !u.Email.StartsWith("deleted_"))
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }

        public async Task UpdateAsync(User user)
        {
            _db.Users.Update(user);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> SoftDeleteUserAsync(long userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return false;

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // 1. Clean up LecturerUploadPermissions for this lecturer to free up subjects
                var permissions = _db.LecturerUploadPermissions.Where(p => p.LecturerId == userId);
                _db.LecturerUploadPermissions.RemoveRange(permissions);

                // 2. Perform soft delete on the user and rename their email
                user.IsActive = false;
                user.Email = $"deleted_{user.Id}_{user.Email.Trim()}";
                _db.Users.Update(user);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
