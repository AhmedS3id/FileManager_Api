namespace FileManagerApi.Services
{
    public interface IFileService
    {
        Task<Guid> UploadAsync(IFormFile file, CancellationToken cancellationToken = default);
        Task<ICollection<Guid>> UploadManyAsync(IFormFileCollection files, CancellationToken cancellationToken = default);
        Task UploadImageAsync(IFormFile Image, CancellationToken cancellationToken = default);
        Task<(byte[] fileContent,string contentType,string fileName)> DownloadAsync(Guid Id, CancellationToken cancellationToken = default);
    }
}
