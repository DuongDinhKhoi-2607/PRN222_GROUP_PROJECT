using DataAccessLayer.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace DataAccessLayer.Repositories
{
    public class ChatSessionRepository
    {
        private readonly RagchatbotDbContext _db;
        public ChatSessionRepository(RagchatbotDbContext db) { _db = db; }

        public async Task<ChatSession> AddAsync(ChatSession session)
        {
            _db.ChatSessions.Add(session);
            await _db.SaveChangesAsync();
            return session;
        }

        public async Task<IEnumerable<ChatSession>> GetByUserIdAsync(long userId)
        {
            return await _db.ChatSessions
                .Include(s => s.Subject)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt)
                .ToListAsync();
        }

        public async Task<ChatSession?> GetByIdAsync(long id) =>
            await _db.ChatSessions
                .Include(s => s.Subject)
                .FirstOrDefaultAsync(s => s.Id == id);

        public async Task UpdateTitleAsync(long sessionId, string title)
        {
            var session = await _db.ChatSessions.FindAsync(sessionId);
            if (session != null)
            {
                session.Title = title;
                await _db.SaveChangesAsync();
            }
        }

        public async Task UpdateSubjectAsync(long sessionId, long? subjectId)
        {
            var session = await _db.ChatSessions.FindAsync(sessionId);
            if (session != null)
            {
                session.SubjectId = subjectId;
                session.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }
    }
}
