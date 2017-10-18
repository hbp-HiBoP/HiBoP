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
        [TestMethod()]
        public void LoadTest_POSFileWithTabs()
        {
            // POS.
            Dictionary<int, List<int>> indexByCode = new Dictionary<int, List<int>>();
            indexByCode.Add(1, new List<int>() { 1, 4, 6, 11, 20, 23, 33, 39, 46, 48 });
            indexByCode.Add(2, new List<int>() { 2, 19, 27, 32, 47 });
            indexByCode.Add(3, new List<int>() { 3 });
            indexByCode.Add(4, new List<int>() { 5, 18, 25, 34, 45 });
            indexByCode.Add(5, new List<int>() { 7, 14, 21, 28, 35, 42, 49 });
            indexByCode.Add(6, new List<int>() { 8, 15, 22, 29, 36, 43, 50 });
            indexByCode.Add(7, new List<int>() { 9, 26, 37 });
            indexByCode.Add(8, new List<int>() { 10, 12, 16, 17, 24, 31, 38, 40, 41 });
            indexByCode.Add(9, new List<int>() { 13, 30, 44 });

            // Test.
            FileInfo fileInfo = new FileInfo("./.../.../Resources/POS/POSWithTabs.pos");
            POS result = POS.Load(fileInfo.FullName);

            foreach (var code in indexByCode.Keys)
            {
                Assert.IsTrue(indexByCode[code].SequenceEqual(result.GetIndexes(code)));
            }
        }
        [TestMethod()]
        public void SaveTest()
        {
            // POS.
            Dictionary<int, List<Tuple<int,int>>> indexByCode = new Dictionary<int, List<Tuple<int,int>>>();
            indexByCode.Add(1, new List<Tuple<int,int>>() { new Tuple<int,int>(1,0) , 4, 6, 11, 20, 23, 33, 39, 46, 48 });
            indexByCode.Add(2, new List<int>() { 2, 19, 27, 32, 47 });
            indexByCode.Add(3, new List<int>() { 3 });
            indexByCode.Add(4, new List<int>() { 5, 18, 25, 34, 45 });
            indexByCode.Add(5, new List<int>() { 7, 14, 21, 28, 35, 42, 49 });
            indexByCode.Add(6, new List<int>() { 8, 15, 22, 29, 36, 43, 50 });
            indexByCode.Add(7, new List<int>() { 9, 26, 37 });
            indexByCode.Add(8, new List<int>() { 10, 12, 16, 17, 24, 31, 38, 40, 41 });
            indexByCode.Add(9, new List<int>() { 13, 30, 44 });
            POS pos = new POS(indexByCode);
            pos.Save("./.../.../Results/POS/Result.pos");

            // Test.
            string model = new StreamReader("./.../.../Resources/POS/POSWithTabs.pos").ReadToEnd();
            string result = new StreamReader("./.../.../Results/POS/Result.pos").ReadToEnd();
            Assert.AreEqual(model, result);
        }

        [TestMethod()]
        public void GetIndexesTest_OneCode()
        {
            // POS.
            Dictionary<int, List<int>> indexByCode = new Dictionary<int, List<int>>();
            indexByCode.Add(1, new List<int>() { 1, 2, 3, 4, 5 });
            indexByCode.Add(2, new List<int>() { 6, 7, 8, 9, 10 });
            POS pos = new POS(indexByCode);

            // Test.
            List<int> indexes = pos.GetIndexes(1).ToList();
            Assert.AreEqual(1, indexes[0]);
            Assert.AreEqual(2, indexes[1]);
            Assert.AreEqual(3, indexes[2]);
            Assert.AreEqual(4, indexes[3]);
            Assert.AreEqual(5, indexes[4]);
        }
        [TestMethod()]
        public void GetIndexesTest_WrongCode()
        {
            // POS.
            Dictionary<int, List<int>> indexByCode = new Dictionary<int, List<int>>();
            indexByCode.Add(1, new List<int>() { 1, 2, 3, 4, 5 });
            indexByCode.Add(2, new List<int>() { 6, 7, 8, 9, 10 });
            POS pos = new POS(indexByCode, new Dictionary<int, List<int>>());

            // Test
            Assert.AreEqual(0,pos.GetIndexes(3).Count());
        }
        [TestMethod()]
        public void GetIndexesTest_MultipleCodes()
        {
            // POS.
            Dictionary<int, List<int>> indexByCode = new Dictionary<int, List<int>>();
            indexByCode.Add(1, new List<int>() { 1, 2, 3, 4, 5 });
            indexByCode.Add(2, new List<int>() { 6, 7, 8, 9, 10 });
            POS pos = new POS(indexByCode, new Dictionary<int, List<int>>());

            // Test.
            List<int> indexes = pos.GetIndexes(new int[] { 0, 1 }).ToList();
            Assert.AreEqual(1, indexes[0]);
            Assert.AreEqual(2, indexes[1]);
            Assert.AreEqual(3, indexes[2]);
            Assert.AreEqual(4, indexes[3]);
            Assert.AreEqual(5, indexes[4]);
        }

        [TestMethod()]
        public void IsCompatibleTest_OK()
        {
            // POS.
            Dictionary<int,List<int>> indexByCode = new Dictionary<int,List<int>>();
            indexByCode.Add(1, new List<int>() { 1, 2, 3, 4, 5 });
            indexByCode.Add(2, new List<int>() { 6, 7, 8, 9, 10 });
            POS pos = new POS(indexByCode, new Dictionary<int, List<int>>());

            // Protocol.
            Experience.Protocol.Protocol protocol = new Experience.Protocol.Protocol();
            Experience.Protocol.Bloc bloc1 = new Experience.Protocol.Bloc();
            Experience.Protocol.Bloc bloc2 = new Experience.Protocol.Bloc();
            bloc1.Events.Add(new Experience.Protocol.Event("Main", new int[] { 1, 10 },Experience.Protocol.Event.TypeEnum.Main));
            bloc2.Events.Add(new Experience.Protocol.Event("Main", new int[] { 2, 20 },Experience.Protocol.Event.TypeEnum.Main));
            protocol.Blocs.Add(bloc1);
            protocol.Blocs.Add(bloc2);
            Assert.IsTrue(pos.IsCompatible(protocol));
        }
        [TestMethod()]
        public void IsCompatibleTest_Wrong()
        {
            // POS.
            Dictionary<int, List<int>> indexByCode = new Dictionary<int, List<int>>();
            indexByCode.Add(1, new List<int>() { 1, 2, 3, 4, 5 });
            indexByCode.Add(2, new List<int>() { 6, 7, 8, 9, 10 });
            POS pos = new POS(indexByCode, new Dictionary<int, List<int>>());

            // Protocol.
            Experience.Protocol.Protocol protocol = new Experience.Protocol.Protocol();
            Experience.Protocol.Bloc bloc1 = new Experience.Protocol.Bloc();
            Experience.Protocol.Bloc bloc2 = new Experience.Protocol.Bloc();
            bloc1.Events.Add(new Experience.Protocol.Event("Main", new int[] { 1, 10 }, Experience.Protocol.Event.TypeEnum.Main));
            bloc2.Events.Add(new Experience.Protocol.Event("Main", new int[] { 3, 20 }, Experience.Protocol.Event.TypeEnum.Main));
            protocol.Blocs.Add(bloc1);
            protocol.Blocs.Add(bloc2);
            Assert.IsFalse(pos.IsCompatible(protocol));
        }
    }
}