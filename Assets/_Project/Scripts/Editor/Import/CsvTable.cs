using System;
using System.Collections.Generic;

namespace FateBastion.Editor.Import
{
    /// <summary>Header plus data rows of one parsed CSV file (S0).</summary>
    public sealed class CsvTable
    {
        private readonly Dictionary<string, int> _columnIndexByName;

        /// <summary>Column names from row 1, in file order.</summary>
        public string[] Headers { get; }

        public List<CsvRow> Rows { get; } = new List<CsvRow>();

        /// <summary>Separator detected from the header row: ',' or ';'.</summary>
        public char Delimiter { get; }

        internal CsvTable(string[] headers, char delimiter)
        {
            Headers = headers;
            Delimiter = delimiter;
            _columnIndexByName = new Dictionary<string, int>(headers.Length, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Length; i++)
            {
                // A duplicated header would otherwise throw; the first one wins.
                if (!_columnIndexByName.ContainsKey(headers[i]))
                {
                    _columnIndexByName.Add(headers[i], i);
                }
            }
        }

        public bool TryGetColumnIndex(string column, out int index) => _columnIndexByName.TryGetValue(column, out index);

        public bool HasColumn(string column) => _columnIndexByName.ContainsKey(column);

        internal void AddRow(string[] cells, int lineNumber) => Rows.Add(new CsvRow(this, cells, lineNumber));
    }
}
