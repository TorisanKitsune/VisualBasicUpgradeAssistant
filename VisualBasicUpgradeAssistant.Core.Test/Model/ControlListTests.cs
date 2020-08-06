using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VisualBasicUpgradeAssistant.Core.Model.Tests
{
    [TestClass]
    public class ControlListTests
    {
        // This is auto-populated on run, and assumed to be non-null
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void ReadDataTest()
        {
            // Setup initial
            String execPath = AppDomain.CurrentDomain.BaseDirectory;
            String jsonFullPath = Path.Combine(execPath, "Resources", "ControlList.json");
            FileInfo jsonPath = new FileInfo(jsonFullPath);
            Assert.IsTrue(jsonPath.Exists);

            // Read data testing
            ControlList result = ControlList.ReadData(jsonPath);
            Assert.IsTrue(result.Controls.Count > 30);
            Assert.IsTrue(result.Controls.All(x => !String.IsNullOrEmpty(x.VB6)), "VB6 Name");
            Assert.IsTrue(result.Controls.Any(x => !String.IsNullOrEmpty(x.CSharp)), "CSharp Name");
            Assert.IsTrue(result.Controls.Any(x => x.InvisibleAtRuntime == true), "InvisibleAtRuntime");
            Assert.IsTrue(result.Controls.Any(x => x.Unsupported == true), "Unsupported");
        }

        [TestMethod]
        public void WriteDataTest()
        {
            // Assumed to be null
            String outFilePath = Path.Combine(TestContext.TestDir, "TestControlList.json");
            FileInfo outFile = new FileInfo(outFilePath);
            Assert.IsTrue(outFile.Directory.Exists);

            ControlList controlList = new ControlList();
            controlList.Controls.Add(new DataClasses.Controltem
            {
                VB6 = "TestVB6",
                CSharp = "TestCSharp",
                InvisibleAtRuntime = true,
                Unsupported = true,
            });
            controlList.WriteData(outFile);

            Assert.IsTrue(outFile.Exists);
            String json = File.ReadAllText(outFile.FullName);
            StringAssert.Contains(json, "VB6");
            StringAssert.Contains(json, "TestVB6");
            StringAssert.Contains(json, "CSharp");
            StringAssert.Contains(json, "TestCSharp");
            StringAssert.Contains(json, "InvisibleAtRuntime");
            StringAssert.Contains(json, "Unsupported");
        }
    }
}
