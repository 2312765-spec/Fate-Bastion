using System;
using System.Globalization;

namespace FateBastion.Editor.Import
{
    /// <summary>
    /// One data row of a CSV file. Every getter is a Try* so a bad cell can be reported with its line
    /// number and the row skipped, without aborting the whole import (S0).
    /// </summary>
    public sealed class CsvRow
    {
        private readonly CsvTable _table;
        private readonly string[] _cells;

        /// <summary>1-based line number in the source file, used in error messages.</summary>
        public int LineNumber { get; }

        public int CellCount => _cells.Length;

        internal CsvRow(CsvTable table, string[] cells, int lineNumber)
        {
            _table = table;
            _cells = cells;
            LineNumber = lineNumber;
        }

        /// <summary>Raw cell by column index; empty string when the row is shorter than the header.</summary>
        public string this[int index] => index >= 0 && index < _cells.Length ? _cells[index] : string.Empty;

        public bool HasColumn(string column) => _table.TryGetColumnIndex(column, out _);

        /// <summary>False when the column does not exist, the row is too short, or the cell is empty.</summary>
        public bool TryGetString(string column, out string value)
        {
            value = string.Empty;
            if (!_table.TryGetColumnIndex(column, out int index) || index >= _cells.Length)
            {
                return false;
            }

            value = _cells[index];
            return value.Length > 0;
        }

        public bool TryGetFloat(string column, out float value)
        {
            value = 0f;
            return TryGetString(column, out string raw)
                   && float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        public bool TryGetInt(string column, out int value)
        {
            value = 0;
            if (!TryGetString(column, out string raw))
            {
                return false;
            }

            // Excel can export a whole number as "7.0"; accept it rather than failing the row.
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }

            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float asFloat)
                && Math.Abs(asFloat - Math.Round(asFloat)) < 0.0001f)
            {
                value = (int)Math.Round(asFloat);
                return true;
            }

            return false;
        }

        /// <summary>Accepts TRUE / FALSE in any casing, and also 1 / 0.</summary>
        public bool TryGetBool(string column, out bool value)
        {
            value = false;
            if (!TryGetString(column, out string raw))
            {
                return false;
            }

            raw = raw.Trim();
            if (bool.TryParse(raw, out value))
            {
                return true;
            }

            if (raw == "1")
            {
                value = true;
                return true;
            }

            if (raw == "0")
            {
                value = false;
                return true;
            }

            return false;
        }

        public bool TryGetEnum<T>(string column, out T value) where T : struct
        {
            value = default;
            if (!TryGetString(column, out string raw))
            {
                return false;
            }

            // Enum.TryParse accepts plain numbers too, which would silently turn a typo into a valid value.
            raw = raw.Trim();
            if (raw.Length == 0 || char.IsDigit(raw[0]) || raw[0] == '-')
            {
                return false;
            }

            return Enum.TryParse(raw, ignoreCase: true, out value) && Enum.IsDefined(typeof(T), value);
        }
    }
}
