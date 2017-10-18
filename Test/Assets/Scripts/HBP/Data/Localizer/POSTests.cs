using Microsoft.VisualStudio.TestTools.UnitTesting;
using HBP.Data.Localizer;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HBP.Data.Localizer.Tests
{
    [TestClass()]
    public class POSTests
    {
        POS m_POS;
        Dictionary<int, List<int>> m_DummyDictionnary;

        [TestInitialize]
        public void Initialize()
        {
            m_DummyDictionnary = GenerateDummyDictionnary();
            m_POS = new POS(m_DummyDictionnary);
        }
        [TestCleanup]
        public void CleanUp()
        {
            m_POS = null;
            m_DummyDictionnary = null;
        }
        [TestMethod()]
        public void LoadTest()
        {
            FileInfo directory = new FileInfo("./Resources/TestWithTabs.pos");
            Console.Write(directory.Exists);
            Console.Write(directory.FullName);
            Assert.Fail();
        }
        [TestMethod()]
        public void SaveTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void GetIndexesTest_OneCode()
        {
            foreach (var pair in m_DummyDictionnary)
            {
                List<int> indexes = m_POS.GetIndexes(pair.Key).ToList();
                Assert.AreEqual(pair.Value.Count, indexes.Count);
                for (int i = 0; i < pair.Value.Count; i++)
                {
                    Assert.AreEqual(pair.Value[i], indexes[i]);
                }
            }
        }
        [TestMethod()]
        public void GetIndexesTest_WrongCode()
        {
            Assert.AreEqual(0,m_POS.GetIndexes(int.MinValue).Count());
        }
        [TestMethod()]
        public void GetIndexesTest_MultipleCodes()
        {
            List<int> indexes = m_POS.GetIndexes(new int[] { 0, 1 }).ToList();
            Assert.AreEqual(0, indexes[0]);
            Assert.AreEqual(1, indexes[1]);
            Assert.AreEqual(100, indexes[2]);
            Assert.AreEqual(101, indexes[3]);
        }

        [TestMethod()]
        public void IsCompatibleTest()
        {
            Assert.Fail();
        }

        Dictionary<int,List<int>> GenerateDummyDictionnary()
        {
            Dictionary<int, List<int>> indexByCode = new Dictionary<int, List<int>>();
            Random random = new Random();
            for (int c = 0; c < 5; c++)
            {
                indexByCode.Add(c, new List<int>());
                for (int i = 0; i < 2; i++)
                {
                    indexByCode[c].Add(100 * c + i);
                }
            }
            return indexByCode;
        }
    }
}