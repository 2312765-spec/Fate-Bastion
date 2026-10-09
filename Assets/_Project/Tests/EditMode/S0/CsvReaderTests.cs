using NUnit.Framework;
using FateBastion.Core;
using FateBastion.Editor.Import;

namespace FateBastion.Tests.EditMode.S0
{
    /// <summary>Unit tests for the balance CSV parser (S0 completion criteria).</summary>
    public class CsvReaderTests
    {
        private const string Bom = "﻿";

        [Test]
        public void Parse_QuotedFieldWithComma_KeepsCommaInsideCell()
        {
            const string text = "id,displayName,notes\n" +
                                "hero_cung_thu,Cung thu,\"Mui ten, set nay 2 quai; uu tien boss\"\n";

            CsvTable table = CsvReader.Parse(text);

            Assert.AreEqual(1, table.Rows.Count);
            Assert.IsTrue(table.Rows[0].TryGetString("notes", out string notes));
            Assert.AreEqual("Mui ten, set nay 2 quai; uu tien boss", notes);
        }

        [Test]
        public void Parse_SemicolonDelimitedFile_DetectsSemicolonAndKeepsQuotedSemicolon()
        {
            const string text = "id;displayName;notes\n" +
                                "hero_tu_si;Tu si;\"Buff sat thuong; trong 5 m\"\n";

            CsvTable table = CsvReader.Parse(text);

            Assert.AreEqual(';', table.Delimiter);
            Assert.AreEqual(3, table.Headers.Length);
            Assert.IsTrue(table.Rows[0].TryGetString("notes", out string notes));
            Assert.AreEqual("Buff sat thuong; trong 5 m", notes);
        }

        [Test]
        public void Parse_FileWithBom_StripsBomFromFirstHeader()
        {
            string text = Bom + "id,displayName\nhero_cung_thu,Cung thu\n";

            CsvTable table = CsvReader.Parse(text);

            Assert.AreEqual("id", table.Headers[0]);
            Assert.IsTrue(table.HasColumn("id"));
            Assert.IsTrue(table.Rows[0].TryGetString("id", out string id));
            Assert.AreEqual("hero_cung_thu", id);
        }

        [Test]
        public void Parse_RowStartingWithHash_IsSkipped()
        {
            const string text = "id,displayName\n" +
                                "# Ma khong dau,Ten hien thi\n" +
                                "hero_cung_thu,Cung thu\n";

            CsvTable table = CsvReader.Parse(text);

            Assert.AreEqual(1, table.Rows.Count);
            Assert.IsTrue(table.Rows[0].TryGetString("id", out string id));
            Assert.AreEqual("hero_cung_thu", id);
        }

        [Test]
        public void Parse_EscapedDoubleQuotes_UnescapeToSingleQuote()
        {
            const string text = "id,notes\n" +
                                "hero_cung_thu,\"He \"\"Loi\"\", nay 2 quai\"\n";

            CsvTable table = CsvReader.Parse(text);

            Assert.IsTrue(table.Rows[0].TryGetString("notes", out string notes));
            Assert.AreEqual("He \"Loi\", nay 2 quai", notes);
        }

        [Test]
        public void Parse_CrLfAndLf_ProduceSameRowCount()
        {
            const string lf = "id,value\na,1\nb,2\n";
            string crlf = lf.Replace("\n", "\r\n");

            Assert.AreEqual(CsvReader.Parse(lf).Rows.Count, CsvReader.Parse(crlf).Rows.Count);
            Assert.AreEqual(2, CsvReader.Parse(crlf).Rows.Count);
        }

        [Test]
        public void Parse_BlankLines_AreSkipped()
        {
            const string text = "id,value\n\na,1\n\n\nb,2\n\n";

            CsvTable table = CsvReader.Parse(text);

            Assert.AreEqual(2, table.Rows.Count);
        }

