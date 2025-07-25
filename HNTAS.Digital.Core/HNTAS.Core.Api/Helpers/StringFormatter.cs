using HNTAS.Core.Api.Models;

namespace HNTAS.Core.Api.Helpers
{
    public class StringFormatter
    {
        public static string ToTitleCaseSingleWord(string? input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }
            // Convert the first character to uppercase and the rest to lowercase.
            return char.ToUpper(input[0]) + input.Substring(1).ToLower();
        }

        public static string FormatAddress(OrgRegisteredAddress? address)
        {
            if (address == null)
            {
                return "";
            }

            var parts = new List<string?>
            {
                address.AddressLine1,
                address.AddressLine2,
                address.Town,
                address.County,
                address.Country,
                address.Postcode
            };

            return string.Join(", ", parts.Where(p => !string.IsNullOrEmpty(p)));
        }
    }
}
