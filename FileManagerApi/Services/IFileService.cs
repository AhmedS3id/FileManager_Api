namespace FileManagerApi.Services
{
    public interface IFileService
    {
        Task<Guid> UploadAsync(IFormFile file, CancellationToken cancellationToken = default);
        Task<ICollection<Guid>> UploadManyAsync(IFormFileCollection files, CancellationToken cancellationToken = default);
        Task<Guid> UploadImageAsync(IFormFile image, CancellationToken cancellationToken = default);
        Task<(Stream? fileContent, string contentType, string fileName)> DownloadAsync(Guid id, CancellationToken cancellationToken = default);
        Task<(Stream? stream, string contentType, string fileName)> StreamAsync(Guid id, CancellationToken cancellationToken = default);
    }
}