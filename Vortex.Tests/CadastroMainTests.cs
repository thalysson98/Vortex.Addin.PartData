using System.Collections.Generic;
using System.Data;
using Vortex.Addin.PartData.Core;
using Xunit;

namespace Vortex.Tests
{
    // ── PartValidator ────────────────────────────────────────────────────────────

    public class PartValidatorTests
    {
        // FormatDecimal

        [Theory]
        [InlineData("10",    "10.0")]
        [InlineData("10.5",  "10.5")]
        [InlineData("10.50", "10.50")]
        [InlineData("3,14",  "3.1")]   // vírgula → ponto; 1 casa → "0.0"
        public void FormatDecimal_ValidInput_ReturnsFormattedString(string input, string expected)
        {
            bool isError = PartValidator.FormatDecimal(input, out string result);
            Assert.False(isError);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void FormatDecimal_NullInput_ReturnsError()
        {
            bool isError = PartValidator.FormatDecimal(null, out _);
            Assert.True(isError);
        }

        [Fact]
        public void FormatDecimal_NonNumericInput_ReturnsError()
        {
            bool isError = PartValidator.FormatDecimal("abc", out _);
            Assert.True(isError);
        }

        // FormatInteger

        [Theory]
        [InlineData("5",   3, "005")]
        [InlineData("12",  3, "012")]
        [InlineData("1",   4, "0001")]
        [InlineData("100", 3, "100")]
        public void FormatInteger_ValidInput_ReturnsZeroPadded(string input, int digits, string expected)
        {
            bool isError = PartValidator.FormatInteger(input, digits, out string result);
            Assert.False(isError);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void FormatInteger_NullInput_ReturnsError()
        {
            bool isError = PartValidator.FormatInteger(null, 3, out _);
            Assert.True(isError);
        }

        [Fact]
        public void FormatInteger_Zero_ReturnsError()
        {
            bool isError = PartValidator.FormatInteger("0", 3, out _);
            Assert.True(isError);
        }

        [Fact]
        public void FormatInteger_ExceedsDigits_ReturnsError()
        {
            bool isError = PartValidator.FormatInteger("9999", 3, out _);
            Assert.True(isError);
        }

        // ValidateCOD

        [Fact]
        public void ValidateCOD_ExistingCode_ReturnsTrue()
        {
            var items = new List<List<string>>
            {
                new List<string> { "001", "002", "0001" },
                new List<string> { "001", "003", "0002" }
            };
            Assert.True(PartValidator.ValidateCOD("001.002.0001", items));
        }

        [Fact]
        public void ValidateCOD_NonExistingCode_ReturnsFalse()
        {
            var items = new List<List<string>>
            {
                new List<string> { "001", "002", "0001" }
            };
            Assert.False(PartValidator.ValidateCOD("999.999.9999", items));
        }

        [Fact]
        public void ValidateCOD_EmptyList_ReturnsFalse()
        {
            Assert.False(PartValidator.ValidateCOD("001.002.0001", new List<List<string>>()));
        }

        [Fact]
        public void ValidateCOD_NullOrEmptyCode_ReturnsFalse()
        {
            var items = new List<List<string>> { new List<string> { "001", "002", "0001" } };
            Assert.False(PartValidator.ValidateCOD(null, items));
            Assert.False(PartValidator.ValidateCOD("", items));
        }

        // ValidateCat

        [Fact]
        public void ValidateCat_ExistingCategory_ReturnsFalse()
        {
            var cats = new List<string> { "ACO", "INOX", "ALUMINIO" };
            Assert.False(PartValidator.ValidateCat("ACO", cats));
        }

        [Fact]
        public void ValidateCat_NonExistingCategory_ReturnsTrue()
        {
            var cats = new List<string> { "ACO", "INOX" };
            Assert.True(PartValidator.ValidateCat("COBRE", cats));
        }

        [Fact]
        public void ValidateCat_TrimsWhitespace()
        {
            var cats = new List<string> { " ACO " };
            Assert.False(PartValidator.ValidateCat("ACO", cats));
        }
    }

    // ── SQLCommands (cache operations) ──────────────────────────────────────────

    public class SQLCommandsCacheTests
    {
        private static SQLCommands BuildSql()
        {
            var materiais = new DataTable();
            materiais.Columns.Add("CATEGORIA");
            materiais.Columns.Add("DIAMETRO");
            materiais.Columns.Add("ESPESSURA");
            materiais.Columns.Add("COMPRIMENTO");
            materiais.Columns.Add("M4");
            materiais.Columns.Add("COD1");
            materiais.Columns.Add("COD2");
            materiais.Columns.Add("COD3");
            materiais.Rows.Add("ACO",   "10.0", "2.0", "100.0", "0",  "001", "002", "0001");
            materiais.Rows.Add("ACO",   "12.0", "3.0", "150.0", "0",  "001", "002", "0002");
            materiais.Rows.Add("INOX",  "8.0",  "1.5", "80.0",  "0",  "002", "001", "0001");

            var categorias = new DataTable();
            categorias.Columns.Add("MATERIAL");
            categorias.Columns.Add("TIPO");
            categorias.Rows.Add("ACO",  "1");
            categorias.Rows.Add("INOX", "2");

            return new SQLCommands(new Dictionary<string, DataTable>
            {
                ["MATERIAIS"]  = materiais,
                ["CATEGORIAS"] = categorias
            });
        }

        [Fact]
        public void GetValColumn_ReturnsDistinctValues()
        {
            var sql = BuildSql();
            var result = sql.GetValColumn("CATEGORIA", "MATERIAIS");
            Assert.Equal(2, result.Count);
            Assert.Contains("ACO",  result);
            Assert.Contains("INOX", result);
        }

        [Fact]
        public void GetValColumn_UnknownTable_ReturnsEmpty()
        {
            var sql = BuildSql();
            var result = sql.GetValColumn("COL", "TABELA_INEXISTENTE");
            Assert.Empty(result);
        }

        [Fact]
        public void GetAllValues_ReturnsAllRows()
        {
            var sql = BuildSql();
            var result = sql.GetAllValues(new List<string> { "COD1", "COD2", "COD3" }, "MATERIAIS");
            Assert.Equal(3, result.Count);
        }

        [Fact]
        public void GetRowValues_FilterBySingleColumn_ReturnsMatches()
        {
            var sql = BuildSql();
            var filtros = new Dictionary<string, object> { ["CATEGORIA"] = "ACO" };
            var result = sql.GetRowValues(filtros, new List<string> { "DIAMETRO" }, "MATERIAIS");
            Assert.Equal(2, result.Count);
            Assert.Contains("10.0", result);
            Assert.Contains("12.0", result);
        }

        [Fact]
        public void GetRowValues_FilterByMultipleColumns_ReturnsNarrowMatch()
        {
            var sql = BuildSql();
            var filtros = new Dictionary<string, object>
            {
                ["CATEGORIA"] = "ACO",
                ["DIAMETRO"]  = "10.0"
            };
            var result = sql.GetRowValues(filtros, new List<string> { "ESPESSURA" }, "MATERIAIS");
            Assert.Single(result);
            Assert.Equal("2.0", result[0]);
        }

        [Fact]
        public void GetRowValues_NoMatch_ReturnsEmpty()
        {
            var sql = BuildSql();
            var filtros = new Dictionary<string, object> { ["CATEGORIA"] = "TITANIO" };
            var result = sql.GetRowValues(filtros, new List<string> { "DIAMETRO" }, "MATERIAIS");
            Assert.Empty(result);
        }

        [Fact]
        public void ObterTabela_KnownTable_ReturnsDataTable()
        {
            var sql = BuildSql();
            var dt = sql.ObterTabela("MATERIAIS");
            Assert.NotNull(dt);
            Assert.Equal(3, dt.Rows.Count);
        }

        [Fact]
        public void ObterTabela_UnknownTable_ReturnsNull()
        {
            var sql = BuildSql();
            Assert.Null(sql.ObterTabela("TABELA_INEXISTENTE"));
        }
    }
}
