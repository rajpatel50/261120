using System.Collections.Generic;
using Xiap.Framework;
using Xiap.Framework.Common;
using Xiap.Framework.Common.Task;
using Xiap.Framework.Logging;
using Xiap.Framework.Metadata;
using Xiap.Framework.Utils;
using Xiap.Framework.Validation;
using Xiap.Metadata.BusinessComponent;
using Xiap.Metadata.Data.Enums;
using Xiap.Framework.Security;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    public class TaskReassignEventQueue : ITaskReassignedNotifier
    {
        private static readonly ILogger _Logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Add Entry in Queue For all Reassign Task 
        /// </summary>
        /// <param name="taskReassignNotifierArgs">task Reassign Notifier Args</param>
        /// <returns>Result Collection</returns>
        public bool Invoke(List<TaskReassignNotifierArg> taskReassignNotifierArgs)
        {
            ArgumentCheck.ArgumentNullOrEmptyCheck(taskReassignNotifierArgs, "args");

            if (_Logger.IsDebugEnabled)
            {
                _Logger.Debug(string.Format("TaskReassignedEvent: Invoked with '{0}' item(s).", taskReassignNotifierArgs.Count));
            }

            using (var messageWriter = ObjectFactory.Resolve<IMessageWriter>())
            {
                // Start a new message writer object to write out to the message queue
                messageWriter.Open(MessageQueueSettings.Contract, MessageQueueSettings.SendService, MessageQueueSettings.ReceiveService);
                foreach (var taskReassignArg in taskReassignNotifierArgs)
                {
                    // Make an asynchronous call to the TaskReassignEvent invocation by building the message, then invoking via the MessageWriter
                    string message = SerialisationUtils.ToXmlString<AsynchronousInvocationMessage>(new AsynchronousInvocationMessage("TaskReassignNotification", this.GetParametersList(taskReassignArg).ToArray()));
                    // This will post the call to the Service Broker Queue on the DB AsycnhronousInvocationXiapMessageQueue
                    messageWriter.Send(MessageQueueSettings.MessageType, message);
                    
                    if (_Logger.IsDebugEnabled)
                    {
                        _Logger.Debug(string.Format("TaskReassignedEvent: Message - {0}", message));
                    }
                }
            }

            if (_Logger.IsDebugEnabled)
            {
                _Logger.Debug("TaskReassignedEvent: Finished");
            }

            return true;
        }

        /// <summary>
        /// Get the parameter list we require for the call to TaskReassignEvent
        /// </summary> 
        /// <param name="taskReassignNotifierArgs">task Reassign Notifier Args</param>
        /// <returns>Result Collection</returns>
        private List<InvocationArg> GetParametersList(TaskReassignNotifierArg taskReassignNotifierArgs)
        {
            List<InvocationArg> parameterList = new List<InvocationArg>();
            parameterList.Add(new InvocationArg("SerialNumber", taskReassignNotifierArgs.SerialNumber));
            parameterList.Add(new InvocationArg("EventDescription", taskReassignNotifierArgs.EventDescription));
            parameterList.Add(new InvocationArg("EventDueDate", taskReassignNotifierArgs.EventDueDate.Ticks.ToString()));

            // Get the original destination name depending on if it's a Role code or a specific user.
            if (taskReassignNotifierArgs.OriginalDestinationType == (short)StaticValues.DestinationType.Role)
            {
                parameterList.Add(new InvocationArg("OriginalDestinationName", SystemValueSetCache.GetCodeDescription(taskReassignNotifierArgs.OriginalDestination, SystemValueSetCodeEnum.Role, true)));
            }
            else
            {
                parameterList.Add(new InvocationArg("OriginalDestinationName", UserHelper.GetUserDescription(taskReassignNotifierArgs.OriginalDestination)));
            }

            parameterList.Add(new InvocationArg("UpdatedDestinationName", UserHelper.GetUserDescription(taskReassignNotifierArgs.UpdatedDestinationUser)));
            parameterList.Add(new InvocationArg("ActioningUserID", taskReassignNotifierArgs.ActionBy.ToString()));
            parameterList.Add(new InvocationArg("ActionDate",taskReassignNotifierArgs.ActionDate.ToString()));
            parameterList.Add(new InvocationArg("OriginalDestinationType", taskReassignNotifierArgs.OriginalDestinationType.ToString()));
            parameterList.Add(new InvocationArg("OriginalDestination", taskReassignNotifierArgs.OriginalDestination));
            parameterList.Add(new InvocationArg("UpdatedDestination", taskReassignNotifierArgs.UpdatedDestinationUser));

            return parameterList;
        }
    }
}
