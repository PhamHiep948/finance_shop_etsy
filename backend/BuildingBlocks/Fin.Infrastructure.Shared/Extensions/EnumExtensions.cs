using System.ComponentModel;
using System.Reflection;

namespace Fin.Infrastructure.Shared.Extensions
{
    public static class EnumExtensions
    {
        public static string GetDescription(this Enum value)
        {
            var member = value.GetType().GetField(value.ToString());
            return member?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? value.ToString();
        }
    }
}
