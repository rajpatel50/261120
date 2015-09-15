using System;
using System.Configuration;
using Xiap.Framework;
using Xiap.Framework.ProcessHandling;

namespace GeniusX.AXA.Underwriting.BusinessLogic
{
    /// <summary>
    /// Helper class for UW Business Logic plugins
    /// </summary>
    public static class UWBusinessLogicHelper
    {
        /// <summary>
        /// Gets the configuration item from the application configuration and raises an exception if it can't find it
        /// </summary>
        /// <typeparam name="T">Type of config type to return</typeparam>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>Configuration item</returns>
        /// <exception cref="System.InvalidOperationException">No configuration item found</exception>
        public static T ResolveMandatoryConfig<T>(string propertyName)
        {
            var value = ConfigurationManager.AppSettings[propertyName];
            if (value == null)
            {
                throw new InvalidOperationException(string.Format("Missing config property: {0}", propertyName));
            }

            return (T)Convert.ChangeType(value, typeof(T));
        }

        /// <summary>
        /// Adds the error passed in to the process results collection.
        /// </summary>
        /// <param name="processResults">The process results.</param>
        /// <param name="errorCode">The error code.</param>
        /// <param name="invocationPoint">The invocation point.</param>
        /// <param name="component">The component.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>Process Results Collection</returns>
        public static ProcessResultsCollection AddError(ProcessResultsCollection processResults, string errorCode, ProcessInvocationPoint invocationPoint, IBusinessComponent component, params object[] args)
        {
            ProcessResult vr = new ProcessResult(component, null, ErrorSeverity.Error, errorCode, args);
            processResults.Add(vr.Key, invocationPoint, vr);

            return processResults;
        }
    }
}
