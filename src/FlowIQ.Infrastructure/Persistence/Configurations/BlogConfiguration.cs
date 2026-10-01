using FlowIQ.Domain.Blog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowIQ.Infrastructure.Persistence.Configurations;

public class BlogPostConfiguration : IEntityTypeConfiguration<BlogPost>
{
    public void Configure(EntityTypeBuilder<BlogPost> builder)
    {
        builder.ToTable("BlogPosts");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Title).IsRequired().HasMaxLength(140);
        builder.Property(p => p.Slug).IsRequired().HasMaxLength(120);
        builder.HasIndex(p => p.Slug).IsUnique();
        builder.Property(p => p.Summary).IsRequired().HasMaxLength(300);
        builder.Property(p => p.Body).IsRequired();
        builder.Property(p => p.Category).IsRequired().HasMaxLength(40);
        builder.Property(p => p.Tags).HasDefaultValueSql("'{}'");
        builder.Property(p => p.AuthorName).IsRequired().HasMaxLength(80);
        builder.Property(p => p.CoverImageAlt).HasMaxLength(200);
        builder.HasIndex(p => p.PublishedAtUtc);
        builder.HasOne<BlogImage>().WithMany().HasForeignKey(p => p.CoverImageId).OnDelete(DeleteBehavior.SetNull);
        builder.Ignore(p => p.IsPublished);
        builder.Ignore(p => p.SignupSource);
        builder.Ignore(p => p.ReadingMinutes);
    }
}

public class BlogImageConfiguration : IEntityTypeConfiguration<BlogImage>
{
    public void Configure(EntityTypeBuilder<BlogImage> builder)
    {
        builder.ToTable("BlogImages");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Data).IsRequired();
        builder.Property(i => i.ContentType).IsRequired().HasMaxLength(40);
        builder.Property(i => i.FileName).IsRequired().HasMaxLength(120);
    }
}
