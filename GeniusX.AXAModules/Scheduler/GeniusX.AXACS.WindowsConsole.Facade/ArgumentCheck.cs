using System;

namespace GeniusX.AXACS.WindowsConsole.Facade
{
    public static class ArgumentCheck
    {
        public static void ArgumentNullOrEmptyCheck(object argument, string exceptionMessage)
        {
            if (argument is string)
            {
                if (string.IsNullOrEmpty(argument.ToString().Trim()))
                {
                    throw new ArgumentNullException(exceptionMessage,string.Empty);
                }
            }
            else
            {
                if (argument == null)
                {
                    throw new ArgumentNullException(exceptionMessage,string.Empty);
                }
            }
        }
    }
}
