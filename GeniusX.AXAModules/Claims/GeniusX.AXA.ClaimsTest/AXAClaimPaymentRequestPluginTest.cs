using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xiap.Testing.Utils;
using GeniusX.AXA.Claims.BusinessLogic;
using Xiap.Claims.BusinessComponent;

namespace GeniusX.AXA.ClaimsTest
{
    [TestClass]
    public class AXAClaimPaymentRequestPluginTest : ComponentPluginBaseTest<AXAClaimPaymentRequestPlugin>
    {
        [TestMethod]
        public void AddresseeNameReferencePropertyChanged()
        {
            ClaimPaymentRequest claimPaymentRequest = new ClaimPaymentRequest();
            ClaimTransactionToPaymentRequestLink cttprl = new ClaimTransactionToPaymentRequestLink();
            cttprl.ClaimPaymentRequest = claimPaymentRequest;
            claimPaymentRequest.ClaimTransactionToPaymentRequestLink.ClaimTransactionHeader = new ClaimTransactionHeader();
            claimPaymentRequest.ClaimTransactionToPaymentRequestLink.ClaimTransactionHeader.AddresseeClaimNameInvolvement = new ClaimNameInvolvement();
            claimPaymentRequest.ClaimTransactionToPaymentRequestLink.ClaimTransactionHeader.AddresseeClaimNameInvolvement.CustomReference01 = "ABCD";

            AXAClaimPaymentRequestPlugin cprp = new AXAClaimPaymentRequestPlugin();
            cprp.PropertyChange(claimPaymentRequest, Xiap.Framework.ProcessInvocationPoint.PropertyChange, "AddresseeNameID", null, "ABCD", 0);

            Assert.AreEqual("ABCD", claimPaymentRequest.PaymentRequestReference);
        }
    }
}
