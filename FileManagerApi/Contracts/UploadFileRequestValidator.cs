using FluentValidation;

namespace FileManagerApi.Contracts
{
    public class UploadFileRequestValidator : AbstractValidator<UploadFileRequest>
    {
        public UploadFileRequestValidator()
        {
            RuleFor(x => x.File)
                .NotNull().WithMessage("A file is required.");

            RuleFor(x => x.File)
                .SetValidator(new FileSizeValidator());

            RuleFor(x => x.File)
                .SetValidator(new LockedSignatureValidator());
        }
    }
}