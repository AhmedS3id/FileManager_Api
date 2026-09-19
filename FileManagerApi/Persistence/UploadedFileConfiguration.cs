using FileManagerApi.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileManagerApi.Persistence
{
    public class UploadedFileConfiguration : IEntityTypeConfiguration<UploadedFilesRequest>
    {
        public void Configure(EntityTypeBuilder<UploadedFilesRequest> builder)
        {
           builder.Property(x=>x.ContentTybe).HasMaxLength(250);
           builder.Property(x=>x.StoredFileName).HasMaxLength(250);
           builder.Property(x=>x.ContentTybe).HasMaxLength(50);
           builder.Property(x=>x.FileExtension).HasMaxLength(10);
        }
    }
}
