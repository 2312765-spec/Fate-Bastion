using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FateBastion.Editor.Import
{
    /// <summary>
    /// CSV parser for the balance files (S0). Pure logic with no Unity or AssetDatabase dependency so it can be
    /// unit tested: strips the UTF-8 BOM, detects ',' or ';' from the header row, understands quoted cells
    /// (the notes column contains commas and semicolons) and "" escapes, and skips comment and blank rows.
    /// </summary>
    public static class CsvReader
    {
        private const char Quote = '"';
        private const char CommentMarker = '#';

        /// <summary>Reads a file as UTF-8 and parses it.</summary>
        public static CsvTable ParseFile(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("CSV file not found: " + path, path);
            }

            return Parse(File.ReadAllText(path, Encoding.UTF8));
        }

        /// <summary>Parses CSV text. The first row that is neither blank nor a comment is the header.</summary>
        public static CsvTable Parse(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            // File.ReadAllText with an explicit UTF8 encoding keeps the BOM as U+FEFF in the first cell.
            text = text.TrimStart('﻿');

            char delimiter = DetectDelimiter(text);
            List<RawRecord> records = Tokenize(text, delimiter);
            if (records.Count == 0)
            {
                throw new CsvFormatException("CSV is empty: no header row found.");
            }

            string[] headers = records[0].Cells;
            for (int i = 0; i < headers.Length; i++)
            {
                headers[i] = headers[i].Trim();
            }

            var table = new CsvTable(headers, delimiter);
            for (int i = 1; i < records.Count; i++)
            {
                table.AddRow(records[i].Cells, records[i].LineNumber);
            }

            return table;
        }

        /// <summary>
        /// Picks the separator by counting ',' and ';' outside quotes on the header row. Vietnamese Windows
        /// Excel can save with ';', and the header row never holds a separator inside a cell (S0).
        /// </summary>
        public static char DetectDelimiter(string text)
        {
            text = text.TrimStart('﻿');

            int commas = 0;
            int semicolons = 0;
            bool inQuotes = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == Quote)
                {
                    inQuotes = !inQuotes;
                }
                else if (inQuotes)
                {
                    continue;
                }
                else if (c == '\n' || c == '\r')
                {
                    break;
                }
                else if (c == ',')
                {
                    commas++;
                }
                else if (c == ';')
                {
                    semicolons++;
                }
            }

            return semicolons > commas ? ';' : ',';
        }

        private static List<RawRecord> Tokenize(string text, char delimiter)
        {
            var records = new List<RawRecord>();
            var cells = new List<string>();
            var cell = new StringBuilder();

            bool inQuotes = false;
            bool cellWasQuoted = false;
            int line = 1;
            int recordStartLine = 1;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (inQuotes)
                {
                    if (c == Quote)
                    {
                        if (i + 1 < text.Length && text[i + 1] == Quote)
                        {
                            cell.Append(Quote);
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        if (c == '\n')
                        {
                            line++;
                        }

                        cell.Append(c);
                    }

                    continue;
                }

                if (c == Quote)
                {
                    inQuotes = true;
                    cellWasQuoted = true;
                }
                else if (c == delimiter)
                {
                    cells.Add(Finish(cell, cellWasQuoted));
                    cellWasQuoted = false;
                }
                else if (c == '\r')
                {
                    // Swallow the CR of a CRLF pair; the LF below ends the record.
                    if (i + 1 < text.Length && text[i + 1] == '\n')
                    {
                        continue;
                    }

                    EndRecord(records, cells, cell, ref cellWasQuoted, recordStartLine);
                    line++;
                    recordStartLine = line;
                }
                else if (c == '\n')
                {
                    EndRecord(records, cells, cell, ref cellWasQuoted, recordStartLine);
                    line++;
                    recordStartLine = line;
                }
                else
                {
                    cell.Append(c);
                }
            }

            // Last record when the file does not end with a newline.
            if (cell.Length > 0 || cells.Count > 0 || cellWasQuoted)
            {
                EndRecord(records, cells, cell, ref cellWasQuoted, recordStartLine);
            }

            return records;
        }

        private static void EndRecord(List<RawRecord> records, List<string> cells, StringBuilder cell,
            ref bool cellWasQuoted, int lineNumber)
        {
            cells.Add(Finish(cell, cellWasQuoted));
            cellWasQuoted = false;

            if (!IsSkippable(cells))
            {
                records.Add(new RawRecord(cells.ToArray(), lineNumber));
            }

            cells.Clear();
        }

        private static string Finish(StringBuilder cell, bool wasQuoted)
        {
            // Quoted cells are kept verbatim; unquoted cells are trimmed so stray spaces do not break parsing.
            string value = wasQuoted ? cell.ToString() : cell.ToString().Trim();
            cell.Clear();
            return value;
        }

        /// <summary>A blank line, or a comment row whose first cell starts with '#' (the Vietnamese description row).</summary>
        private static bool IsSkippable(List<string> cells)
        {
            if (cells.Count == 0)
            {
                return true;
            }

            string first = cells[0];
            if (cells.Count == 1 && first.Length == 0)
            {
                return true;
            }

            return first.Length > 0 && first[0] == CommentMarker;
        }

        private readonly struct RawRecord
        {
            public readonly string[] Cells;
            public readonly int LineNumber;

            public RawRecord(string[] cells, int lineNumber)
            {
                Cells = cells;
                LineNumber = lineNumber;
            }
        }
    }
}
