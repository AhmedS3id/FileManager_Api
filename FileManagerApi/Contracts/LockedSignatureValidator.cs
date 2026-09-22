using FluentValidation;

namespace FileManagerApi.Contracts
{
    public class LockedSignatureValidator : AbstractValidator<IFormFile>
    {
        private static readonly string[] BlockedSignatures = ["4D-5A", "2F-2A", "D0-CF", "7F-45", "23-21"];
        private static readonly string[] ReservedFileNames = [".", ".."];
        private const int MaxFileNameLength = 255;

        public LockedSignatureValidator()
        {
            RuleFor(x => x)
               .Must(file =>
               {
                   using var reader = new BinaryReader(file.OpenReadStream());
                   var signatureBytes = reader.ReadBytes(2);
                   var signature = BitConverter.ToString(signatureBytes);
                   return !BlockedSignatures.Contains(signature);
               })
               .WithMessage("Not Allowed File Content.")
               .When(x => x != null);

            RuleFor(x => x)
                .Must(file => IsSafeFileName(file.FileName))
                .WithMessage("File name is invalid.")
                .When(x => x != null);
        }

        private static bool IsSafeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > MaxFileNameLength)
                return false;

            if (ReservedFileNames.Contains(fileName))
                return false;

            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var c in fileName)
            {
                if (c is '/' or '\\' || char.IsControl(c) || Array.IndexOf(invalidChars, c) >= 0)
                    return false;
            }

            return true;
        }
    }
}