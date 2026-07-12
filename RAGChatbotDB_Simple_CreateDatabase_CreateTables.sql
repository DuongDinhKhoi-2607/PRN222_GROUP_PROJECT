/*
RAGChatbotDB - Simple SQL Server Create Script

Bản này chỉ giữ các phần cơ bản:
- CREATE DATABASE
- USE database
- CREATE TABLE
- CREATE INDEX
- DEFAULT, FOREIGN KEY, CHECK constraints

Đã bỏ các cấu hình database như COMPATIBILITY_LEVEL, FULLTEXT, QUERY_STORE,
SCOPED CONFIGURATION, RECOVERY, BROKER, READ_WRITE, FILESTREAM, v.v.
*/

CREATE DATABASE [RAGChatbotDB]
GO

USE [RAGChatbotDB]
GO

CREATE TABLE [dbo].[__EFMigrationsHistory](
	[MigrationId] [nvarchar](150) NOT NULL,
	[ProductVersion] [nvarchar](32) NOT NULL,
 CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY CLUSTERED 
(
	[MigrationId] ASC
)
)

GO
CREATE TABLE [dbo].[chapters](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[subject_id] [bigint] NOT NULL,
	[title] [nvarchar](255) NOT NULL,
	[order_index] [int] NOT NULL,
	[created_at] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)
)

GO
CREATE TABLE [dbo].[chat_messages](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[session_id] [bigint] NOT NULL,
	[llm_model_id] [bigint] NULL,
	[role] [varchar](20) NOT NULL,
	[content] [nvarchar](max) NOT NULL,
	[latency_ms] [int] NULL,
	[token_usage] [int] NULL,
	[created_at] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)
)

GO
CREATE TABLE [dbo].[chat_sessions](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[user_id] [bigint] NOT NULL,
	[subject_id] [bigint] NULL,
	[title] [nvarchar](255) NULL,
	[created_at] [datetime2](7) NULL,
	[updated_at] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)
)

GO
CREATE TABLE [dbo].[chunk_embeddings](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[chunk_id] [bigint] NOT NULL,
	[embedding_model_id] [bigint] NOT NULL,
	[vector] [nvarchar](max) NOT NULL,
	[dimension] [int] NOT NULL,
	[created_at] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)
)

GO
CREATE TABLE [dbo].[chunking_strategies](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[name] [nvarchar](255) NOT NULL,
	[chunk_size] [int] NOT NULL,
	[chunk_overlap] [int] NOT NULL,
	[params] [nvarchar](max) NULL,
	[description] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)
)

GO
CREATE TABLE [dbo].[document_chunks](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[document_id] [bigint] NOT NULL,
	[chunking_strategy_id] [bigint] NOT NULL,
	[chunk_index] [int] NOT NULL,
	[content] [nvarchar](max) NOT NULL,
	[token_count] [int] NOT NULL,
	[page_number] [int] NULL,
	[metadata] [nvarchar](max) NULL,
	[created_at] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)
)

GO
CREATE TABLE [dbo].[documents](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[subject_id] [bigint] NOT NULL,
	[chapter_id] [bigint] NULL,
	[title] [nvarchar](255) NOT NULL,
	[file_name] [nvarchar](255) NOT NULL,
	[file_type] [varchar](20) NOT NULL,
	[file_path] [nvarchar](500) NOT NULL,
	[file_size] [bigint] NOT NULL,
	[status] [varchar](20) NOT NULL,
	[uploaded_at] [datetime2](7) NULL,
	[indexed_at] [datetime2](7) NULL,
	[user_id] [bigint] NULL,
	[uploaded_by] [bigint] NULL,
	[content_hash] [varchar](64) NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)
)

GO
CREATE TABLE [dbo].[embedding_models](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[name] [nvarchar](255) NOT NULL,
	[provider] [nvarchar](100) NOT NULL,
	[dimension] [int] NOT NULL,
	[is_free] [bit] NULL,
	[description] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)
)

GO
CREATE TABLE [dbo].[evaluation_results](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[experiment_run_id] [bigint] NOT NULL,
	[test_question_id] [bigint] NOT NULL,
	[generated_answer] [nvarchar](max) NULL,
	[retrieved_contexts] [nvarchar](max) NULL,
	[faithfulness] [float] NULL,
	[answer_relevancy] [float] NULL,
	[context_precision] [float] NULL,
	[context_recall] [float] NULL,
	[answer_correctness] [float] NULL,
	[latency_ms] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)
)

