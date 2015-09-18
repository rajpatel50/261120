using GeniusX.AXA.Claims.BusinessLogic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xiap.Testing.Utils;
using Rhino.Mocks;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework.ProcessHandling;
using Xiap.Framework.Data.Underwriting;

namespace GeniusX.AXA.ClaimsTest
{
	[TestClass]
	public class UKClaimsExternalPolicyVerificationTest : NewComponentPluginBaseTest<UKClaimsExternalPolicyVerification>
	{
		private ClaimHeader component;

		[TestInitialize]
		public void TestIniitalise()
		{
			this.target = new UKClaimsExternalPolicyVerification();
			this.component = new ClaimHeader();

			IAXAClaimsQuery axaClaimQuery = this.StubAndRegister<IAXAClaimsQuery>();

			// status from app.config
			axaClaimQuery.Stub(a => a.GetHeaderStatus("VALID")).Return("OPV");
			axaClaimQuery.Stub(a => a.GetHeaderStatus("INVALID")).Return("INVALID");
		}


		[TestMethod]
		public void ProcessComponent_ParametersNull_ErrorAttachmentNotAllowed()
		{
			var result = this.target.ProcessComponent(this.component, Xiap.Framework.ProcessInvocationPoint.Virtual, 0, new ProcessParameters());

			AssertEx.ContainsMessage(result, ClaimConstants.ATTACHMENT_NOT_ALLOWED);
		}

		[TestMethod]
		public void ProcessComponent_NoParameters_ErrorAttachmentNotAllowed()
		{
			var result = this.target.ProcessComponent(this.component, Xiap.Framework.ProcessInvocationPoint.Virtual, 0, new ProcessParameters { Parameters = new object[] { } });

			AssertEx.ContainsMessage(result, ClaimConstants.ATTACHMENT_NOT_ALLOWED);
		}

		[TestMethod]
		public void ProcessComponent_NullParameter_ErrorAttachmentNotAllowed()
		{
			var parameters = new ProcessParameters  { Parameters = new object[] { null } };

			var result = this.target.ProcessComponent(this.component, Xiap.Framework.ProcessInvocationPoint.Virtual, 0, parameters);

			AssertEx.ContainsMessage(result, ClaimConstants.ATTACHMENT_NOT_ALLOWED);
		}

		[TestMethod]
		public void ProcessComponent_ParameterNotUWHeaderData_ErrorAttachmentNotAllowed()
		{
			var parameters = new ProcessParameters  { Parameters = new object[] { "Hello" } };

			var result = this.target.ProcessComponent(this.component, Xiap.Framework.ProcessInvocationPoint.Virtual, 0, parameters);

			AssertEx.ContainsMessage(result, ClaimConstants.ATTACHMENT_NOT_ALLOWED);
		}

		[TestMethod]
		public void ProcessComponent_NoHeaderReference_ErrorAttachmentNotAllowed()
		{
			var parameters = new ProcessParameters  { Parameters = new object[] { new UWHeaderData() } };

			var result = this.target.ProcessComponent(this.component, Xiap.Framework.ProcessInvocationPoint.Virtual, 0, parameters);

			AssertEx.ContainsMessage(result, ClaimConstants.ATTACHMENT_NOT_ALLOWED);
		}

		[TestMethod]
		public void ProcessComponent_WithHeaderReference_NoErrorAttachmentNotAllowed()
		{
			var parameters = new ProcessParameters { Parameters = new object[] { new UWHeaderData { HeaderReference = "INVALID", HeaderStatusCode = "INVALID" } } };

			var result = this.target.ProcessComponent(this.component, Xiap.Framework.ProcessInvocationPoint.Virtual, 0, parameters);

			AssertEx.DoesNotContainMessage(result, ClaimConstants.ATTACHMENT_NOT_ALLOWED);
		}

		[TestMethod]
		public void ProcessComponent_InvalidStatus_ErrorNotAuthorised()
		{
			var parameters = new ProcessParameters { Parameters = new object[] { new UWHeaderData { HeaderReference = "INVALID", HeaderStatusCode = "INVALID" } } };

			var result = this.target.ProcessComponent(this.component, Xiap.Framework.ProcessInvocationPoint.Virtual, 0, parameters);

			AssertEx.ContainsMessage(result, ClaimConstants.POLICYNOTVERIFIED_COVERAGEVERIFICATION_NOTALLOWED);
		}

		[TestMethod]
		public void ProcessComponent_ValidStatus_NoErrorReturned()
		{
			var parameters = new ProcessParameters { Parameters = new object[] { new UWHeaderData { HeaderReference = "VALID", HeaderStatusCode = "OPV" } } };

			var result = this.target.ProcessComponent(this.component, Xiap.Framework.ProcessInvocationPoint.Virtual, 0, parameters);

			AssertEx.DoesNotContainMessage(result, ClaimConstants.ATTACHMENT_NOT_ALLOWED);
			AssertEx.DoesNotContainMessage(result, ClaimConstants.POLICYNOTVERIFIED_COVERAGEVERIFICATION_NOTALLOWED);
		}
	}
}
