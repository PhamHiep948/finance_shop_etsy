namespace Fin.Model.Dto.Finance.Responses
{
    public sealed class IdResponse
    {
        public IdResponse(long id) => Id = id;

        public long Id { get; }
    }
}
