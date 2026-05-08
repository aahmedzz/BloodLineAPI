using BloodLineAPI.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Chatbot.Commands.DeleteConversation;

public class DeleteConversationCommand : IRequest<bool>
{
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
}

public class DeleteConversationCommandHandler : IRequestHandler<DeleteConversationCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeleteConversationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteConversationCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _context.ChatConversations
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId && c.UserId == request.UserId, cancellationToken);

        if (conversation == null)
        {
            return false;
        }

        // Messages are cascade-deleted by EF Core configuration
        _context.ChatConversations.Remove(conversation);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
