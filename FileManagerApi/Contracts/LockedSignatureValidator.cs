using FluentValidation;
using System.Text.RegularExpressions;

namespace FileManagerApi.Contracts
{
    public class LockedSignatureValidator : AbstractValidator<IFormFile>
    {
        private static readonly string[] BlockedSignature = ["4D-5A", "2F-2A", "D0-CF"];
        public LockedSignatureValidator()
        {
            RuleFor(x => x)
               .Must(file =>
               {
                   using var reader = new BinaryReader(file.OpenReadStream());
                   var signatureBytes = reader.ReadBytes(2);
                   var signature = BitConverter.ToString(signatureBytes);
                   return !BlockedSignature.Contains(signature);
               })
               .WithMessage("Not Allowed File Content.")
               .When(x => x != null);

            RuleFor(x => x)
                .Must(file => Regex.IsMatch(file.FileName, @"^[^\/\\]+$"))
                .WithMessage("File name cannot contain '/' or '\\'.")
                .When(x => x != null);
        }
    }
}
