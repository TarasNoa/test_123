using Libr4.Chat.Domain.CodeSnippets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Libr4.Chat.Infrastructure.Persistence.Configurations;

public class CodeSnippetConfiguration : IEntityTypeConfiguration<CodeSnippet>
{
    public void Configure(EntityTypeBuilder<CodeSnippet> builder)
    {
        builder.ToTable("code_snippets", "chat");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ChannelId).IsRequired();
        builder.Property(x => x.CreatorId).IsRequired();
        builder.Property(x => x.Language).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Code).IsRequired();
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.ChannelId);
        builder.HasIndex(x => x.CreatorId);
    }
}
