namespace Fin.Model.Dto.Finance.Responses
{
    public sealed class PurgeResponse
    {
        public PurgeResponse(int purged) => Purged = purged;

        public int Purged { get; }
    }
}
