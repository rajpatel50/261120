using GeniusX.AXA.Claims.BusinessLogic;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xiap.Claims.BusinessComponent;
using Xiap.Claims.BusinessLogic.AuthorityCheck;
using Xiap.Framework;
using Xiap.Framework.Caching;
using Xiap.Framework.Data.Claims;
using Xiap.Framework.Data.Underwriting;
using Xiap.Framework.Logging;
using Xiap.Framework.Messages;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.Data.Enums;
using Xiap.Testing.Utils;
using Xiap.Testing.Utils.Mocks;
using Xiap.UW.BusinessComponent;

namespace GeniusX.AXA.ClaimsTest
{
    [TestClass]
    public class PolicyAttachmentAndDateProcessingForClaimTransactionHeaderTest : ComponentPluginBaseTest<PolicyAttachmentAndDateProcessingForClaimTransactionHeader>
    {
        private ClaimTransactionHeader claimTransactionHeader;
        private void TestInitializeVariables(string headerStatusCode)
        {
            this.target = new PolicyAttachmentAndDateProcessingForClaimTransactionHeader();
            UWHeaderData uwHeaderData = new UWHeaderData() { HeaderReference = "XUK000512612MO", HeaderID = 2 };

            Builder<ClaimDetail> claimDetailBuilder = new BusinessComponentBuilder<ClaimDetail>();

            Builder<ClaimTransactionHeader> claimTransactionHeaderBuilder = new BusinessComponentBuilder<ClaimTransactionHeader>()
              .SetProperty(a => a.IsInProgress = true)
                      .SetProperty(a => a.ClaimTransactionSource = 1)
            .Add(new BusinessComponentBuilder<ClaimTransactionGroup>());

            new BusinessComponentBuilder<ClaimHeader>()
                .SetProperty(a => a.IsAutomaticDeductibleProcessingApplied = true)
                .SetProperty(a => a.UWHeader = uwHeaderData)
                .SetProperty(a => a.PolicyHeaderID = 2)
                .SetProperty(a => a.PolicyExternalDataSource = "GENIUS")
                  .AddRef(claimDetailBuilder)
                  .AddRef(claimTransactionHeaderBuilder).Build();
            this.claimTransactionHeader = claimTransactionHeaderBuilder.Current;

            UnityContainer container = new UnityContainer();
            IAXAClaimsQuery claimsQuery = MockRepository.GenerateStub<IAXAClaimsQuery>();
            var underwritingEntities = MockRepository.GenerateStub<IUnderwritingQuery>();

            claimsQuery.Stub(a => a.GetHeaderStatus(Arg<string>.Is.Anything)).Return(headerStatusCode);

            container.RegisterInstance<IUnderwritingQuery>(underwritingEntities);
            container.RegisterInstance<IAXAClaimsQuery>(claimsQuery);
            container.RegisterInstance<ILoggingWrapper>(new MockLoggingService());
            container.RegisterType<IMessageService, MessageService>();
            container.RegisterInstance<IXiapCache>(MockRepository.GenerateStub<IXiapCache>());
            ObjectFactory.Instance = new ObjectFactory(container);
        }

        [TestMethod]
        public void PreCreateValidationHasUnauthorisedReserveAndPayment()
        {
            this.TestInitializeVariables("OPV");
            this.claimTransactionHeader.ReserveAuthorisationStatus = (short?)StaticValues.ReserveAuthorisationStatus.ReserveUnauthorised;
            this.claimTransactionHeader.ClaimTransactionSource = (short?)StaticValues.ClaimTransactionSource.Reserve;
            ProcessInvocationPoint point = ProcessInvocationPoint.PreCreateValidation;
            ProcessResultsCollection result = target.ProcessComponent(this.claimTransactionHeader, point, 0, new ProcessParameters() { Alias = String.Empty, TransactionInvocationPoint = TransactionInvocationPoint.PostComplete });
            Assert.IsTrue(this.MessageIdExists(result, ClaimConstants.CLAIM_AMOUNT_UNAUTHORISED));
        }

        [TestMethod]
        public void PreCreateValidationWithErrorMessage()
        {
            this.TestInitializeVariables("CLM");
            this.claimTransactionHeader.ClaimTransactionSource = 6;
            ProcessInvocationPoint point = ProcessInvocationPoint.PreCreateValidation;
            ProcessResultsCollection result = target.ProcessComponent(this.claimTransactionHeader, point, 0, new ProcessParameters() { Alias = String.Empty, TransactionInvocationPoint = TransactionInvocationPoint.PostComplete });
            Assert.IsTrue(this.MessageIdExists(result, ClaimConstants.POLICYNOTVERIFIED_CLAIMTRANSACTION_NOTALLOWED));
        }


        [TestMethod]
        public void PreCreateValidation()
        {
            this.TestInitializeVariables("OPV");
            this.claimTransactionHeader.ReserveAuthorisationStatus = (short?)StaticValues.ReserveAuthorisationStatus.ReserveAuthorised;
            this.claimTransactionHeader.ClaimTransactionSource = (short?)StaticValues.ClaimTransactionSource.Reserve;
            ProcessInvocationPoint point = ProcessInvocationPoint.PreCreateValidation;
            ProcessResultsCollection result = target.ProcessComponent(this.claimTransactionHeader, point, 0, new ProcessParameters() { Alias = String.Empty, TransactionInvocationPoint = TransactionInvocationPoint.PostComplete });
            Assert.AreEqual(0,result.Results.Count);
        }

        private bool MessageIdExists(ProcessResultsCollection results, string messageId)
        {
            foreach (ProcessResult result in results.Results)
            {
                if (result.MessageId == messageId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
