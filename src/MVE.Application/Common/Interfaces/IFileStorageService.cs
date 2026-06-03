using Microsoft.AspNetCore.Http;

namespace Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveAsync(IFormFile file, string folder, CancellationToken ct = default);
    Task<List<string>> SaveManyAsync(IEnumerable<IFormFile> files, string folder, CancellationToken ct = default);
    Task DeleteAsync(string relativePath);
}
