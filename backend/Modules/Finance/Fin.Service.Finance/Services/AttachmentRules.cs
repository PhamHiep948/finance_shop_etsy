namespace Fin.Service.Finance.Services
{
    public static class AttachmentRules
    {
        public const int MaxFilesPerUpload = 10;
        public const int OriginalNameMaxLength = 255;
        private const long MaxBytes = 10 * 1024 * 1024;

        private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".gif"] = "image/gif",
            [".webp"] = "image/webp",
            [".pdf"] = "application/pdf",
            [".txt"] = "text/plain",
            [".csv"] = "text/csv",
            [".doc"] = "application/msword",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".xls"] = "application/vnd.ms-excel",
            [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [".zip"] = "application/zip",
        };

        public static string? Validate(string fileName, long length)
        {
            if (!ContentTypes.ContainsKey(Path.GetExtension(fileName)))
            {
                return $"File \"{fileName}\" không được hỗ trợ. Chỉ nhận ảnh, PDF, Word, Excel, CSV, TXT, ZIP.";
            }
            if (length == 0)
            {
                return $"File \"{fileName}\" rỗng.";
            }
            if (length > MaxBytes)
            {
                return $"File \"{fileName}\" vượt quá 10 MB.";
            }
            return null;
        }

        public static string ContentTypeOf(string fileName) => ContentTypes[Path.GetExtension(fileName)];

        public static bool CanPreview(string contentType) =>
            contentType.StartsWith("image/", StringComparison.Ordinal) || contentType is "application/pdf" or "text/plain";

        public static string DisplayName(string fileName)
        {
            var name = Path.GetFileName(fileName);
            return name.Length > OriginalNameMaxLength ? name[^OriginalNameMaxLength..] : name;
        }
    }
}
