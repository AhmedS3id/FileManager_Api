namespace FileManagerApi.Services
{
    public interface IFileService
    {
        Task<Guid> UploadAsync(IFormFile file, CancellationToken cancellationToken = default);
    }
}
