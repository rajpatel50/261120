using System;

namespace GeniusX.AXACS.WindowsConsole.Facade
{
    public static class ArgumentCheck
    { 
        /// <summary>
        /// checks if the arguments are null or empty
        /// </summary>
        /// <param name="argument"> argument to be tested</param>
        /// <param name="exceptionMessage"> exception message</param>
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
