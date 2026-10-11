namespace Fin.Domain.Finance.Constants
{
    public static class CategoryKinds
    {
        public const string Income = "INCOME";
        public const string Expense = "EXPENSE";
        public const string Scope = "SCOPE";

        public static string? FromRoute(string? type) => type?.Trim().ToLowerInvariant() switch
        {
            "income" => Income,
            "expense" => Expense,
            "scope" => Scope,
            _ => null,
        };

        public static string Label(string kind) => kind switch
        {
            Income => "Loại thu",
            Scope => "Phạm vi khoản chi",
            _ => "Loại chi",
        };
    }
}
