using FluentValidation;

namespace FileManagerApi.Contracts
{
    public class UploadImageRequestValidator : AbstractValidator<UploadImageRequest>
    {
        private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".bmp"];

        public UploadImageRequestValidator()
        {
            RuleFor(x => x.Image)
                .NotNull().WithMessage("An image is required.");

            RuleFor(x => x.Image)
                .SetValidator(new FileSizeValidator());

            RuleFor(x => x.Image)
                .SetValidator(new LockedSignatureValidator());

            RuleFor(x => x.Image)
                .Must(x =>
                {
                    var extension = Path.GetExtension(x.FileName).ToLowerInvariant();
                    return AllowedExtensions.Contains(extension);
                })
                .WithMessage("File must be an image")
                .When(x => x.Image != null);

            RuleFor(x => x.Image)
                .Must(HaveValidImageSignature)
                .WithMessage("File content does not match a valid image format.")
                .When(x => x.Image != null);
        }

        private static bool HaveValidImageSignature(IFormFile image)
        {
            using var reader = new BinaryReader(image.OpenReadStream());
            var header = reader.ReadBytes(8);

            return IsJpeg(header) || IsPng(header) || IsGif(header) || IsBmp(header);
        }

        private static bool IsJpeg(byte[] header) =>
            header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;

        private static bool IsPng(byte[] header) =>
            header.Length >= 8 &&
            header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
            header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A;

        private static bool IsGif(byte[] header) =>
            header.Length >= 4 && header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38;

        private static bool IsBmp(byte[] header) =>
            header.Length >= 2 && header[0] == 0x42 && header[1] == 0x4D;
    }
}