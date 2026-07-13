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

            // Auto-migration: Add uploaded_by column to documents table if not exists
            try
            {
                await _db.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (
                        SELECT * FROM sys.columns 
                        WHERE object_id = OBJECT_ID('documents') AND name = 'uploaded_by'
                    )
                    BEGIN
                        ALTER TABLE documents ADD uploaded_by BIGINT NULL;
                    END
                ");
                Console.WriteLine("[Seed] Verified uploaded_by column in documents table");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Seed] Error adding uploaded_by column: " + ex.Message);
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

            // Auto-migration: Add token limit columns to users table if not exists
            try
            {
                await _db.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (
                        SELECT * FROM sys.columns 
                        WHERE object_id = OBJECT_ID('users') AND name = 'available_tokens'
                    )
                    BEGIN
                        ALTER TABLE users ADD available_tokens INT NOT NULL DEFAULT 20;
                    END

                    IF NOT EXISTS (
                        SELECT * FROM sys.columns 
                        WHERE object_id = OBJECT_ID('users') AND name = 'last_token_update_time'
                    )
                    BEGIN
                        ALTER TABLE users ADD last_token_update_time DATETIME2 NOT NULL DEFAULT GETDATE();
                    END

                    IF NOT EXISTS (
                        SELECT * FROM sys.columns 
                        WHERE object_id = OBJECT_ID('users') AND name = 'is_pro'
                    )
                    BEGIN
                        ALTER TABLE users ADD is_pro BIT NOT NULL DEFAULT 0;
                    END
                ");
                Console.WriteLine("[Seed] Verified token columns in users table");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Seed] Error adding token columns to users: " + ex.Message);
            }

            // Auto-migration: Ensure subject_id is nullable in chat_sessions table
            try
            {
                await _db.Database.ExecuteSqlRawAsync(@"
                    ALTER TABLE chat_sessions ALTER COLUMN subject_id BIGINT NULL;
                ");
                Console.WriteLine("[Seed] Verified subject_id is nullable in chat_sessions table");

                // Fix historical incorrect revenue data
                await _db.Database.ExecuteSqlRawAsync(@"
                    UPDATE pro_upgrades SET amount = 49000 WHERE amount = 99000;
                ");
                Console.WriteLine("[Seed] Fixed historical pro_upgrades revenue");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Seed] Error altering chat_sessions subject_id or updating pro_upgrades: " + ex.Message);
            }

            // Auto-migration: Create pro_upgrades table if not exists
            try
            {
                await _db.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'pro_upgrades')
                    BEGIN
                        CREATE TABLE [dbo].[pro_upgrades](
                            [id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [user_id] BIGINT NOT NULL,
                            [amount] DECIMAL(18,2) NOT NULL,
                            [payment_method] VARCHAR(50) NOT NULL DEFAULT 'VNPay',
                            [transaction_id] NVARCHAR(255) NULL,
                            [upgraded_at] DATETIME2(7) NULL DEFAULT (GETDATE()),
                            CONSTRAINT [FK_pro_upgrades_users] FOREIGN KEY([user_id]) REFERENCES [dbo].[users]([id])
                        );
                        CREATE NONCLUSTERED INDEX [IX_pro_upgrades_user_id] ON [dbo].[pro_upgrades]([user_id]);
                    END
                ");
                Console.WriteLine("[Seed] Verified pro_upgrades table");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Seed] Error creating pro_upgrades table: " + ex.Message);
            }

            // Auto-migration: Create token_usage_logs table if not exists
            try
            {
                await _db.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'token_usage_logs')
                    BEGIN
                        CREATE TABLE [dbo].[token_usage_logs](
                            [id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [user_id] BIGINT NOT NULL,
                            [tokens_used] INT NOT NULL,
                            [action] VARCHAR(50) NOT NULL DEFAULT 'chat',
                            [used_at] DATETIME2(7) NULL DEFAULT (GETDATE()),
                            CONSTRAINT [FK_token_usage_logs_users] FOREIGN KEY([user_id]) REFERENCES [dbo].[users]([id])
                        );
                        CREATE NONCLUSTERED INDEX [IX_token_usage_logs_user_id] ON [dbo].[token_usage_logs]([user_id]);
                        CREATE NONCLUSTERED INDEX [IX_token_usage_logs_used_at] ON [dbo].[token_usage_logs]([used_at]);
                    END
                ");
                Console.WriteLine("[Seed] Verified token_usage_logs table");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Seed] Error creating token_usage_logs table: " + ex.Message);
            }

            // 1. ChunkingStrategy id=1, 2, 3
            if (!await _db.ChunkingStrategies.AnyAsync(s => s.Id == 1))
            {
                await _db.Database.ExecuteSqlRawAsync(@"
                    SET IDENTITY_INSERT chunking_strategies ON;
                    INSERT INTO chunking_strategies (id, name, chunk_size, chunk_overlap, description)
                    VALUES (1, 'Fixed-size (1000 tokens)', 1000, 100, 'Default fixed-size chunking strategy');
                    SET IDENTITY_INSERT chunking_strategies OFF;");
                Console.WriteLine("[Seed] Created default ChunkingStrategy (id=1)");
            }
            if (!await _db.ChunkingStrategies.AnyAsync(s => s.Id == 2))
            {
                await _db.Database.ExecuteSqlRawAsync(@"
                    SET IDENTITY_INSERT chunking_strategies ON;
                    INSERT INTO chunking_strategies (id, name, chunk_size, chunk_overlap, description)
                    VALUES (2, 'Recursive Character (500 chars)', 500, 50, 'Recursive character chunking strategy with separators');
                    SET IDENTITY_INSERT chunking_strategies OFF;");
                Console.WriteLine("[Seed] Created ChunkingStrategy (id=2)");
            }
            if (!await _db.ChunkingStrategies.AnyAsync(s => s.Id == 3))
            {
                var jsonParams = "{\"window\": 3, \"step\": 2}";
                await _db.Database.ExecuteSqlInterpolatedAsync($@"
                    SET IDENTITY_INSERT chunking_strategies ON;
                    INSERT INTO chunking_strategies (id, name, chunk_size, chunk_overlap, description, params)
                    VALUES (3, 'Sentence-Window (3 sentences)', 0, 0, 'Sentence-window chunking strategy (3 sentences, step 2)', {jsonParams});
                    SET IDENTITY_INSERT chunking_strategies OFF;");
                Console.WriteLine("[Seed] Created ChunkingStrategy (id=3)");
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
                await _db.Database.ExecuteSqlInterpolatedAsync($@"
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
                        DECLARE @ConstraintName nvarchar(200);
                        SELECT @ConstraintName = name FROM sys.check_constraints 
                        WHERE parent_object_id = OBJECT_ID('users') AND definition LIKE '%role%';
                        
                        IF @ConstraintName IS NOT NULL
                        BEGIN
                            EXEC('ALTER TABLE users DROP CONSTRAINT ' + @ConstraintName);
                        END
                        
                        ALTER TABLE users ADD CONSTRAINT CK_users_role CHECK (role IN ('admin', 'lecturer', 'student', 'benchmarkmanager'));
                    ");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[Seed] Warning when updating CK_users_role constraint: " + ex.Message);
                }

                string hashed = PasswordHelper.HashPassword("bench123");
                await _db.Database.ExecuteSqlInterpolatedAsync($@"
                    INSERT INTO users (full_name, email, role, password_hash, is_active, created_at)
                    VALUES ('Benchmark Manager', 'benchmark@ragassistant.local', 'benchmarkmanager', '{hashed}', 1, GETDATE());
                ");
                Console.WriteLine("[Seed] Created benchmarkmanager User with hashed password bench123");
            }
        }
    }
}
