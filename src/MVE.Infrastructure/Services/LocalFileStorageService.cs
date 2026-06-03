using Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;

    public LocalFileStorageService(IWebHostEnvironment env) => _env = env;

    public async Task<string> SaveAsync(IFormFile file, string folder, CancellationToken ct = default)
    {
        var uploads = Path.Combine(_env.WebRootPath, "uploads", folder);
        Directory.CreateDirectory(uploads);

        var ext = Path.GetExtension(file.FileName);
        var name = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploads, name);

        await using var stream = File.Create(fullPath);
        await file.CopyToAsync(stream, ct);

        return $"/uploads/{folder}/{name}";
    }

    public async Task<List<string>> SaveManyAsync(IEnumerable<IFormFile> files, string folder, CancellationToken ct = default)
    {
        var urls = new List<string>();
        foreach (var f in files) urls.Add(await SaveAsync(f, folder, ct));
        return urls;
    }

    public Task DeleteAsync(string relativePath)
    {
        var full = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/'));
        if (File.Exists(full)) File.Delete(full);
        return Task.CompletedTask;
    }
}
