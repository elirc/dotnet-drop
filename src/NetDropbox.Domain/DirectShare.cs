using NetDropbox.Domain.Common;

namespace NetDropbox.Domain;

public sealed class DirectShare : Entity
{
    private DirectShare() { }

    private DirectShare(Guid fileNodeId, Guid sharedByUserId, Guid sharedWithUserId, DirectShareRole role)
    {
        FileNodeId = fileNodeId;
        SharedByUserId = sharedByUserId;
        SharedWithUserId = sharedWithUserId;
        Role = role;
    }

    public Guid FileNodeId { get; private set; }
    public Guid SharedByUserId { get; private set; }
    public Guid SharedWithUserId { get; private set; }
    public DirectShareRole Role { get; private set; }

    public static DirectShare Create(Guid fileNodeId, Guid sharedByUserId, Guid sharedWithUserId, DirectShareRole role) =>
        new(fileNodeId, sharedByUserId, sharedWithUserId, role);
}

