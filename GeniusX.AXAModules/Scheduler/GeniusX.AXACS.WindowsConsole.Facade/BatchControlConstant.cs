
namespace GeniusX.AXACS.WindowsConsole.Facade
{
    public static class BatchControlConstant
    {
        public const string ADD_BATCH_ENTRY = @"INSERT INTO [Common].[BatchControlLog] ([BatchType], [BatchParameters], [BatchStatus], [RunDescription])"
                                                  + " VALUES (@batchType, @batchParameters, @batchStatus, @runDescription)"
                                                  + " SELECT CAST(scope_identity() AS int)";

        public const string UPDATE_BATCH_STATUS_RUN_START = @"UPDATE [Common].[BatchControlLog]"
                                                                + " SET [BatchStatus] = @batchStatus"
                                                                + " ,[CompletionStatus] = @completionStatus"
                                                                + " ,[RunStart] = @runTime"
                                                                + " WHERE [BatchControlLogID] = @batchControlLogId";


        public const string UPDATE_BATCH_STATUS_RUN_END = @"UPDATE [Common].[BatchControlLog]"
                                                              + " SET [BatchStatus] = @batchStatus"
                                                              + " ,[CompletionStatus] = @completionStatus"
                                                              + " ,[RunEnd] = @runTime"
                                                              + " WHERE [BatchControlLogID] = @batchControlLogId";

        public const string UPDATE_BATCH_STATUS = @"UPDATE [Common].[BatchControlLog]"
                                                                + " SET [BatchStatus] = @batchStatus"
                                                                + " ,[CompletionStatus] = @completionStatus"
                                                                + " WHERE [BatchControlLogID] = @batchControlLogId";
           

        public enum BatchType
        {
            DocumentProduction = 1,
            AutomaticRenewal = 2,
            Custom = 3
        }

        public enum BatchStatus
        {
            Unprocessed = 1,
            InProgress = 2,
            Completed = 3,
            Cancelled = 4
        }

        public enum CompletionStatus
        {
            Clear = 1,
            Warnings = 2,
            Errors = 3
        }

        public enum MessageLevel
        {            
            Warning = 1,         
            Error = 2
        }
    }
}
