using BloodLineAPI.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Chatbot.Queries.GetChatHistory;

public class GetChatHistoryQuery : IRequest<List<ChatConversationDto>>
{
    public Guid UserId { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GetChatHistoryQueryHandler : IRequestHandler<GetChatHistoryQuery, List<ChatConversationDto>>
{
    private readonly IApplicationDbContext _context;

    public GetChatHistoryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ChatConversationDto>> Handle(GetChatHistoryQuery request, CancellationToken cancellationToken)
    {
        var query = _context.ChatConversations
            .AsNoTracking()
            .Where(c => c.UserId == request.UserId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = request.Search.Trim();
            query = query.Where(c => c.Title.Contains(searchTerm));
        }

        var conversations = await query
            .OrderByDescending(c => c.LastMessageAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new ChatConversationDto
            {
                Id = c.Id,
                Title = c.Title,
                LastMessageAt = c.LastMessageAt,
                LastMessagePreview = c.Messages
                    .OrderByDescending(m => m.SentAt)
                    .Select(m => m.Content.Length > 80 ? m.Content.Substring(0, 80) + "…" : m.Content)
                    .FirstOrDefault() ?? string.Empty
            })
            .ToListAsync(cancellationToken);

        return conversations;
    }
}
