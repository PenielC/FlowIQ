using FlowIQ.Application.Authentication;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.Authentication;
using FlowIQ.Domain.Exceptions;
using FlowIQ.Domain.ProductUpdates;
using FluentValidation;
using Mediator;

namespace FlowIQ.Application.ProductUpdates;

public interface IProductUpdateRepository : IRepository<ProductUpdate>
{
    Task<List<ProductUpdate>> ListPublishedAsync(int take, CancellationToken cancellationToken = default);

    /// <summary>Every entry, drafts included, newest first (for the admin page).</summary>
    Task<List<ProductUpdate>> ListAllAsync(CancellationToken cancellationToken = default);

    Task<HashSet<Guid>> DismissedIdsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddDismissalAsync(ProductUpdateDismissal dismissal, CancellationToken cancellationToken = default);
}

public record ProductUpdateResult(
    Guid Id,
    string Title,
    string Summary,
    string? LinkUrl,
    string? LinkLabel,
    ProductUpdateAudience Audience,
    bool ShowOnDashboard,
    DateTime? PublishedAtUtc,
    bool IsUnread)
{
    public static ProductUpdateResult From(ProductUpdate u, bool isUnread = false) =>
        new(u.Id, u.Title, u.Summary, u.LinkUrl, u.LinkLabel, u.Audience, u.ShowOnDashboard, u.PublishedAtUtc, isUnread);
}

/// <param name="Updates">The feed, newest first.</param>
/// <param name="DashboardCards">Cards still to show on the dashboard: recent, not dismissed, at most two.</param>
public record WhatsNewResult(IReadOnlyList<ProductUpdateResult> Updates, int UnreadCount, IReadOnlyList<ProductUpdateResult> DashboardCards);

public record GetWhatsNewQuery(Guid UserId) : IQuery<WhatsNewResult>;

public record MarkWhatsNewSeenCommand(Guid UserId) : ICommand;

public record DismissProductUpdateCommand(Guid UserId, Guid ProductUpdateId) : ICommand;

public record ListProductUpdatesQuery : IQuery<IReadOnlyList<ProductUpdateResult>>;

public record SaveProductUpdateCommand(
    Guid? Id,
    string Title,
    string Summary,
    string? LinkUrl,
    string? LinkLabel,
    ProductUpdateAudience Audience,
    bool ShowOnDashboard) : ICommand<ProductUpdateResult>;

public record SetProductUpdatePublishedCommand(Guid Id, bool Published) : ICommand<ProductUpdateResult>;

public record DeleteProductUpdateCommand(Guid Id) : ICommand;

public class GetWhatsNewQueryHandler(IProductUpdateRepository updates, IUserRepository users, IDateTimeProvider clock)
    : IQueryHandler<GetWhatsNewQuery, WhatsNewResult>
{
    public const int FeedSize = 20;
    public const int MaxCards = 2;
    public static readonly TimeSpan CardLifetime = TimeSpan.FromDays(90);

    public async ValueTask<WhatsNewResult> Handle(GetWhatsNewQuery query, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(query.UserId, cancellationToken) ?? throw new DomainException("User not found.");
        var ownerOrAdmin = user.Role is UserRole.Owner or UserRole.Admin;
        var visible = (await updates.ListPublishedAsync(FeedSize, cancellationToken)).Where(u => u.IsFor(ownerOrAdmin)).ToList();
        var dismissed = await updates.DismissedIdsAsync(query.UserId, cancellationToken);

        bool Unread(ProductUpdate u) => user.WhatsNewSeenAtUtc is null || u.PublishedAtUtc > user.WhatsNewSeenAtUtc;
        var cardsSince = clock.UtcNow - CardLifetime;

        return new WhatsNewResult(
            visible.Select(u => ProductUpdateResult.From(u, Unread(u))).ToList(),
            visible.Count(Unread),
            visible.Where(u => u.ShowOnDashboard && u.PublishedAtUtc >= cardsSince && !dismissed.Contains(u.Id))
                .Take(MaxCards)
                .Select(u => ProductUpdateResult.From(u, Unread(u)))
                .ToList());
    }
}

