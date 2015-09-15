using GeniusX.AXA.Claims.BusinessLogic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.Data.Enums;
using Xiap.Testing.Utils;
namespace GeniusX.AXA.ClaimsTest
{
    [TestClass]
    public class AXAClaimDocumentPluginTest : NewComponentPluginBaseTest<AXAClaimDocumentPlugin>
    {
        private ClaimHeader claimHeader = null;

        [TestInitialize]
        public void InitTestData()
        {
            this.target = new AXAClaimDocumentPlugin();
           
           
               this.claimHeader = new BusinessComponentBuilder<ClaimHeader>()
                                       .SetProperty(a => a.ClaimHeaderID = 1)
                                       .SetProperty(a => a.ClaimReference = "XUK1029214MO")
                                       .SetProperty(a => a.ClaimProductVersionID = 1)
                                       .SetProperty(a => a.ClaimStage = (short)StaticValues.ClaimStage.Claim)
                                       .Add(new BusinessComponentBuilder<ClaimDocument>()
                                       .SetProperty(a => a.DocumentReference = "DR01")
                                       .SetProperty(a => a.DocumentGroupReference = "DGR01")
                                       .SetProperty(a => a.CustomCode08 = "1")
                                       .SetProperty(a => a.ProductDocumentID = 1)).Build();
        }

        [TestMethod]
        public void ProcessTransaction_ClaimDocumentToClaimEventMapping_MappedSuccessfully()
        {
            ClaimEvent claimEvent = new ClaimEvent { ProductEventID = 34 };
            ClaimDocument claimDocument = this.claimHeader.ClaimDocuments.First();
            ProcessResultsCollection results = this.target.ProcessComponent(claimDocument, ProcessInvocationPoint.Virtual, 0, new ProcessParameters { Parameters = new object[] { claimEvent }, TransactionInvocationPoint = TransactionInvocationPoint.Blank, Alias = "CreationEvent" });
            Assert.AreEqual(claimEvent.CustomReference01, claimDocument.DocumentReference);
            Assert.AreEqual(claimEvent.CustomReference02, claimDocument.DocumentGroupReference);
            Assert.AreEqual(claimEvent.CustomCode08, claimDocument.CustomCode08);
        }
    }
}
