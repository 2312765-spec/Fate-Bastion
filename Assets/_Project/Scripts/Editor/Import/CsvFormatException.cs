using System;

namespace FateBastion.Editor.Import
{
    /// <summary>Thrown when a CSV file cannot be parsed at all (empty file, missing header row).</summary>
    public class CsvFormatException : Exception
    {
        public CsvFormatException(string message) : base(message)
        {
        }
    }
}
