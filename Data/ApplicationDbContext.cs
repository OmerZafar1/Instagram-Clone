using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MiniInstagram.Models;

namespace MiniInstagram.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Like> Likes => Set<Like>();
    public DbSet<SavedPost> SavedPosts => Set<SavedPost>();
    public DbSet<PostVisibleUser> PostVisibleUsers => Set<PostVisibleUser>();
    public DbSet<UserFollow> UserFollows => Set<UserFollow>();
    public DbSet<FollowRequest> FollowRequests => Set<FollowRequest>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Story> Stories => Set<Story>();
    public DbSet<StoryView> StoryViews => Set<StoryView>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Post>(entity =>
        {
            entity.HasOne(p => p.User)
                .WithMany(u => u.Posts)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(p => p.Caption).HasMaxLength(2200);
            entity.Property(p => p.ImagePath).HasMaxLength(500);
            entity.Property(p => p.MediaType).HasMaxLength(50).HasDefaultValue("image");
            entity.Property(p => p.Visibility).HasConversion<int>();
            entity.HasIndex(p => p.CreatedAt);
        });

        builder.Entity<Comment>(entity =>
        {
            entity.HasOne(c => c.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(c => c.Content).HasMaxLength(1000);
            entity.HasIndex(c => c.CreatedAt);
        });

        builder.Entity<Like>(entity =>
        {
            entity.HasOne(l => l.Post)
                .WithMany(p => p.Likes)
                .HasForeignKey(l => l.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(l => l.User)
                .WithMany(u => u.Likes)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(l => new { l.PostId, l.UserId }).IsUnique();
        });

        builder.Entity<SavedPost>(entity =>
        {
            entity.HasOne(s => s.Post)
                .WithMany(p => p.SavedByUsers)
                .HasForeignKey(s => s.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.User)
                .WithMany(u => u.SavedPosts)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(s => new { s.PostId, s.UserId }).IsUnique();
            entity.HasIndex(s => new { s.UserId, s.CreatedAt });
        });

        builder.Entity<PostVisibleUser>(entity =>
        {
            entity.HasOne(v => v.Post)
                .WithMany(p => p.VisibleUsers)
                .HasForeignKey(v => v.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(v => v.User)
                .WithMany(u => u.VisiblePosts)
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(v => new { v.PostId, v.UserId }).IsUnique();
            entity.HasIndex(v => v.UserId);
        });

        builder.Entity<UserFollow>(entity =>
        {
            entity.HasOne(f => f.Follower)
                .WithMany(u => u.Following)
                .HasForeignKey(f => f.FollowerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(f => f.Following)
                .WithMany(u => u.Followers)
                .HasForeignKey(f => f.FollowingId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(f => new { f.FollowerId, f.FollowingId }).IsUnique();
        });

        builder.Entity<FollowRequest>(entity =>
        {
            entity.HasOne(r => r.Requester)
                .WithMany(u => u.FollowRequestsSent)
                .HasForeignKey(r => r.RequesterId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.TargetUser)
                .WithMany(u => u.FollowRequestsReceived)
                .HasForeignKey(r => r.TargetUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(r => new { r.RequesterId, r.TargetUserId }).IsUnique();
            entity.HasIndex(r => new { r.TargetUserId, r.CreatedAt });
        });

        builder.Entity<Story>(entity =>
        {
            entity.HasOne(s => s.User)
                .WithMany(u => u.Stories)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(s => s.MediaPath).HasMaxLength(500);
            entity.Property(s => s.MediaType).HasMaxLength(50);
            entity.Property(s => s.Caption).HasMaxLength(500);
            entity.HasIndex(s => new { s.UserId, s.ExpiresAt });
            entity.HasIndex(s => s.CreatedAt);
        });

        builder.Entity<StoryView>(entity =>
        {
            entity.HasOne(v => v.Story)
                .WithMany(s => s.Views)
                .HasForeignKey(v => v.StoryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(v => v.Viewer)
                .WithMany(u => u.StoryViews)
                .HasForeignKey(v => v.ViewerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(v => new { v.StoryId, v.ViewerId }).IsUnique();
            entity.HasIndex(v => v.ViewedAt);
        });

        builder.Entity<Conversation>(entity =>
        {
            entity.HasOne(c => c.User1)
                .WithMany()
                .HasForeignKey(c => c.User1Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.User2)
                .WithMany()
                .HasForeignKey(c => c.User2Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(c => new { c.User1Id, c.User2Id }).IsUnique();
            entity.HasIndex(c => c.UpdatedAt);
        });

        builder.Entity<ChatMessage>(entity =>
        {
            entity.HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(m => m.Content).HasMaxLength(4000);
            entity.Property(m => m.MediaPath).HasMaxLength(500);
            entity.HasIndex(m => m.SentAt);
        });

        builder.Entity<Notification>(entity =>
        {
            entity.HasOne(n => n.Recipient)
                .WithMany()
                .HasForeignKey(n => n.RecipientId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(n => n.Actor)
                .WithMany()
                .HasForeignKey(n => n.ActorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(n => n.Message).HasMaxLength(500);
            entity.HasIndex(n => new { n.RecipientId, n.IsRead });
            entity.HasIndex(n => n.CreatedAt);
        });

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.DisplayName).HasMaxLength(100);
            entity.Property(u => u.Bio).HasMaxLength(500);
            entity.Property(u => u.AvatarPath).HasMaxLength(500);
            entity.HasIndex(u => u.UserName);
        });
    }
}
