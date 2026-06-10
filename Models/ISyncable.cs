using System;

namespace GoodGovernanceApp.Models;

/// <summary>
/// Marker interface for all entities that participate in bidirectional sync.
/// Every entity with a SyncId column must implement this.
/// </summary>
public interface ISyncable
{
    /// <summary>Stable GUID that uniquely identifies this record across all PCs.</summary>
    Guid SyncId { get; set; }

    /// <summary>Timestamp used for last-write-wins conflict resolution.</summary>
    DateTime? UpdatedAt { get; set; }
}
