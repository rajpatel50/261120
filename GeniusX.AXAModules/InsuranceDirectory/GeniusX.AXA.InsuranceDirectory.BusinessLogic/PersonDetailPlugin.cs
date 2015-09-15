using System;
using Xiap.Framework;
using Xiap.Framework.Entity;
using Xiap.Framework.ProcessHandling;
using Xiap.InsuranceDirectory.BusinessComponent;

namespace GeniusX.AXA.InsuranceDirectory.BusinessLogic
{
    /// <summary>
    /// When a Person is being created or coped—Create Name or Copy Name transactions—sets the ListName on the 
    /// PersonDetailVersion.
    /// </summary> 
    public class PersonDetailPlugin : AbstractComponentPlugin
    {
        /// <summary>
        /// Process during PreValidation Defaulting for create or copy of a person.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <param name="point">The point.</param>
        /// <param name="pluginId">The plugin identifier.</param>
        /// <returns>Process Results Collection</returns>
        public override ProcessResultsCollection ProcessComponent(IBusinessComponent component, ProcessInvocationPoint point, int pluginId)
        {
            PluginHelper<PersonDetailVersion> pluginHelper = new PluginHelper<PersonDetailVersion>(point, (PersonDetailVersion)component, new ProcessResultsCollection());
            TransactionContext transactionContext = component.Context;
            PersonDetailVersion person = (PersonDetailVersion)component;
            switch (point)
            {
                case ProcessInvocationPoint.PreValidationDefaulting:
                    {
                        // Only validate if we're doing a Create or Copy of the person
                        if (transactionContext.TransactionType == TransactionProcessConstants.CreateName || transactionContext.TransactionType == TransactionProcessConstants.CopyName)
                        {
                            this.PersonValidation(pluginHelper.ProcessResults, person, point);
                        }

                        break;
                    }
            }

            return pluginHelper.ProcessResults;
        }

        /// <summary>
        /// Validates the person has a title and then uses this in the list name creation.
        /// Otherwise, list name is just a concatenation of forename and surname.
        /// </summary>
        /// <param name="results">The results.</param>
        /// <param name="personDetailVersion">The person detail version.</param>
        /// <param name="point">The point.</param>
        public void PersonValidation(ProcessResultsCollection results, PersonDetailVersion personDetailVersion, ProcessInvocationPoint point)
        {
            string title = string.Empty;
            if (!string.IsNullOrEmpty(personDetailVersion.TitleCode))
            {
                // Default the title as a string from the selected Title code on the Person detail
                title = FieldsHelper.GetCodeDescription(personDetailVersion.TitleCodeField, personDetailVersion.TitleCode);
            }
            
            if (!string.IsNullOrWhiteSpace(title))
            {
                // We have a title, so format the list name to include the title at the start.
                string listName = String.Format("{0} {1}", title, personDetailVersion.Forename).Trim();
                personDetailVersion.ListName = String.Format("{0} {1}", listName, personDetailVersion.Surname).Trim();
            }
            else 
            {
                // Otherwise, the list name is just forename surname.
                personDetailVersion.ListName = String.Format("{0} {1}", personDetailVersion.Forename, personDetailVersion.Surname).Trim();
            }
        }
    }
}
