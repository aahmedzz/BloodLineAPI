using BloodLineAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BloodLineAPI.Infrastructure.Persistence.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Role)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(m => m.Content)
            .IsRequired();

        builder.Property(m => m.SentAt)
            .IsRequired();

        // Index: load messages for a conversation in chronological order
        builder.HasIndex(m => new { m.ConversationId, m.SentAt })
            .HasDatabaseName("IX_ChatMessages_ConversationId_SentAt");
    }
}
