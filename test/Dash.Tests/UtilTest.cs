using System;
using System.Collections.Generic;
using Web.App.BusinessLayer;
using Xunit;

namespace Dash.Tests
{
    public class UtilTest
    {
        [Fact]
        public void TestFindMatchTableWithoutPrefix()
        {
            var tableName = "PERSON";
            var sequenceList = new List<string>();
            sequenceList.Add("SEQ_TASK");
            sequenceList.Add("SEQ_PERSON");
            var seqTask = Util.FindBestMatch(tableName, sequenceList);
            Assert.Equal("SEQ_PERSON", seqTask);
        }

        [Fact]
        public void TestFindMatchTableWithPrefix()
        {
            var tableName = "SYS_PERSON";
            var sequenceList = new List<string>();
            sequenceList.Add("SEQ_TASK");
            sequenceList.Add("SEQ_PERSON");
            var seqTask = Util.FindBestMatch(tableName, sequenceList);
            Assert.Equal("SEQ_PERSON", seqTask);
        }
    }
}
