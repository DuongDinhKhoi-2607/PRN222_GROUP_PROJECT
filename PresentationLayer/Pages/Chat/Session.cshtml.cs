using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
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
        private readonly IUserService _userService;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<PresentationLayer.Hubs.DashboardHub> _dashboardHub;
        private readonly IDashboardService _dashboardService;

        public SessionModel(
            IChatService chat,
            IRetrievalService retrieval,
            ILLMService llm,
            IMessageCitationService citationSvc,
            ISubjectService subjectService,
            IUserService userService,
            Microsoft.AspNetCore.SignalR.IHubContext<PresentationLayer.Hubs.DashboardHub> dashboardHub,
            IDashboardService dashboardService)
        {
            _chat = chat;
            _retrieval = retrieval;
            _llm = llm;
            _citationSvc = citationSvc;
            _subjectService = subjectService;
            _userService = userService;
            _dashboardHub = dashboardHub;
            _dashboardService = dashboardService;
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

        public async Task<IActionResult> OnPostSendAsync(long sessionId, string question, long? subjectId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();

            if (sessionId <= 0)
            {
                var title = question.Length > 30 ? question.Substring(0, 30) + "..." : question;
                var newSession = await _chat.CreateSessionAsync(userId, subjectId, title);
                sessionId = newSession.Id;
            }
            else
            {
                var session = await _chat.GetSessionAsync(sessionId);
                if (session != null)
                {
                    if (session.SubjectId != subjectId)
                    {
                        await _chat.UpdateSessionSubjectAsync(sessionId, subjectId);
                    }

                    if (string.IsNullOrWhiteSpace(session.Title))
                    {
                        var newTitle = question.Length > 30 ? question.Substring(0, 30) + "..." : question;
                        await _chat.UpdateSessionTitleAsync(sessionId, newTitle);
                    }
                }
            }

            await _chat.AddMessageAsync(sessionId, "user", question);

            // Token Limit Check
            bool canProceed = await _userService.DeductTokenAsync(userId, 4);
            if (!canProceed)
            {
                var upgradeMsg = "❌ Bạn đã hết Token miễn phí hoặc không đủ 4 Token cho câu hỏi này. " +
                                 "Mỗi câu hỏi tốn 4 Token và bạn sẽ được hồi 1 Token mỗi 20 phút. " +
                                 "Vui lòng đợi hoặc [Nâng cấp gói Pro](/Upgrade) để sử dụng không giới hạn!";
                await _chat.AddMessageAsync(sessionId, "assistant", upgradeMsg);
                return RedirectToPage("Session", new { id = sessionId });
            }

            // Real-time broadcast for token usage
            await _dashboardHub.Clients.Group("AdminDashboard").SendAsync("DashboardUpdated", "TokenUsage", new { userId });
            var summary = await _dashboardService.GetSummaryAsync();
            await _dashboardHub.Clients.Group("AdminDashboard").SendAsync("SummaryUpdated", summary);

            var contexts = await _retrieval.RetrieveAsync(question, subjectId);
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