using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using SimhashLib;

namespace SimhashTests
{
    [TestClass]
    public class TestSimhashIndexMD5
    {
        private Dictionary<long, Simhash> objs = new Dictionary<long, Simhash>();
        private SimhashIndex index;
        private Dictionary<long, string> testData = new Dictionary<long, string>();
        [TestInitialize]
        public void SetUp()
        {
            testData.Add(1, "How are you? I Am fine. blar blar blar blar blar Thanks.");
            testData.Add(2, "How are you i am fine. blar blar blar blar blar than");
            testData.Add(3, "This is simhash test.");
            testData.Add(4, "How are you i am fine. blar blar blar blar blar thank1");

            foreach (var it in testData)
            {
                var simHash = new Simhash(hashingType: Simhash.HashingType.MD5);
                simHash.GenerateSimhash(it.Value);
                objs.Add(it.Key, simHash);

            }
            index = new SimhashIndex(objs: objs, k: 10);

        }

        [TestMethod]
        public void GetKeys()
        {
            Dictionary<long, string> testdata = new Dictionary<long, string>();
            testdata.Add(1, "How are you? I Am fine. blar blar blar blar blar Thanks.");

            Dictionary<long, Simhash> simHashObjs = new Dictionary<long, Simhash>();
            foreach (var it in testdata)
            {
                var simHash = new Simhash(hashingType: Simhash.HashingType.MD5);
                simHash.GenerateSimhash(it.Value);
                simHashObjs.Add(it.Key, simHash);
            }
            var simHashIndex = new SimhashIndex(objs: simHashObjs, k: 10);
            var listOfKeys = simHashIndex.GetKeys(simHashObjs[1]);

            Assert.IsTrue(listOfKeys.Count == 10);
            Assert.AreEqual("0-YA==", listOfKeys[0]);
            Assert.AreEqual("1-AQ==", listOfKeys[1]);
            Assert.AreEqual("2-Aw==", listOfKeys[2]);
            Assert.AreEqual("3-AQ==", listOfKeys[3]);
            Assert.AreEqual("4-Dw==", listOfKeys[4]);
            Assert.AreEqual("5-Kw==", listOfKeys[5]);
            Assert.AreEqual("6-Hg==", listOfKeys[6]);
            Assert.AreEqual("7-Aw==", listOfKeys[7]);
            Assert.AreEqual("8-Dg==", listOfKeys[8]);
            Assert.AreEqual("9-Mg==", listOfKeys[9]);
        }

        [TestMethod]
        public void GetNearDupHash()
        {
            var s1 = new Simhash(hashingType: Simhash.HashingType.MD5);
            s1.GenerateSimhash("How are you i am fine.ablar ablar xyz blar blar blar blar blar blar blar thank");
            var dups = index.GetNearDups(s1);
            Assert.AreEqual(3, dups.Count);

            var s2 = new Simhash(hashingType: Simhash.HashingType.MD5);
            s2.GenerateSimhash(testData[1]);
            index.Delete(1, s2);
            dups = index.GetNearDups(s1);
            Assert.AreEqual(2, dups.Count);

            var s3 = new Simhash(hashingType: Simhash.HashingType.MD5);
            s3.GenerateSimhash(testData[1]);
            index.Delete(1, s3);
            dups = index.GetNearDups(s1);
            Assert.AreEqual(2, dups.Count);

            var s4 = new Simhash(hashingType: Simhash.HashingType.MD5);
            s4.GenerateSimhash(testData[1]);
            index.Add(1, s4);
            dups = index.GetNearDups(s1);
            Assert.AreEqual(3, dups.Count);

            var s5 = new Simhash(hashingType: Simhash.HashingType.MD5);
            s5.GenerateSimhash(testData[1]);
            index.Add(1, s5);
            dups = index.GetNearDups(s1);
            Assert.AreEqual(3, dups.Count);
        }
    }
}
