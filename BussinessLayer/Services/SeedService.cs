using System;
using System.IO;
using System.Linq;
using DataAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using BussinessLayer.Interfaces;

namespace BussinessLayer.Services
{
    public class SeedService : ISeedService
    {
        private readonly RagchatbotDbContext _db;

        public SeedService(RagchatbotDbContext db)
        {
            _db = db;
        }

        public async Task SeedAsync()
        {
            // 0. Auto-migration: Add user_id to documents table if not exists
            try
            {
                await _db.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (
                        SELECT * FROM sys.columns 
                        WHERE object_id = OBJECT_ID('documents') AND name = 'user_id'
                    )
                    BEGIN
                        ALTER TABLE documents ADD user_id BIGINT NULL;
                    END
                ");
                Console.WriteLine("[Seed] Verified user_id column in documents table");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Seed] Error adding user_id column: " + ex.Message);
            }

            // Auto-migration: Add content_hash to documents table if not exists
            try
            {
                await _db.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (
                        SELECT * FROM sys.columns 
                        WHERE object_id = OBJECT_ID('documents') AND name = 'content_hash'
                    )
                    BEGIN
                        ALTER TABLE documents ADD content_hash VARCHAR(64) NULL;
                    END
                ");
                Console.WriteLine("[Seed] Verified content_hash column in documents table");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Seed] Error adding content_hash column: " + ex.Message);
            }

            // Hash existing documents if content_hash is null
            try
            {
                var unhashedDocs = await _db.Documents
                    .Where(d => d.ContentHash == null || d.ContentHash == "")
                    .ToListAsync();

                if (unhashedDocs.Any())
                {
                    foreach (var doc in unhashedDocs)
                    {
                        if (File.Exists(doc.FilePath))
                        {
                            using (var stream = File.OpenRead(doc.FilePath))
                            {
                                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                                {
                                    var hashBytes = sha256.ComputeHash(stream);
                                    doc.ContentHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                                }
                            }
                        }
                    }
                    await _db.SaveChangesAsync();
                    Console.WriteLine($"[Seed] Hashed {unhashedDocs.Count} existing documents.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Seed] Error hashing existing documents: " + ex.Message);
            }

            // 1. ChunkingStrategy id=1
            if (!await _db.ChunkingStrategies.AnyAsync(s => s.Id == 1))
            {
                await _db.Database.ExecuteSqlRawAsync(@"
                    SET IDENTITY_INSERT chunking_strategies ON;
                    INSERT INTO chunking_strategies (id, name, chunk_size, chunk_overlap, description)
                    VALUES (1, 'Fixed-size (1000 tokens)', 1000, 100, 'Default fixed-size chunking strategy');
                    SET IDENTITY_INSERT chunking_strategies OFF;");
                Console.WriteLine("[Seed] Created default ChunkingStrategy (id=1)");
            }

            // 2. EmbeddingModel id=1
            if (!await _db.EmbeddingModels.AnyAsync(m => m.Id == 1))
            {
                await _db.Database.ExecuteSqlRawAsync(@"
                    SET IDENTITY_INSERT embedding_models ON;
                    INSERT INTO embedding_models (id, name, provider, dimension, is_free, description)
                    VALUES (1, 'text-embedding-3-small', 'OpenAI', 1536, 0, 'OpenAI text-embedding-3-small (1536 dims)');
                    SET IDENTITY_INSERT embedding_models OFF;");
                Console.WriteLine("[Seed] Created default EmbeddingModel (id=1)");
            }

            // 3. User id=1 (admin)
            if (!await _db.Users.AnyAsync(u => u.Id == 1))
            {
                string hashed = PasswordHelper.HashPassword("admin123");
                await _db.Database.ExecuteSqlRawAsync($@"
                    SET IDENTITY_INSERT users ON;
                    INSERT INTO users (id, full_name, email, role, password_hash, is_active, created_at)
                    VALUES (1, 'Demo User', 'demo@ragassistant.local', 'admin', '{hashed}', 1, GETDATE());
                    SET IDENTITY_INSERT users OFF;");
                Console.WriteLine("[Seed] Created demo User (id=1) with hashed password admin123");
            }

            // 4. User id=2 (benchmarkmanager)
            if (!await _db.Users.AnyAsync(u => u.Email == "benchmark@ragassistant.local"))
            {
                try 
                {
                    // Drop existing check constraint and recreate it to allow benchmarkmanager
                    await _db.Database.ExecuteSqlRawAsync(@"
                        IF EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_users_role')
                        BEGIN
                            ALTER TABLE users DROP CONSTRAINT CK_users_role;
                        END
                        ALTER TABLE users ADD CONSTRAINT CK_users_role CHECK (role IN ('admin', 'lecturer', 'student', 'benchmarkmanager'));
                    ");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[Seed] Warning when updating CK_users_role constraint: " + ex.Message);
                }

                string hashed = PasswordHelper.HashPassword("bench123");
                await _db.Database.ExecuteSqlRawAsync($@"
                    INSERT INTO users (full_name, email, role, password_hash, is_active, created_at)
                    VALUES ('Benchmark Manager', 'benchmark@ragassistant.local', 'benchmarkmanager', '{hashed}', 1, GETDATE());
                ");
                Console.WriteLine("[Seed] Created benchmarkmanager User with hashed password bench123");
            }
        }
    }
}
