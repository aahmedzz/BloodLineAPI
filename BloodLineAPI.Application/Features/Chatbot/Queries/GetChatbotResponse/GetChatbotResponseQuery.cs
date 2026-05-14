using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Chatbot.Queries.GetChatbotResponse;

public class GetChatbotResponseQuery : IRequest<ChatbotResponseDto>
{
    public string Message { get; set; } = string.Empty;
    public Guid? ConversationId { get; set; }
    public Guid UserId { get; set; }
    public Guid DonorId { get; set; }
}

public class ChatbotResponseDto
{
    public Guid ConversationId { get; set; }
    public string Response { get; set; } = string.Empty;
}

public class GetChatbotResponseQueryHandler : IRequestHandler<GetChatbotResponseQuery, ChatbotResponseDto>
{
    private readonly IChatbotService _chatbotService;
    private readonly IApplicationDbContext _context;

    public GetChatbotResponseQueryHandler(IChatbotService chatbotService, IApplicationDbContext context)
    {
        _chatbotService = chatbotService;
        _context = context;
    }

    public async Task<ChatbotResponseDto> Handle(GetChatbotResponseQuery request, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        ChatConversation conversation;

        if (request.ConversationId.HasValue)
        {
            // Continue existing conversation — load it with ownership check
            conversation = await _context.ChatConversations
                .FirstOrDefaultAsync(c => c.Id == request.ConversationId.Value && c.UserId == request.UserId, cancellationToken)
                ?? throw new KeyNotFoundException("Conversation not found.");
        }
        else
        {
            // Create a new conversation with a title from the first message
            var title = request.Message.Length > 100
                ? request.Message[..100]
                : request.Message;

            conversation = new ChatConversation
            {
                Id = Guid.NewGuid(),
                Title = title,
                UserId = request.UserId,
                CreatedAt = utcNow,
                LastMessageAt = utcNow
            };

            _context.ChatConversations.Add(conversation);
        }

        // Load existing messages for the AI context (skip for new conversations)
        // Taking the last 15 messages so we don't exceed model context limits
        var existingMessages = request.ConversationId.HasValue
            ? await _context.ChatMessages
                .AsNoTracking()
                .Where(m => m.ConversationId == conversation.Id)
                .OrderByDescending(m => m.SentAt)
                .Take(15)
                .OrderBy(m => m.SentAt)
                .Select(m => new ChatMessageDto
                {
                    Role = m.Role,
                    Content = m.Content
                })
                .ToListAsync(cancellationToken)
            : new List<ChatMessageDto>();

        // Call the AI service
        var aiResponse = await _chatbotService.GetResponseAsync(request.Message, existingMessages, request.DonorId);

        // Persist both the user message and assistant response
        var userMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = "user",
            Content = request.Message,
            SentAt = utcNow
        };

        var assistantMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = "assistant",
            Content = aiResponse,
            SentAt = utcNow.AddMilliseconds(1) // Ensure ordering after user message
        };

        _context.ChatMessages.Add(userMessage);
        _context.ChatMessages.Add(assistantMessage);

        // Update conversation timestamp
        conversation.LastMessageAt = utcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new ChatbotResponseDto
        {
            ConversationId = conversation.Id,
            Response = aiResponse
        };
    }
}