using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Vortex.Addin.PartData.Core
{
    internal static class PartValidator
    {
        internal static bool FormatDecimal(object value, out string result)
        {
            result = string.Empty;
            if (value == null) return true;

            string[] parts = value.ToString().Replace(",", ".").Split('.');
            string format = (parts.Length > 1 && parts[1].Length > 1) ? "0.00" : "0.0";

            if (decimal.TryParse(value.ToString().Replace(",", "."),
                NumberStyles.Any, CultureInfo.InvariantCulture, out decimal res))
            {
                result = res.ToString(format, CultureInfo.InvariantCulture);
                return false;
            }
            return true;
        }

        internal static bool FormatInteger(object value, int digits, out string result)
        {
            result = string.Empty;
            if (value == null) return true;

            if (int.TryParse(value.ToString(), out int res) && res > 0)
            {
                result = res.ToString(new string('0', digits));
                return result.Length > digits;
            }
            return true;
        }

        internal static bool ValidateCOD(string value, List<List<string>> items)
        {
            if (string.IsNullOrEmpty(value) || items.Count == 0) return false;

            return items.Any(row =>
                row.Count >= 3 &&
                $"{row[0].Trim()}.{row[1].Trim()}.{row[2].Trim()}" == value);
        }

        internal static bool ValidateCat(string value, List<string> categorias)
        {
            return !categorias.Any(v => v.Trim() == value);
        }
    }
}
