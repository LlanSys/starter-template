using MediatR;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Shared.Entities;
using ST.Persistence.Common;
using ST.Persistence.Common.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ST.Tests.Integration.NeutralSlice;

internal sealed class NeutralNote : BaseEntity
{
    public required string Title { get; set; }
}

internal sealed class NeutralSliceDBContext(DbContextOptions<NeutralSliceDBContext> options,
    ICurrentTenantProvider tenant, ICurrentActorProvider actor) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder model)
    {
        var entity = model.Entity<NeutralNote>();
        entity.ToTable("CertificationNotes");
        entity.HasKey(note => note.Id);
        entity.Property(note => note.Title).HasMaxLength(100).IsRequired();
        entity.Property(note => note.RowVersion).IsRowVersion();
        entity.HasQueryFilter(note => note.TenantId == tenant.TenantId);
    }
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<NeutralNote>().Where(entry => entry.State == EntityState.Added))
        {
            entry.Entity.TenantId = tenant.TenantId;
            entry.Entity.CreatedBy = actor.ActorId;
            entry.Entity.CreatedAt = DateTimeOffset.UtcNow;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}

internal sealed class NeutralSliceUnitOfWork(NeutralSliceDBContext context, IPublisher publisher,
    ILogger<NeutralSliceUnitOfWork> logger) : BaseUnitOfWork<NeutralSliceDBContext>(context, publisher, logger)
{
    internal Repository<NeutralNote> Notes { get; } = new(context);
}

internal sealed record CreateNeutralNote(string Title) : IRequest<Guid>;
internal sealed record ReadNeutralNote(Guid Id) : IRequest<string?>;

internal sealed class CreateNeutralNoteHandler(NeutralSliceUnitOfWork unitOfWork)
    : IRequestHandler<CreateNeutralNote, Guid>
{
    public async Task<Guid> Handle(CreateNeutralNote request, CancellationToken cancellationToken)
    {
        var note = new NeutralNote { Title = request.Title, CreatedBy = ICurrentActorProvider.SystemActor };
        await unitOfWork.Notes.CreateAsync(note, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return note.Id;
    }
}
internal sealed class ReadNeutralNoteHandler(NeutralSliceUnitOfWork unitOfWork) : IRequestHandler<ReadNeutralNote, string?>
{
    public async Task<string?> Handle(ReadNeutralNote request, CancellationToken cancellationToken)
        => (await unitOfWork.Notes.FindByIdAsync(request.Id, cancellationToken))?.Title;
}
