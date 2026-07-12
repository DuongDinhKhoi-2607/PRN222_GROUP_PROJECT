using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BussinessLayer.Interfaces;
using BussinessLayer.DTOs;

namespace PresentationLayer.Pages.Chat
{
    [Authorize(Roles = "student,lecturer")]
    public class SessionModel : PageModel
    {
        private readonly IChatService _chat;
        private readonly IRetrievalService _retrieval;
        private readonly ILLMService _llm;
        private readonly IMessageCitationService _citationSvc;
        private readonly ISubjectService _subjectService;

        public SessionModel(
            IChatService chat,
            IRetrievalService retrieval,
            ILLMService llm,
            IMessageCitationService citationSvc,
            ISubjectService subjectService)
        {
            _chat = chat;
            _retrieval = retrieval;
            _llm = llm;
            _citationSvc = citationSvc;
            _subjectService = subjectService;
        }

        public ChatSessionDto CurrentSession { get; set; } = null!;
        public IEnumerable<ChatMessageDto> Messages { get; set; } = new List<ChatMessageDto>();
        public IEnumerable<ChatSessionDto> History { get; set; } = new List<ChatSessionDto>();
        public IEnumerable<SubjectDto> Subjects { get; set; } = new List<SubjectDto>();
        public long CurrentUserId { get; set; }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && long.TryParse(userIdClaim.Value, out var uid))
                CurrentUserId = uid;

            var session = await _chat.GetSessionAsync(id);
            if (session == null) return NotFound();
            CurrentSession = session;

            Messages = await _chat.GetMessagesAsync(id);
            Subjects = await _subjectService.GetAllAsync();

            if (CurrentUserId > 0)
            {
                History = await _chat.GetSessionsAsync(CurrentUserId);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostSendAsync(long sessionId, string question)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();

            if (sessionId <= 0)
            {
                var title = question.Length > 30 ? question.Substring(0, 30) + "..." : question;
                var newSession = await _chat.CreateSessionAsync(userId, null, title);
                sessionId = newSession.Id;
            }
            else
            {
                var session = await _chat.GetSessionAsync(sessionId);
                if (session != null && string.IsNullOrWhiteSpace(session.Title))
                {
                    var newTitle = question.Length > 30 ? question.Substring(0, 30) + "..." : question;
                    await _chat.UpdateSessionTitleAsync(sessionId, newTitle);
                }
            }

            await _chat.AddMessageAsync(sessionId, "user", question);

            var contexts = await _retrieval.RetrieveAsync(question, null);
            var (answer, citations) = await _llm.GenerateAnswerAsync(question, contexts, null);

            var assistantMsg = await _chat.AddMessageAsync(sessionId, "assistant", answer);

            foreach (var c in citations)
            {
                await _citationSvc.AddAsync(new MessageCitationDto
                {
                    MessageId = assistantMsg.Id,
                    ChunkId = c.chunk.Id,
                    DocumentId = c.doc.Id,
                    RelevanceScore = c.score,
                    Snippet = c.chunk.Content
                });
            }

            return RedirectToPage("Session", new { id = sessionId });
        }
    }
}