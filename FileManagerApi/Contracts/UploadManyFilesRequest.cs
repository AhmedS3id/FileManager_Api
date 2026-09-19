namespace FileManagerApi.Contracts
{
    public record UploadManyFilesRequest(
        IFormFileCollection Files
        );
}
