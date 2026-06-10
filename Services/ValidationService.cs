using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using GoodGovernanceApp.Data;
using GoodGovernanceApp.Models;

namespace GoodGovernanceApp.Services;

/// <summary>
/// Manages the validate_users validation workflow: Accept, Reject, Resubmit.
/// All actions are automatically logged to audit_trails.
/// </summary>
public class ValidationService
{
    private readonly LocalDbContext _db;

    public ValidationService(LocalDbContext db)
    {
        _db = db;
    }

    // ── Queries ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all validate_users records, optionally filtered by status.
    /// </summary>
    /// <param name="filter">One of: "all", "pending", "accepted", "rejected"</param>
    public async Task<List<ValidateUser>> GetAllAsync(string filter = "all")
    {
        var query = _db.ValidateUsers.AsQueryable();

        switch (filter.ToLower())
        {
            case "pending":
                query = query.Where(v => v.Status == "pending");
                break;
            case "accepted":
                query = query.Where(v => v.Status == "accepted");
                break;
            case "rejected":
                query = query.Where(v => v.Status == "rejected");
                break;
        }

        return await query.OrderByDescending(v => v.CreatedAt).ToListAsync();
    }

    /// <summary>
    /// Returns a single validate_users record by ID.
    /// </summary>
    public async Task<ValidateUser?> GetByIdAsync(int id)
    {
        return await _db.ValidateUsers.FirstOrDefaultAsync(v => v.Id == id);
    }

    /// <summary>
    /// Searches validate_users by name or civil registry ID.
    /// </summary>
    public async Task<List<ValidateUser>> SearchAsync(string keyword, string filter = "all")
    {
        var query = _db.ValidateUsers.AsQueryable();

        switch (filter.ToLower())
        {
            case "pending":
                query = query.Where(v => v.Status == "pending");
                break;
            case "accepted":
                query = query.Where(v => v.Status == "accepted");
                break;
            case "rejected":
                query = query.Where(v => v.Status == "rejected");
                break;
        }

        return await query
            .Where(v => (v.CivilRegistryId != null && v.CivilRegistryId.Contains(keyword)) ||
                        (v.Firstname != null && v.Firstname.Contains(keyword)) ||
                        (v.Lastname != null && v.Lastname.Contains(keyword)) ||
                        (v.Middlename != null && v.Middlename.Contains(keyword)))
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync();
    }

    // ── Actions ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Accepts a validate_user record and logs the action in audit_trails.
    /// </summary>
    public async Task<bool> AcceptAsync(int id, long adminUserId)
    {
        var now = DateTime.UtcNow;
        var user = await _db.ValidateUsers.FindAsync(id);
        
        if (user != null)
        {
            user.Status = "accepted";
            user.ValidatedBy = (int)adminUserId;
            user.ValidatedAt = now;
            user.UpdatedAt = now;

            await LogAuditAsync(adminUserId, "accept", "validate_users", id, $"Validation record #{id} was accepted.", now);
            await _db.SaveChangesAsync();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Rejects a validate_user record with a required reason and logs the action.
    /// </summary>
    public async Task<bool> RejectAsync(int id, long adminUserId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason is required.", nameof(reason));

        var now = DateTime.UtcNow;
        var user = await _db.ValidateUsers.FindAsync(id);

        if (user != null)
        {
            user.Status = "rejected";
            user.RejectionReason = reason;
            user.ValidatedBy = (int)adminUserId;
            user.ValidatedAt = now;
            user.UpdatedAt = now;

            await LogAuditAsync(adminUserId, "reject", "validate_users", id, $"Validation record #{id} was rejected. Reason: {reason}", now);
            await _db.SaveChangesAsync();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Resets a rejected record back to pending and clears the rejection reason.
    /// </summary>
    public async Task<bool> ResubmitAsync(int id)
    {
        var now = DateTime.UtcNow;
        var user = await _db.ValidateUsers.FindAsync(id);

        if (user != null)
        {
            user.Status = "pending";
            user.RejectionReason = null;
            user.ValidatedBy = null;
            user.ValidatedAt = null;
            user.UpdatedAt = now;

            await LogAuditAsync(0, "resubmit", "validate_users", id, $"Validation record #{id} was reset to pending (resubmitted).", now);
            await _db.SaveChangesAsync();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Updates basic fields of a rejected record (used in the resubmit form).
    /// </summary>
    public async Task<bool> UpdateRecordAsync(int id, string firstname, string middlename,
        string lastname, string address, string? email, string? phone)
    {
        var now = DateTime.UtcNow;
        var user = await _db.ValidateUsers.FindAsync(id);

        if (user != null)
        {
            user.Firstname = firstname;
            user.Middlename = middlename;
            user.Lastname = lastname;
            user.Address = address;
            user.Email = email;
            user.Phone = phone;
            user.Status = "pending";
            user.RejectionReason = null;
            user.ValidatedBy = null;
            user.ValidatedAt = null;
            user.UpdatedAt = now;

            await _db.SaveChangesAsync();
            return true;
        }
        return false;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private async Task LogAuditAsync(long userId, string action, string modelType,
        int modelId, string description, DateTime timestamp)
    {
        var audit = new AuditTrail
        {
            UserId = (int)userId,
            Action = action,
            ModelType = modelType,
            ModelId = modelId,
            Description = description,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };

        _db.AuditTrails.Add(audit);
    }
}
