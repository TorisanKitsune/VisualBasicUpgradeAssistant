using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VisualBasicUpgradeAssistant.Core.DataClasses;

namespace VisualBasicUpgradeAssistant.Core.Model.Tests
{
    [TestClass()]
    public class ConvertCodeTests
    {
        [TestMethod]
        public void ClassStandardVarableTest()
        {
            ConvertCode convertCode = new ConvertCode();

            String line = "Public foo As String";
            Variable result = convertCode.ParseVariableDeclaration(line);
            Assert.AreEqual("foo", result.Name);
            Assert.AreEqual("String", result.Type);
            Assert.AreEqual("public", result.Scope);
            Assert.AreEqual(null, result.Comment);
        }

        [TestMethod]
        public void ClassNewVariableTest()
        {
            ConvertCode convertCode = new ConvertCode();

            String line = "Public foo As New Bar";
            Variable result = convertCode.ParseVariableDeclaration(line);
            Assert.AreEqual("foo", result.Name);
            Assert.AreEqual("new Bar()", result.Type);
            Assert.AreEqual("public", result.Scope);
            Assert.AreEqual(null, result.Comment);
        }
    }
}
