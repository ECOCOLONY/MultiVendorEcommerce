using Application.Common.Models;

namespace Application.Common.Interfaces;

public sealed record UserSummary(
    string Id,
    string Email,
    string FullName,
    DateTime CreatedAtUtc,
    IReadOnlyCollection<string> Roles
);

public interface IIdentityService
{
    Task<string?> GetUserNameAsync(string userId);
    Task<bool> IsInRoleAsync(string userId, string role);
    Task<Result<string>> CreateUserAsync(string email, string password, string fullName, string role);
    Task<Result> DeleteUserAsync(string userId);
    Task<Result> SignInAsync(string email, string password, bool rememberMe);
    Task SignOutAsync();

    Task<List<UserSummary>> GetUsersAsync(string? searchTerm = null, CancellationToken ct = default);
    Task<PaginatedList<UserSummary>> GetUsersPagedAsync(string? search, string? role, int page, int pageSize, CancellationToken ct = default);
    Task<UserSummary?> GetUserAsync(string userId);
    Task<Result> SetUserRolesAsync(string userId, IEnumerable<string> roles);

    Task<Result> UpdateProfileAsync(string userId, string fullName, string? profileImageUrl);
    Task<Result> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
}