GO
CREATE TABLE [dbo].[experiment_run_metrics](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[experiment_run_id] [bigint] NOT NULL,
	[avg_faithfulness] [float] NULL,
	[avg_answer_relevancy] [float] NULL,
	[avg_context_precision] [float] NULL,
	[avg_context_recall] [float] NULL,
	[avg_answer_correctness] [float] NULL,
	[avg_latency_ms] [float] NULL,
	[total_questions] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
),
UNIQUE NONCLUSTERED 
(
	[experiment_run_id] ASC
)
)

GO
CREATE TABLE [dbo].[experiment_runs](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[experiment_id] [bigint] NOT NULL,
	[embedding_model_id] [bigint] NULL,
	[chunking_strategy_id] [bigint] NULL,
	[llm_model_id] [bigint] NULL,
	[run_name] [nvarchar](255) NULL,
	[params] [nvarchar](max) NULL,
	[started_at] [datetime2](7) NULL,
	[finished_at] [datetime2](7) NULL,
	[status] [varchar](20) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)
)

GO
CREATE TABLE [dbo].[experiments](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[name] [nvarchar](255) NOT NULL,
	[type] [varchar](50) NOT NULL,
	[description] [nvarchar](max) NULL,
	[status] [varchar](20) NOT NULL,
	[created_at] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)
)

GO
CREATE TABLE [dbo].[lecturer_upload_permissions](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[lecturer_id] [bigint] NOT NULL,
	[subject_id] [bigint] NOT NULL,
	[can_upload] [bit] NULL,
	[granted_by] [bigint] NOT NULL,
	[granted_at] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
),
 CONSTRAINT [UQ_lup_lecturer_subject] UNIQUE NONCLUSTERED 
(
	[lecturer_id] ASC,
	[subject_id] ASC
)
)

GO
CREATE TABLE [dbo].[llm_models](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[name] [nvarchar](255) NOT NULL,
	[type] [varchar](30) NOT NULL,
	[provider] [nvarchar](100) NOT NULL,
	[base_model] [nvarchar](255) NULL,
	[description] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)
)

GO
CREATE TABLE [dbo].[message_citations](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[message_id] [bigint] NOT NULL,
	[chunk_id] [bigint] NOT NULL,
	[document_id] [bigint] NOT NULL,
	[relevance_score] [float] NULL,
	[snippet] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)
)

GO
CREATE TABLE [dbo].[subjects](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[code] [varchar](50) NOT NULL,
	[name] [nvarchar](255) NOT NULL,
	[description] [nvarchar](max) NULL,
	[created_at] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
),
UNIQUE NONCLUSTERED 
(
	[code] ASC
)
)

GO
CREATE TABLE [dbo].[test_questions](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[subject_id] [bigint] NOT NULL,
	[question] [nvarchar](max) NOT NULL,
	[ground_truth] [nvarchar](max) NOT NULL,
	[reference_context] [nvarchar](max) NULL,
	[difficulty] [varchar](20) NULL,
	[created_at] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)
)

GO
CREATE TABLE [dbo].[users](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[full_name] [nvarchar](255) NOT NULL,
	[email] [nvarchar](255) NOT NULL,
	[role] [varchar](20) NOT NULL,
	[created_at] [datetime2](7) NULL,
	[password_hash] [nvarchar](500) NOT NULL,
	[is_active] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
),
UNIQUE NONCLUSTERED 
(
	[email] ASC
)
)

