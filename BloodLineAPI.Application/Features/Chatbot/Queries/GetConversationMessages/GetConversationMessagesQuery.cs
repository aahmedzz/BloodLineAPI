using BloodLineAPI.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Chatbot.Queries.GetConversationMessages;

public class GetConversationMessagesQuery : IRequest<List<ConversationMessageDto>?>
{
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
}

public class GetConversationMessagesQueryHandler : IRequestHandler<GetConversationMessagesQuery, List<ConversationMessageDto>?>
{
    private readonly IApplicationDbContext _context;

    public GetConversationMessagesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ConversationMessageDto>?> Handle(GetConversationMessagesQuery request, CancellationToken cancellationToken)
    {
        // Verify ownership
        var conversationExists = await _context.ChatConversations
            .AsNoTracking()
            .AnyAsync(c => c.Id == request.ConversationId && c.UserId == request.UserId, cancellationToken);

        if (!conversationExists)
        {
            return null;
        }

        var messages = await _context.ChatMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == request.ConversationId)
            .OrderBy(m => m.SentAt)
            .Select(m => new ConversationMessageDto
            {
                Role = m.Role,
                Content = m.Content,
                SentAt = m.SentAt
            })
            .ToListAsync(cancellationToken);

        return messages;
    }
}
