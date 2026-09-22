using FluentValidation;
using System.Text.RegularExpressions;

namespace FileManagerApi.Contracts
{
    public class UploadFileRequestValidator : AbstractValidator<UploadFileRequest>
    {
        private const long MaxFileSize = 1 * 1024 * 1024;
        public static readonly string[] BlockedSignature = ["4D-5A", "2F-2A", "D0-CF"];
        public UploadFileRequestValidator()
        {
            RuleFor(x => x.File)
                .Must(file => file.Length <= MaxFileSize)
                .WithMessage("File size must not exceed 1 MB.")
                .When(x => x.File != null);

            RuleFor(x => x.File)
                .Must(file =>
                {
                    using var reader = new BinaryReader(file.OpenReadStream());
                    var signatureBytes = reader.ReadBytes(2);
                    var signature = BitConverter.ToString(signatureBytes);
                    return !BlockedSignature.Contains(signature);
                })
                .WithMessage("Not Allowed File Content.")
                .When(x => x.File != null);

            RuleFor(x => x.File)
                .Must(file => Regex.IsMatch(file.FileName, @"^[^\/\\]+$"))
                .WithMessage("File name cannot contain '/' or '\\'.")
                .When(x => x.File != null);
        }
    }
}
