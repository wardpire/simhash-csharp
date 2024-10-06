using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimhashLib;
using System.Collections.Generic;

namespace SimhashTests
{
    [TestClass]
    public class TestShingling
    {

        [TestMethod]
        public void Slide()
        {
            var shingling = new Shingling();
            List<string> pieces = shingling.slide("aaabbb", width: 4);
            //aaab, aabb, abbb
            Assert.AreEqual(3, pieces.Count);
        }

        [TestMethod]
        public void TokenizeWidthDefault()
        {
            var shingling = new Shingling();
            List<string> pieces = shingling.tokenize("aaabbb");
            //aaab, aabb, abbb
            Assert.AreEqual(3, pieces.Count);
        }
        [TestMethod]
        public void TokenizeWidthThree()
        {
            var shingling = new Shingling();
            List<string> pieces = shingling.tokenize("This is a test for really cool content. yeah! =)", width: 3);
            //thi, his, isi, sis, isa .. etc....
            Assert.AreEqual(33, pieces.Count);
        }
        [TestMethod]
        public void Clean()
        {
            var shingling = new Shingling();
            string cleaned = shingling.scrub("aaa bbb test test testing. happy time =-).");
            Assert.AreEqual("aaabbbtesttesttestinghappytime", cleaned);
        }
    }
}
