using FluentValidation;

namespace FileManagerApi.Contracts
{
    public class UploadImageRequestValidator:AbstractValidator<UploadImageRequest>
    {
        private readonly string[] _allowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".bmp"];
        public UploadImageRequestValidator()
        {
            RuleFor(x=>x.Image)
                .SetValidator(new FileSizeValidator())
                .SetValidator(new LockedSignatureValidator());

            RuleFor(x=>x.Image)
                .Must(x =>
                {
                    var extensions = Path.GetExtension(x.FileName.ToLower());
                    return _allowedExtensions.Contains(extensions);
                })
                .WithMessage("File must be an image")
                .When(x => x.Image != null);
        }
    }
}
