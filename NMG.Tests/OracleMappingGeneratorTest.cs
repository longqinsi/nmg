﻿using System.Xml;
using NMG.Core;
using NMG.Core.Domain;
using NUnit.Framework;

namespace NMG.Tests
{
    [TestFixture]
    public class OracleMappingGeneratorTest
    {
        [Test]
        public void ShouldGenerateMappingForOracleTable()
        {
            const string generatedXML = "<?xml version=\"1.0\" encoding=\"utf-8\"?><hibernate-mapping assembly=\"myAssemblyName\" xmlns=\"urn:nhibernate-mapping-2.2\"><class name=\"myNameSpace.Customer, myAssemblyName\" table=\"Customer\" lazy=\"true\" xmlns=\"\" /></hibernate-mapping>";
            var generator = new OracleMappingGenerator("\\", "Customer", "myNameSpace", "myAssemblyName", "mySequenceName",new ColumnDetails());
            XmlDocument document = generator.CreateMappingDocument();
            Assert.AreEqual(generatedXML, document.InnerXml);
        }
    }
}
