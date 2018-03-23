using System;
using System.Collections.Generic;
using System.Text;
using Web.App.BusinessLayer;
using Xunit;

namespace Dash.Tests
{
    public class PagerTest
    {
        [Fact]
        public void TestFindPagerStartPageZero()
        {
            var page = 0;
            var pagerStart = Util.FindPagerStart(page);
            Assert.Equal(1, pagerStart);
        }

        [Fact]
        public void TestFindPagerStartDiffPage1()
        {
            var page = 10;
            var pagerStart = Util.FindPagerStart(page);
            Assert.Equal(11, pagerStart);
        }

        [Fact]
        public void TestFindPagerStartDiffPage2()
        {
            var page = 23;
            var pagerStart = Util.FindPagerStart(page);
            Assert.Equal(21, pagerStart);
        }

        [Fact]
        public void TestFindPagerStartForFirstPage()
        {
            var pagerStart = Util.FindPagerStartForFirstPage();
            Assert.Equal(1, pagerStart);
        }

        [Fact]
        public void TestFindPagerStartForLastPage()
        {
            var pageCount = 23;
            var pagerStart = Util.FindPagerStartForLastPage(pageCount);
            Assert.Equal(21, pagerStart);

            pageCount = 21;
            pagerStart = Util.FindPagerStartForLastPage(pageCount);
            Assert.Equal(21, pagerStart);

            pageCount = 20;
            pagerStart = Util.FindPagerStartForLastPage(pageCount);
            Assert.Equal(11, pagerStart);
        }

        [Fact]
        public void TestFindPagerStartForPreviousPage()
        {
            var page = 23;
            var pagerStart = Util.FindPagerStartForPreviousPage(page);
            Assert.Equal(21, pagerStart);

            page = 21;
            pagerStart = Util.FindPagerStartForPreviousPage(page);
            Assert.Equal(21, pagerStart);

            page = 20;
            pagerStart = Util.FindPagerStartForPreviousPage(page);
            Assert.Equal(11, pagerStart);
        }

        [Fact]
        public void TestFindPagerStartForNextPage()
        {
            var page = 23;
            var pagerStart = Util.FindPagerStartForNextPage(page);
            Assert.Equal(21, pagerStart);

            page = 29;
            pagerStart = Util.FindPagerStartForNextPage(page);
            Assert.Equal(31, pagerStart);
        }
    }
}
