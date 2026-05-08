using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Chatbot.Commands.DeleteConversation;
using BloodLineAPI.Application.Features.Chatbot.Queries.GetChatbotResponse;
using BloodLineAPI.Application.Features.Chatbot.Queries.GetChatHistory;
using BloodLineAPI.Application.Features.Chatbot.Queries.GetConversationMessages;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using BloodLineAPI.Attributes;
using System.Security.Claims;

namespace BloodLineAPI.Controllers.V1.Mobile;

[ApiController]
[Route("api/v{version:apiVersion}/mobile/[controller]")]
[ApiAudience("Mobile")]
[ApiVersion("1.0")]
[Authorize(Roles = "Donor")]
public class ChatbotController : ControllerBase
{
    private readonly IMediator _mediator;

    public ChatbotController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Sends a message to the chatbot. Pass ConversationId to continue an existing conversation, or omit it to start a new one.
    /// </summary>
    [HttpPost("ask")]
    [ProducesResponseType(typeof(ApiResponse<ChatbotResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Ask([FromBody] ChatbotRequestDto request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));
        }

        // Treat Guid.Empty the same as null (start new conversation)
        var conversationId = request.ConversationId == Guid.Empty ? null : request.ConversationId;

        var response = await _mediator.Send(new GetChatbotResponseQuery
        {
            Message = request.Message,
            ConversationId = conversationId,
            UserId = userId,
            DonorId = userId
        });

        return Ok(ApiResponse<ChatbotResponseDto>.Ok(response));
    }

    /// <summary>
    /// Gets the user's chat conversation history with optional title search.
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(ApiResponse<List<ChatConversationDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetHistory([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));
        }

        var result = await _mediator.Send(new GetChatHistoryQuery
        {
            UserId = userId,
            Search = search,
            Page = page,
            PageSize = pageSize
        });

        return Ok(ApiResponse<List<ChatConversationDto>>.Ok(result));
    }

    /// <summary>
    /// Gets all messages for a specific conversation.
    /// </summary>
    [HttpGet("{conversationId:guid}/messages")]
    [ProducesResponseType(typeof(ApiResponse<List<ConversationMessageDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessages(Guid conversationId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));
        }

        var result = await _mediator.Send(new GetConversationMessagesQuery
        {
            ConversationId = conversationId,
            UserId = userId
        });

        if (result == null)
        {
            return NotFound(ApiResponse<object>.Fail("Conversation not found."));
        }

        return Ok(ApiResponse<List<ConversationMessageDto>>.Ok(result));
    }

    /// <summary>
    /// Deletes a specific conversation and all its messages.
    /// </summary>
    [HttpDelete("{conversationId:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteConversation(Guid conversationId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));
        }

        var deleted = await _mediator.Send(new DeleteConversationCommand
        {
            ConversationId = conversationId,
            UserId = userId
        });

        if (!deleted)
        {
            return NotFound(ApiResponse<object>.Fail("Conversation not found."));
        }

        return Ok(ApiResponse.Ok(message: "Conversation deleted successfully."));
    }
}