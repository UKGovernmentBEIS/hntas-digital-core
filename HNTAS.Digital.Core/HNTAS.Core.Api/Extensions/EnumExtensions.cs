using System.ComponentModel;
using System.Reflection;

namespace HNTAS.Core.Api.Extensions
{
    public static class EnumExtensions
    {
        public static string GetDescription(this Enum value)
        {
            FieldInfo field = value.GetType().GetField(value.ToString());
            if (field == null) return value.ToString(); // Fallback to enum name

            DescriptionAttribute[] attributes = field.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];
            return (attributes != null && attributes.Length > 0) ? attributes[0].Description : value.ToString();
        }
    }
}
