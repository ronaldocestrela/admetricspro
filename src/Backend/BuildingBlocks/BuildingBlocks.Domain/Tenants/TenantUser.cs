using System.Text.RegularExpressions;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Domain.Tenants;

/// <summary>
/// Domain entity representing a user inside an operational tenant database instance.
/// </summary>
public sealed partial class TenantUser : Entity<Guid>
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private TenantUser(
        Guid id,
        string fullName,
        string email,
        string? phoneNumber,
        string passwordHash,
        TenantRole role,
        bool isActive,
        DateTime createdAtUtc)
        : base(id)
    {
        FullName = fullName;
        Email = email;
        PhoneNumber = phoneNumber;
        PasswordHash = passwordHash;
        Role = role;
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc;
    }

    private TenantUser()
        : base(Guid.Empty)
    {
        FullName = string.Empty;
        Email = string.Empty;
        PasswordHash = string.Empty;
        Role = TenantRole.Guest;
        IsActive = false;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the full legal or display name of the user.
    /// </summary>
    public string FullName { get; private set; }

    /// <summary>
    /// Gets the email address used for tenant authentication and communications.
    /// </summary>
    public string Email { get; private set; }

    /// <summary>
    /// Gets the optional phone number of the user.
    /// </summary>
    public string? PhoneNumber { get; private set; }

    /// <summary>
    /// Gets the cryptographic hash of the user's password.
    /// </summary>
    public string PasswordHash { get; private set; }

    /// <summary>
    /// Gets the assigned hierarchical role of the user within the tenant.
    /// </summary>
    public TenantRole Role { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the user account is active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the user account was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp of the most recent profile or credential update.
    /// </summary>
    public DateTime? UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Creates a new <see cref="TenantUser"/> instance validating all business invariants.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <param name="fullName">The full name of the user.</param>
    /// <param name="email">The email address of the user.</param>
    /// <param name="phoneNumber">Optional phone number.</param>
    /// <param name="passwordHash">The secure password hash.</param>
    /// <param name="role">The assigned role.</param>
    /// <param name="createdAtUtc">Optional creation timestamp; defaults to UTC now.</param>
    /// <returns>A <see cref="Result{T}"/> containing the new user on success or a validation failure.</returns>
    public static Result<TenantUser> Create(
        Guid id,
        string fullName,
        string email,
        string? phoneNumber,
        string passwordHash,
        TenantRole role,
        DateTime? createdAtUtc = null)
    {
        if (id == Guid.Empty)
        {
            return Result<TenantUser>.Failure(Error.Validation("TenantUser.InvalidId", "User identifier cannot be empty."));
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return Result<TenantUser>.Failure(Error.Validation("TenantUser.InvalidFullName", "User full name is required."));
        }

        var trimmedName = fullName.Trim();
        if (trimmedName.Length > 200)
        {
            return Result<TenantUser>.Failure(Error.Validation("TenantUser.FullNameTooLong", "User full name cannot exceed 200 characters."));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return Result<TenantUser>.Failure(Error.Validation("TenantUser.InvalidEmail", "User email address is required."));
        }

        var trimmedEmail = email.Trim();
        if (trimmedEmail.Length > 256)
        {
            return Result<TenantUser>.Failure(Error.Validation("TenantUser.EmailTooLong", "User email cannot exceed 256 characters."));
        }

        if (!EmailRegex.IsMatch(trimmedEmail))
        {
            return Result<TenantUser>.Failure(Error.Validation("TenantUser.InvalidEmail", "User email address format is invalid."));
        }

        var trimmedPhone = phoneNumber?.Trim();
        if (trimmedPhone is not null && trimmedPhone.Length > 50)
        {
            return Result<TenantUser>.Failure(Error.Validation("TenantUser.PhoneNumberTooLong", "User phone number cannot exceed 50 characters."));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return Result<TenantUser>.Failure(Error.Validation("TenantUser.InvalidPasswordHash", "User password hash is required."));
        }

        if (passwordHash.Length > 500)
        {
            return Result<TenantUser>.Failure(Error.Validation("TenantUser.PasswordHashTooLong", "Password hash cannot exceed 500 characters."));
        }

        if (!Enum.IsDefined(typeof(TenantRole), role))
        {
            return Result<TenantUser>.Failure(Error.Validation("TenantUser.InvalidRole", "Provided tenant role is invalid."));
        }

        var user = new TenantUser(
            id,
            trimmedName,
            trimmedEmail,
            string.IsNullOrWhiteSpace(trimmedPhone) ? null : trimmedPhone,
            passwordHash,
            role,
            isActive: true,
            createdAtUtc ?? DateTime.UtcNow);

        return Result<TenantUser>.Success(user);
    }

    /// <summary>
    /// Updates the user profile details.
    /// </summary>
    /// <param name="fullName">New full name.</param>
    /// <param name="phoneNumber">New optional phone number.</param>
    /// <returns>A <see cref="Result"/> indicating success or a validation failure.</returns>
    public Result UpdateProfile(string fullName, string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return Result.Failure(Error.Validation("TenantUser.InvalidFullName", "User full name is required."));
        }

        var trimmedName = fullName.Trim();
        if (trimmedName.Length > 200)
        {
            return Result.Failure(Error.Validation("TenantUser.FullNameTooLong", "User full name cannot exceed 200 characters."));
        }

        var trimmedPhone = phoneNumber?.Trim();
        if (trimmedPhone is not null && trimmedPhone.Length > 50)
        {
            return Result.Failure(Error.Validation("TenantUser.PhoneNumberTooLong", "User phone number cannot exceed 50 characters."));
        }

        FullName = trimmedName;
        PhoneNumber = string.IsNullOrWhiteSpace(trimmedPhone) ? null : trimmedPhone;
        UpdatedAtUtc = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Updates the user's secure password hash.
    /// </summary>
    /// <param name="newPasswordHash">The new secure password hash.</param>
    /// <returns>A <see cref="Result"/> indicating success or validation failure.</returns>
    public Result ChangePasswordHash(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
        {
            return Result.Failure(Error.Validation("TenantUser.InvalidPasswordHash", "Password hash cannot be empty."));
        }

        if (newPasswordHash.Length > 500)
        {
            return Result.Failure(Error.Validation("TenantUser.PasswordHashTooLong", "Password hash cannot exceed 500 characters."));
        }

        PasswordHash = newPasswordHash;
        UpdatedAtUtc = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Updates the assigned role of the user.
    /// </summary>
    /// <param name="newRole">The new tenant role to assign.</param>
    /// <returns>A <see cref="Result"/> indicating success or validation failure.</returns>
    public Result ChangeRole(TenantRole newRole)
    {
        if (!Enum.IsDefined(typeof(TenantRole), newRole))
        {
            return Result.Failure(Error.Validation("TenantUser.InvalidRole", "Provided tenant role is invalid."));
        }

        Role = newRole;
        UpdatedAtUtc = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Deactivates the user account.
    /// </summary>
    /// <returns>A <see cref="Result"/> indicating success.</returns>
    public Result Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Activates the user account.
    /// </summary>
    /// <returns>A <see cref="Result"/> indicating success.</returns>
    public Result Activate()
    {
        IsActive = true;
        UpdatedAtUtc = DateTime.UtcNow;

        return Result.Success();
    }
}
