using FluentValidation;

namespace FileManagerApi.Contracts
{
    public class UploadFileRequestValidator : AbstractValidator<UploadFileRequest>
    {
        public UploadFileRequestValidator()
        {
            RuleFor(x=>x.File)
                .Must(file => file.Length <= 1 * 1024 * 1024)
                .WithMessage("File size must not exceed 1 MB.")
                .When(x => x.File != null);
        }
    }
}
