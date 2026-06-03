using Application.Common.Interfaces;
using Application.Common.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public IdentityService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<List<UserSummary>> GetUsersAsync(string? searchTerm = null, CancellationToken ct = default)
    {
        var q = _userManager.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var s = searchTerm.Trim();
            q = q.Where(u => (u.Email != null && u.Email.Contains(s)) || u.FullName.Contains(s));
        }
        var users = await q.OrderByDescending(u => u.CreatedAtUtc).ToListAsync(ct);

        var result = new List<UserSummary>(users.Count);
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            result.Add(new UserSummary(u.Id, u.Email ?? string.Empty, u.FullName, u.CreatedAtUtc, roles.ToArray()));
        }
        return result;
    }

    public async Task<PaginatedList<UserSummary>> GetUsersPagedAsync(string? search, string? role, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _userManager.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(u => (u.Email != null && u.Email.Contains(s)) || u.FullName.Contains(s));
        }
        if (!string.IsNullOrWhiteSpace(role))
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(role);
            var ids = usersInRole.Select(u => u.Id).ToHashSet();
            q = q.Where(u => ids.Contains(u.Id));
        }

        var totalCount = await q.CountAsync(ct);
        var users = await q
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var summaries = new List<UserSummary>(users.Count);
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            summaries.Add(new UserSummary(u.Id, u.Email ?? string.Empty, u.FullName, u.CreatedAtUtc, roles.ToArray()));
        }
        return new PaginatedList<UserSummary>(summaries, totalCount, page, pageSize);
    }

    public async Task<UserSummary?> GetUserAsync(string userId)
    {
        var u = await _userManager.FindByIdAsync(userId);
        if (u is null) return null;
        var roles = await _userManager.GetRolesAsync(u);
        return new UserSummary(u.Id, u.Email ?? string.Empty, u.FullName, u.CreatedAtUtc, roles.ToArray());
    }

    public async Task<Result> SetUserRolesAsync(string userId, IEnumerable<string> roles)
    {
        var u = await _userManager.FindByIdAsync(userId);
        if (u is null) return Result.Failure("User not found.");

        var current = await _userManager.GetRolesAsync(u);
        var target  = roles.Distinct().ToHashSet();
        var toRemove = current.Except(target).ToArray();
        var toAdd    = target.Except(current).ToArray();

        if (toRemove.Length > 0)
        {
            var r = await _userManager.RemoveFromRolesAsync(u, toRemove);
            if (!r.Succeeded) return Result.Failure(r.Errors.Select(e => e.Description).ToArray());
        }
        if (toAdd.Length > 0)
        {
            var r = await _userManager.AddToRolesAsync(u, toAdd);
            if (!r.Succeeded) return Result.Failure(r.Errors.Select(e => e.Description).ToArray());
        }
        return Result.Success();
    }

    public async Task<string?> GetUserNameAsync(string userId)
    {
        var u = await _userManager.FindByIdAsync(userId);
        return u?.UserName;
    }

    public async Task<bool> IsInRoleAsync(string userId, string role)
    {
        var u = await _userManager.FindByIdAsync(userId);
        return u != null && await _userManager.IsInRoleAsync(u, role);
    }

    public async Task<Result<string>> CreateUserAsync(string email, string password, string fullName, string role)
    {
        var user = new ApplicationUser { UserName = email, Email = email, FullName = fullName };
        var create = await _userManager.CreateAsync(user, password);
        if (!create.Succeeded) return Result<string>.Failure(create.Errors.Select(e => e.Description).ToArray());

        var addRole = await _userManager.AddToRoleAsync(user, role);
        if (!addRole.Succeeded) return Result<string>.Failure(addRole.Errors.Select(e => e.Description).ToArray());

        return Result<string>.Success(user.Id);
    }

    public async Task<Result> DeleteUserAsync(string userId)
    {
        var u = await _userManager.FindByIdAsync(userId);
        if (u is null) return Result.Failure("User not found.");
        var r = await _userManager.DeleteAsync(u);
        return r.Succeeded ? Result.Success() : Result.Failure(r.Errors.Select(e => e.Description).ToArray());
    }

    public async Task<Result> SignInAsync(string email, string password, bool rememberMe)
    {
        var r = await _signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: false);
        return r.Succeeded ? Result.Success() : Result.Failure("Invalid credentials.");
    }

    public Task SignOutAsync() => _signInManager.SignOutAsync();

    public async Task<Result> UpdateProfileAsync(string userId, string fullName, string? profileImageUrl)
    {
        var u = await _userManager.FindByIdAsync(userId);
        if (u is null) return Result.Failure("User not found.");
        u.FullName = fullName;
        if (profileImageUrl is not null) u.ProfileImageUrl = profileImageUrl;
        var r = await _userManager.UpdateAsync(u);
        return r.Succeeded ? Result.Success() : Result.Failure(r.Errors.Select(e => e.Description).ToArray());
    }

    public async Task<Result> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        var u = await _userManager.FindByIdAsync(userId);
        if (u is null) return Result.Failure("User not found.");
        var r = await _userManager.ChangePasswordAsync(u, currentPassword, newPassword);
        return r.Succeeded ? Result.Success() : Result.Failure(r.Errors.Select(e => e.Description).ToArray());
    }
}
