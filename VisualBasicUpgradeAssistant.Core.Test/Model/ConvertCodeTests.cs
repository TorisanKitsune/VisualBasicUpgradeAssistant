using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VisualBasicUpgradeAssistant.Core.DataClasses;

namespace VisualBasicUpgradeAssistant.Core.Model.Tests
{
    [TestClass()]
    public class ConvertCodeTests
    {
        [DataTestMethod]
        [DataRow("Public foo As String", "public", "String", "foo")]
        [DataRow("Public foo As New Bar", "public", "Bar", "new Bar()")]
        public void ClassStandardVarableTest(String line, String scope, String type, String name)
        {
            ConvertCode convertCode = new ConvertCode();

            Variable result = convertCode.ParseVariableDeclaration(line);
            Assert.AreEqual(name, result.Name);
            Assert.AreEqual(type, result.Type);
            Assert.AreEqual(scope, result.Scope);
            Assert.AreEqual(null, result.Comment);
        }
    }
}
