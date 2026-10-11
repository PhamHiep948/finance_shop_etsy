using Fin.Service.Finance.Services;

namespace Fin.Tests.Unit.Services
{
    public sealed class AttachmentRulesTests
    {
        [Theory]
        [InlineData("hoa-don.pdf", 1024, null)]
        [InlineData("anh.JPG", 1024, null)]
        [InlineData("virus.exe", 1024, "không được hỗ trợ")]
        [InlineData("rong.pdf", 0, "rỗng")]
        [InlineData("to.pdf", 11 * 1024 * 1024, "vượt quá 10 MB")]
        public void Validates_type_and_size(string fileName, long length, string? expectedError)
        {
            var error = AttachmentRules.Validate(fileName, length);

            if (expectedError is null)
            {
                Assert.Null(error);
            }
            else
            {
                Assert.Contains(expectedError, error);
            }
        }

        [Fact]
        public void Only_images_pdf_and_text_open_in_the_browser()
        {
            Assert.True(AttachmentRules.CanPreview("image/png"));
            Assert.True(AttachmentRules.CanPreview("application/pdf"));
            Assert.False(AttachmentRules.CanPreview("application/zip"));
        }
    }
}
