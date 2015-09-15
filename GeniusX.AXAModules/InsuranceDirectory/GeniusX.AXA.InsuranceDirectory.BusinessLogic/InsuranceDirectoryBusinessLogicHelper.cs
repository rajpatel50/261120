using System;
using System.Configuration;
using Xiap.Framework.Security;

namespace GeniusX.AXA.InsuranceDirectory.BusinessLogic
{
    public static class InsuranceDirectoryBusinessLogicHelper
    {
        /// <summary>
        /// Retrieves the item from the Application Configuration or throws an exception if not found.
        /// </summary>
        /// <typeparam name="T">Type of configuration item to return</typeparam>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>Configuration item</returns>
        /// <exception cref="System.InvalidOperationException"></exception>
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
        /// Finds the required permission token from the application configuration and then checks the current user
        /// has permission to access that token.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        public static void VerifyPermissionForCurrentUser(string propertyName)
        {
            string permissionToken = InsuranceDirectoryBusinessLogicHelper.ResolveMandatoryConfig<string>(propertyName);
            XiapSecurity.Assert(permissionToken);
        }
    }
}