        [Test]
        public void Parse_LastRowWithoutTrailingNewline_IsStillRead()
        {
            const string text = "id,value\na,1";

            CsvTable table = CsvReader.Parse(text);

            Assert.AreEqual(1, table.Rows.Count);
            Assert.IsTrue(table.Rows[0].TryGetInt("value", out int value));
            Assert.AreEqual(1, value);
        }

        [Test]
        public void Parse_RowWithFewerCells_ReportsMissingColumnAsFalse()
        {
            const string text = "id,displayName,damage\nhero_cung_thu,Cung thu\n";

            CsvTable table = CsvReader.Parse(text);

            Assert.AreEqual(1, table.Rows.Count);
            Assert.IsFalse(table.Rows[0].TryGetFloat("damage", out _));
        }

        [Test]
        public void Parse_LineNumber_MatchesSourceFileLine()
        {
            const string text = "id,value\n" +
                                "# description row\n" +
                                "a,1\n" +
                                "b,2\n";

            CsvTable table = CsvReader.Parse(text);

            Assert.AreEqual(3, table.Rows[0].LineNumber);
            Assert.AreEqual(4, table.Rows[1].LineNumber);
        }

        [Test]
        public void Parse_EmptyText_Throws()
        {
            Assert.Throws<CsvFormatException>(() => CsvReader.Parse(string.Empty));
        }

        [Test]
        public void TryGetFloat_InvariantCulture_ParsesDotDecimal()
        {
            CsvTable table = CsvReader.Parse("attacksPerSecond\n1.25\n");

            Assert.IsTrue(table.Rows[0].TryGetFloat("attacksPerSecond", out float value));
            Assert.AreEqual(1.25f, value, 0.0001f);
        }

        [Test]
        public void TryGetFloat_InvalidNumber_ReturnsFalse()
        {
            CsvTable table = CsvReader.Parse("damage\nmuoi hai\n");

            Assert.IsFalse(table.Rows[0].TryGetFloat("damage", out _));
        }

        [Test]
        public void TryGetInt_WholeNumberWrittenAsFloat_IsAccepted()
        {
            CsvTable table = CsvReader.Parse("count\n7.0\n");

            Assert.IsTrue(table.Rows[0].TryGetInt("count", out int value));
            Assert.AreEqual(7, value);
        }

        [Test]
        public void TryGetBool_MixedCase_Parses()
        {
            CsvTable table = CsvReader.Parse("flying,boss\ntrue,FALSE\n");

            Assert.IsTrue(table.Rows[0].TryGetBool("flying", out bool flying));
            Assert.IsTrue(flying);
            Assert.IsTrue(table.Rows[0].TryGetBool("boss", out bool boss));
            Assert.IsFalse(boss);
        }

        [Test]
        public void TryGetEnum_LowercaseValue_ParsesIgnoreCase()
        {
            CsvTable table = CsvReader.Parse("rarity\nlegendary\n");

            Assert.IsTrue(table.Rows[0].TryGetEnum("rarity", out Rarity rarity));
            Assert.AreEqual(Rarity.Legendary, rarity);
        }

        [Test]
        public void TryGetEnum_UnknownValue_ReturnsFalse()
        {
            CsvTable table = CsvReader.Parse("rarity\nMythic\n");

            Assert.IsFalse(table.Rows[0].TryGetEnum("rarity", out Rarity _));
        }

        [Test]
        public void TryGetEnum_NumericValue_ReturnsFalse()
        {
            // A typo that happens to be a number must not silently become a valid enum member.
            CsvTable table = CsvReader.Parse("rarity\n3\n");

            Assert.IsFalse(table.Rows[0].TryGetEnum("rarity", out Rarity _));
        }

        [Test]
        public void TryGetString_UnknownColumn_ReturnsFalse()
        {
            CsvTable table = CsvReader.Parse("id\na\n");

            Assert.IsFalse(table.Rows[0].TryGetString("calc_totalHP", out _));
            Assert.IsFalse(table.Rows[0].HasColumn("calc_totalHP"));
        }
    }
}