GO
CREATE NONCLUSTERED INDEX [IX_chat_messages_session_id] ON [dbo].[chat_messages]
(
	[session_id] ASC
)
GO
CREATE NONCLUSTERED INDEX [IX_chunk_embeddings_chunk_id] ON [dbo].[chunk_embeddings]
(
	[chunk_id] ASC
)
GO
CREATE NONCLUSTERED INDEX [IX_document_chunks_document_id] ON [dbo].[document_chunks]
(
	[document_id] ASC
)
GO
CREATE NONCLUSTERED INDEX [IX_documents_subject_id] ON [dbo].[documents]
(
	[subject_id] ASC
)
GO
CREATE NONCLUSTERED INDEX [IX_evaluation_results_run_id] ON [dbo].[evaluation_results]
(
	[experiment_run_id] ASC
)
GO
CREATE NONCLUSTERED INDEX [IX_message_citations_message_id] ON [dbo].[message_citations]
(
	[message_id] ASC
)
GO
ALTER TABLE [dbo].[chapters] ADD  DEFAULT (getdate()) FOR [created_at]
GO
ALTER TABLE [dbo].[chat_messages] ADD  DEFAULT (getdate()) FOR [created_at]
GO
ALTER TABLE [dbo].[chat_sessions] ADD  DEFAULT (getdate()) FOR [created_at]
GO
ALTER TABLE [dbo].[chat_sessions] ADD  DEFAULT (getdate()) FOR [updated_at]
GO
ALTER TABLE [dbo].[chunk_embeddings] ADD  DEFAULT (getdate()) FOR [created_at]
GO
ALTER TABLE [dbo].[document_chunks] ADD  DEFAULT (getdate()) FOR [created_at]
GO
ALTER TABLE [dbo].[documents] ADD  DEFAULT (getdate()) FOR [uploaded_at]
GO
ALTER TABLE [dbo].[embedding_models] ADD  DEFAULT ((1)) FOR [is_free]
GO
ALTER TABLE [dbo].[experiment_runs] ADD  DEFAULT (getdate()) FOR [started_at]
GO
ALTER TABLE [dbo].[experiments] ADD  DEFAULT (getdate()) FOR [created_at]
GO
ALTER TABLE [dbo].[lecturer_upload_permissions] ADD  DEFAULT ((1)) FOR [can_upload]
GO
ALTER TABLE [dbo].[lecturer_upload_permissions] ADD  DEFAULT (getdate()) FOR [granted_at]
GO
ALTER TABLE [dbo].[subjects] ADD  DEFAULT (getdate()) FOR [created_at]
GO
ALTER TABLE [dbo].[test_questions] ADD  DEFAULT (getdate()) FOR [created_at]
GO
ALTER TABLE [dbo].[users] ADD  DEFAULT (getdate()) FOR [created_at]
GO
ALTER TABLE [dbo].[users] ADD  DEFAULT ((1)) FOR [is_active]
GO
ALTER TABLE [dbo].[chapters]  WITH CHECK ADD  CONSTRAINT [FK_chapters_subjects] FOREIGN KEY([subject_id])
REFERENCES [dbo].[subjects] ([id])
GO
ALTER TABLE [dbo].[chapters] CHECK CONSTRAINT [FK_chapters_subjects]
GO
ALTER TABLE [dbo].[chat_messages]  WITH CHECK ADD  CONSTRAINT [FK_chat_messages_llm_models] FOREIGN KEY([llm_model_id])
REFERENCES [dbo].[llm_models] ([id])
GO
ALTER TABLE [dbo].[chat_messages] CHECK CONSTRAINT [FK_chat_messages_llm_models]
GO
ALTER TABLE [dbo].[chat_messages]  WITH CHECK ADD  CONSTRAINT [FK_chat_messages_sessions] FOREIGN KEY([session_id])
REFERENCES [dbo].[chat_sessions] ([id])
GO
ALTER TABLE [dbo].[chat_messages] CHECK CONSTRAINT [FK_chat_messages_sessions]
GO
ALTER TABLE [dbo].[chat_sessions]  WITH CHECK ADD  CONSTRAINT [FK_chat_sessions_subjects] FOREIGN KEY([subject_id])
REFERENCES [dbo].[subjects] ([id])
GO
ALTER TABLE [dbo].[chat_sessions] CHECK CONSTRAINT [FK_chat_sessions_subjects]
GO
ALTER TABLE [dbo].[chat_sessions]  WITH CHECK ADD  CONSTRAINT [FK_chat_sessions_users] FOREIGN KEY([user_id])
REFERENCES [dbo].[users] ([id])
GO
ALTER TABLE [dbo].[chat_sessions] CHECK CONSTRAINT [FK_chat_sessions_users]
GO
ALTER TABLE [dbo].[chunk_embeddings]  WITH CHECK ADD  CONSTRAINT [FK_chunk_embeddings_chunks] FOREIGN KEY([chunk_id])
REFERENCES [dbo].[document_chunks] ([id])
GO
ALTER TABLE [dbo].[chunk_embeddings] CHECK CONSTRAINT [FK_chunk_embeddings_chunks]
GO
ALTER TABLE [dbo].[chunk_embeddings]  WITH CHECK ADD  CONSTRAINT [FK_chunk_embeddings_embedding_models] FOREIGN KEY([embedding_model_id])
REFERENCES [dbo].[embedding_models] ([id])
GO
ALTER TABLE [dbo].[chunk_embeddings] CHECK CONSTRAINT [FK_chunk_embeddings_embedding_models]
GO
ALTER TABLE [dbo].[document_chunks]  WITH CHECK ADD  CONSTRAINT [FK_document_chunks_chunking_strategies] FOREIGN KEY([chunking_strategy_id])
REFERENCES [dbo].[chunking_strategies] ([id])
GO
ALTER TABLE [dbo].[document_chunks] CHECK CONSTRAINT [FK_document_chunks_chunking_strategies]
GO
ALTER TABLE [dbo].[document_chunks]  WITH CHECK ADD  CONSTRAINT [FK_document_chunks_documents] FOREIGN KEY([document_id])
REFERENCES [dbo].[documents] ([id])
GO
ALTER TABLE [dbo].[document_chunks] CHECK CONSTRAINT [FK_document_chunks_documents]
GO
ALTER TABLE [dbo].[documents]  WITH CHECK ADD  CONSTRAINT [FK_documents_chapters] FOREIGN KEY([chapter_id])
REFERENCES [dbo].[chapters] ([id])
GO
ALTER TABLE [dbo].[documents] CHECK CONSTRAINT [FK_documents_chapters]
GO
ALTER TABLE [dbo].[documents]  WITH CHECK ADD  CONSTRAINT [FK_documents_subjects] FOREIGN KEY([subject_id])
REFERENCES [dbo].[subjects] ([id])
GO
ALTER TABLE [dbo].[documents] CHECK CONSTRAINT [FK_documents_subjects]
GO
ALTER TABLE [dbo].[documents]  WITH CHECK ADD  CONSTRAINT [FK_documents_uploaded_by] FOREIGN KEY([uploaded_by])
REFERENCES [dbo].[users] ([id])
GO
ALTER TABLE [dbo].[documents] CHECK CONSTRAINT [FK_documents_uploaded_by]
GO
ALTER TABLE [dbo].[evaluation_results]  WITH CHECK ADD  CONSTRAINT [FK_evaluation_results_questions] FOREIGN KEY([test_question_id])
REFERENCES [dbo].[test_questions] ([id])
GO
ALTER TABLE [dbo].[evaluation_results] CHECK CONSTRAINT [FK_evaluation_results_questions]
GO
ALTER TABLE [dbo].[evaluation_results]  WITH CHECK ADD  CONSTRAINT [FK_evaluation_results_runs] FOREIGN KEY([experiment_run_id])
REFERENCES [dbo].[experiment_runs] ([id])
GO
ALTER TABLE [dbo].[evaluation_results] CHECK CONSTRAINT [FK_evaluation_results_runs]
GO
ALTER TABLE [dbo].[experiment_run_metrics]  WITH CHECK ADD  CONSTRAINT [FK_experiment_run_metrics_runs] FOREIGN KEY([experiment_run_id])
REFERENCES [dbo].[experiment_runs] ([id])
GO
ALTER TABLE [dbo].[experiment_run_metrics] CHECK CONSTRAINT [FK_experiment_run_metrics_runs]
GO
ALTER TABLE [dbo].[experiment_runs]  WITH CHECK ADD  CONSTRAINT [FK_experiment_runs_chunking_strategies] FOREIGN KEY([chunking_strategy_id])
REFERENCES [dbo].[chunking_strategies] ([id])
GO
ALTER TABLE [dbo].[experiment_runs] CHECK CONSTRAINT [FK_experiment_runs_chunking_strategies]
GO
ALTER TABLE [dbo].[experiment_runs]  WITH CHECK ADD  CONSTRAINT [FK_experiment_runs_embedding_models] FOREIGN KEY([embedding_model_id])
REFERENCES [dbo].[embedding_models] ([id])
GO
ALTER TABLE [dbo].[experiment_runs] CHECK CONSTRAINT [FK_experiment_runs_embedding_models]
GO
ALTER TABLE [dbo].[experiment_runs]  WITH CHECK ADD  CONSTRAINT [FK_experiment_runs_experiments] FOREIGN KEY([experiment_id])
REFERENCES [dbo].[experiments] ([id])
GO
ALTER TABLE [dbo].[experiment_runs] CHECK CONSTRAINT [FK_experiment_runs_experiments]
GO
ALTER TABLE [dbo].[experiment_runs]  WITH CHECK ADD  CONSTRAINT [FK_experiment_runs_llm_models] FOREIGN KEY([llm_model_id])
REFERENCES [dbo].[llm_models] ([id])
GO
ALTER TABLE [dbo].[experiment_runs] CHECK CONSTRAINT [FK_experiment_runs_llm_models]
GO
ALTER TABLE [dbo].[lecturer_upload_permissions]  WITH CHECK ADD  CONSTRAINT [FK_lup_admin] FOREIGN KEY([granted_by])
REFERENCES [dbo].[users] ([id])
GO
ALTER TABLE [dbo].[lecturer_upload_permissions] CHECK CONSTRAINT [FK_lup_admin]
GO
ALTER TABLE [dbo].[lecturer_upload_permissions]  WITH CHECK ADD  CONSTRAINT [FK_lup_lecturer] FOREIGN KEY([lecturer_id])
REFERENCES [dbo].[users] ([id])
GO
ALTER TABLE [dbo].[lecturer_upload_permissions] CHECK CONSTRAINT [FK_lup_lecturer]
GO
ALTER TABLE [dbo].[lecturer_upload_permissions]  WITH CHECK ADD  CONSTRAINT [FK_lup_subject] FOREIGN KEY([subject_id])
REFERENCES [dbo].[subjects] ([id])
GO
ALTER TABLE [dbo].[lecturer_upload_permissions] CHECK CONSTRAINT [FK_lup_subject]
GO
ALTER TABLE [dbo].[message_citations]  WITH CHECK ADD  CONSTRAINT [FK_message_citations_chunks] FOREIGN KEY([chunk_id])
REFERENCES [dbo].[document_chunks] ([id])
GO
ALTER TABLE [dbo].[message_citations] CHECK CONSTRAINT [FK_message_citations_chunks]
GO
ALTER TABLE [dbo].[message_citations]  WITH CHECK ADD  CONSTRAINT [FK_message_citations_documents] FOREIGN KEY([document_id])
REFERENCES [dbo].[documents] ([id])
GO
ALTER TABLE [dbo].[message_citations] CHECK CONSTRAINT [FK_message_citations_documents]
GO
ALTER TABLE [dbo].[message_citations]  WITH CHECK ADD  CONSTRAINT [FK_message_citations_messages] FOREIGN KEY([message_id])
REFERENCES [dbo].[chat_messages] ([id])
GO
ALTER TABLE [dbo].[message_citations] CHECK CONSTRAINT [FK_message_citations_messages]
GO
ALTER TABLE [dbo].[test_questions]  WITH CHECK ADD  CONSTRAINT [FK_test_questions_subjects] FOREIGN KEY([subject_id])
REFERENCES [dbo].[subjects] ([id])
GO
ALTER TABLE [dbo].[test_questions] CHECK CONSTRAINT [FK_test_questions_subjects]
GO
ALTER TABLE [dbo].[chat_messages]  WITH CHECK ADD CHECK  (([role]='assistant' OR [role]='user'))
GO
ALTER TABLE [dbo].[documents]  WITH CHECK ADD CHECK  (([file_type]='slide' OR [file_type]='docx' OR [file_type]='pdf' OR [file_type]='txt' OR [file_type]='md'))
GO
ALTER TABLE [dbo].[documents]  WITH CHECK ADD CHECK  (([status]='failed' OR [status]='indexed' OR [status]='processing' OR [status]='uploaded'))
GO
ALTER TABLE [dbo].[experiment_runs]  WITH CHECK ADD CHECK  (([status]='error' OR [status]='done' OR [status]='running' OR [status]='queued'))
GO
ALTER TABLE [dbo].[experiments]  WITH CHECK ADD CHECK  (([status]='done' OR [status]='running' OR [status]='draft'))
GO
ALTER TABLE [dbo].[experiments]  WITH CHECK ADD CHECK  (([type]='embedding_bench' OR [type]='chunking_bench' OR [type]='rag_vs_finetune'))
GO
ALTER TABLE [dbo].[llm_models]  WITH CHECK ADD CHECK  (([type]='base' OR [type]='fine_tuned' OR [type]='rag'))
GO
ALTER TABLE [dbo].[test_questions]  WITH CHECK ADD CHECK  (([difficulty]='hard' OR [difficulty]='medium' OR [difficulty]='easy'))
GO
ALTER TABLE [dbo].[users]  WITH CHECK ADD  CONSTRAINT [CK_users_role] CHECK  (([role]='admin' OR [role]='lecturer' OR [role]='student'))
GO
ALTER TABLE [dbo].[users] CHECK CONSTRAINT [CK_users_role]
GO
