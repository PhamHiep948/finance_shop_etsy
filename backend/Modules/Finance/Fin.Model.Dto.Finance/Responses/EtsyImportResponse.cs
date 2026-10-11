namespace Fin.Model.Dto.Finance.Responses
{
    public sealed class EtsyImportResponse
    {
        public EtsyImportResponse(int imported, int skipped)
        {
            Imported = imported;
            Skipped = skipped;
        }

        public int Imported { get; }
        public int Skipped { get; }
    }
}
