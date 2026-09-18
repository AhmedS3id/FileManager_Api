namespace FileManagerApi.Entities
{
    public sealed class UploadedFiles
    {
        public Guid Id { get; set; }= Guid.CreateVersion7();
        public string FileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string ContentTybe { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
    }
}