public class MarkWhatsNewSeenCommandHandler(IUserRepository users, IDateTimeProvider clock, IUnitOfWork unitOfWork)
    : ICommandHandler<MarkWhatsNewSeenCommand>
{
    public async ValueTask<Unit> Handle(MarkWhatsNewSeenCommand command, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(command.UserId, cancellationToken) ?? throw new DomainException("User not found.");
        user.MarkWhatsNewSeen(clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

public class DismissProductUpdateCommandHandler(IProductUpdateRepository updates, IDateTimeProvider clock, IUnitOfWork unitOfWork)
    : ICommandHandler<DismissProductUpdateCommand>
{
    public async ValueTask<Unit> Handle(DismissProductUpdateCommand command, CancellationToken cancellationToken)
    {
        var dismissed = await updates.DismissedIdsAsync(command.UserId, cancellationToken);
        if (dismissed.Contains(command.ProductUpdateId)) return Unit.Value;
        _ = await updates.GetByIdAsync(command.ProductUpdateId, cancellationToken) ?? throw new DomainException("Update not found.");

        await updates.AddDismissalAsync(new ProductUpdateDismissal(command.UserId, command.ProductUpdateId, clock.UtcNow), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

public class ListProductUpdatesQueryHandler(IProductUpdateRepository updates) : IQueryHandler<ListProductUpdatesQuery, IReadOnlyList<ProductUpdateResult>>
{
    public async ValueTask<IReadOnlyList<ProductUpdateResult>> Handle(ListProductUpdatesQuery query, CancellationToken cancellationToken) =>
        (await updates.ListAllAsync(cancellationToken)).Select(u => ProductUpdateResult.From(u)).ToList();
}

public class SaveProductUpdateCommandHandler(IProductUpdateRepository updates, IUnitOfWork unitOfWork)
    : ICommandHandler<SaveProductUpdateCommand, ProductUpdateResult>
{
    public async ValueTask<ProductUpdateResult> Handle(SaveProductUpdateCommand command, CancellationToken cancellationToken)
    {
        ProductUpdate update;
        if (command.Id is { } id)
        {
            update = await updates.GetByIdAsync(id, cancellationToken) ?? throw new DomainException("Update not found.");
            update.Edit(command.Title, command.Summary, command.LinkUrl, command.LinkLabel, command.Audience, command.ShowOnDashboard);
        }
        else
        {
            // New entries start as drafts: nobody sees them until they're published.
            update = new ProductUpdate(command.Title, command.Summary, command.LinkUrl, command.LinkLabel, command.Audience, command.ShowOnDashboard);
            await updates.AddAsync(update, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ProductUpdateResult.From(update);
    }
}

public class SaveProductUpdateCommandValidator : AbstractValidator<SaveProductUpdateCommand>
{
    public SaveProductUpdateCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Summary).NotEmpty().MaximumLength(400);
        RuleFor(x => x.LinkUrl).MaximumLength(300);
        RuleFor(x => x.LinkLabel).MaximumLength(40);
        RuleFor(x => x.Audience).IsInEnum();
    }
}

public class SetProductUpdatePublishedCommandHandler(IProductUpdateRepository updates, IDateTimeProvider clock, IUnitOfWork unitOfWork)
    : ICommandHandler<SetProductUpdatePublishedCommand, ProductUpdateResult>
{
    public async ValueTask<ProductUpdateResult> Handle(SetProductUpdatePublishedCommand command, CancellationToken cancellationToken)
    {
        var update = await updates.GetByIdAsync(command.Id, cancellationToken) ?? throw new DomainException("Update not found.");
        if (command.Published) update.Publish(clock.UtcNow);
        else update.Unpublish();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ProductUpdateResult.From(update);
    }
}

public class DeleteProductUpdateCommandHandler(IProductUpdateRepository updates, IUnitOfWork unitOfWork) : ICommandHandler<DeleteProductUpdateCommand>
{
    public async ValueTask<Unit> Handle(DeleteProductUpdateCommand command, CancellationToken cancellationToken)
    {
        var update = await updates.GetByIdAsync(command.Id, cancellationToken) ?? throw new DomainException("Update not found.");
        updates.Remove(update);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
