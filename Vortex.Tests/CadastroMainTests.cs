using Moq;
using System;
using System.Data;
using System.Data.SqlClient;
using Vortex.Addin.PartData.Core;
using Xunit;

namespace Vortex.Tests
{
    public class SQLCommandsTests
    {

        [Fact]
        public void TestConnect()
        {
            var primeService = new SQLCommands();
            bool result = primeService.oncon();
            Assert.False(result, "1 should not be prime");
        }
    }
}
