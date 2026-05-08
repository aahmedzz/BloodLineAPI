using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BloodLineAPI.Application.Features.Chatbot.Queries.GetChatbotResponse;

public class ChatMessageDto
{

    [Required]
    [RegularExpression("^(user|assistant)$", ErrorMessage = "Role must be exactly 'user' or 'assistant'.")]
    public string Role { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;
}
