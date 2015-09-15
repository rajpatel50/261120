using System;
using System.Linq;
using GeniusX.AXA.ScheduledTaskClient.ScheduledTaskXuber6;
using Xiap.Framework.Logging;

namespace GeniusX.AXA.ScheduledTaskClient
{
    public class Program
    {
        private static readonly ILogger _Logger = LoggerFactory.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        public static int Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    throw new InvalidOperationException("args must contain at least the taskName");
                }

                string taskName = args[0];
                string[] taskArgs = args.Skip(1).ToArray();

                return InvokeTask(taskName, taskArgs);
            }
            catch (Exception e)
            {
                _Logger.Error(e);
                return 1;
            }
        }

        private static int InvokeTask(string taskName, string[] args)
        {
            if (_Logger.IsDebugEnabled)
            {
                _Logger.Debug(string.Format("InvokeTask({0}, {1})", taskName, string.Join(",", args)));
            }
            
            ScheduledTaskServiceClient client = null;
            try
            {
                client = new ScheduledTaskServiceClient();
                ScheduledTaskResponse response = client.Invoke(taskName, args);
                return (response.Result == ScheduledTaskResult.Success) ? 0 : 1;
            }
            finally
            {
                if (client != null && client.State == System.ServiceModel.CommunicationState.Opened)
                {
                    client.Close();
                }
            }
        }
    }
}
