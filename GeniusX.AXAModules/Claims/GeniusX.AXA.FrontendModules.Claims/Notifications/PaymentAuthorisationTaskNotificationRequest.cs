using System;
using System.Collections.ObjectModel;
using GeniusX.AXA.FrontendModules.Claims.Resources;
using Xiap.Framework.Metadata;
using XIAP.Frontend.CoreControls;
using XIAP.Frontend.Infrastructure.Notifications;
using XIAP.FrontendModules.Common.ClaimService;
using XIAP.FrontendModules.Tasks.Service;

namespace GeniusX.AXA.FrontendModules.Claims.Notifications
{
    public class PaymentAuthorisationTaskNotificationRequest : NotificationRequest
    {
        private ITaskServiceHelper client;
        private int attemptCounter = 0;
        private int notificationResultAttempt;
        private string claimReference;
        private long claimTransactionHeaderID;
        private ClaimPaymentRequestData paymentRequest;

        public PaymentAuthorisationTaskNotificationRequest(ITaskServiceHelper client, long claimTransactionHeaderID, int notificationAttempt, string claimReference, ClaimPaymentRequestData claimPaymentRequest)
        {
            this.client = client;
            this.claimReference = claimReference;
            this.claimTransactionHeaderID = claimTransactionHeaderID;
            this.notificationResultAttempt = notificationAttempt;
            this.paymentRequest = claimPaymentRequest;
        }

        public override void CheckForNotification(Action<bool> notificationTriggered)
        {
            this.client.GetEventDestinationsByLinkedComponent(SystemComponentConstants.ClaimTransaction,
                this.claimTransactionHeaderID,
                (names) =>
                {
                    this.EventDestinationsResponse(notificationTriggered, names);
                },
                new XIAP.FrontendModules.Common.HandleAsyncError(
                    (o) =>
                    {
                        notificationTriggered(false);
                        return false;
                    }));
        }

        private void EventDestinationsResponse(Action<bool> notificationTriggered, ObservableCollection<string> names)
        {
            if (names.Count > 0)
            {
                notificationTriggered(true);
                decimal paymentAmountOriginal = 0;
                string originalCurrencyCode = string.Empty;
                string payeeNameDescription = string.Empty;

                if (this.paymentRequest != null)
                {
                    paymentAmountOriginal = this.paymentRequest.PaymentAmountOriginal.Value * -1;
                    originalCurrencyCode = this.paymentRequest.OriginalCurrencyCode;
                    payeeNameDescription = this.paymentRequest.PayeeNameDescription;
                }

                string destinations = string.Join(",", names);
                XIAPMessageBox.Show(string.Format(StringResources.Notification_AuthenticationTaskTitle, this.claimReference),
                    string.Format(StringResources.Notification_AuthenticationTaskMessage,
                                    this.claimReference,
                                    paymentAmountOriginal,
                                    originalCurrencyCode,
                                    payeeNameDescription,
                                    destinations),
                    XIAPMessageBox.Buttons.OK,
                    XIAPMessageBox.Icons.Information,
                    null);
            }
            else
            {
                this.attemptCounter++;
                if (this.attemptCounter >= this.notificationResultAttempt)
                {
                    notificationTriggered(true);
                }
                else
                {
                    notificationTriggered(false);
                }
            }
        }
    }
}
