using System;
using Xiap.Framework;
using Xiap.Framework.Locking;
using Xiap.Framework.ProcessHandling;
using Xiap.InsuranceDirectory.BusinessComponent;
using Xiap.InsuranceDirectory.BusinessLogic;

namespace GeniusX.AXA.InsuranceDirectory.BusinessLogic
{
    public class AXANameReferenceDefaulterPlugin : AbstractComponentPlugin
    {
        /// <summary>
        /// Process runs on Create or Copy of a name.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <param name="point">The point.</param>
        /// <param name="pluginId">The plugin identifier.</param>
        /// <returns>Process Results Collection</returns>
        public override ProcessResultsCollection ProcessComponent(IBusinessComponent component, ProcessInvocationPoint point, int pluginId)
        {
            ProcessResultsCollection results = new ProcessResultsCollection();
            TransactionContext transactionContext = component.Context;
            switch (point)
            {
                case ProcessInvocationPoint.Created:
                    {
                        // Run for create or copy of any name usage that is defined as controlled in GeniusX
                        if ((transactionContext.TransactionType == TransactionProcessConstants.CreateName || transactionContext.TransactionType == TransactionProcessConstants.CopyName)
                            && (component as NameUsage).GetDefinitionComponent().CustomCode01 == IDConstants.NAME_CONTROLLED_IN_GENIUSX)
                        {
                            Name name = component.Parent as Name;
                            this.DefaultNameRefrence(results, component, point, name);
                        }

                        break;
                    }

                case ProcessInvocationPoint.Copy:
                    {
                        // Run for copy of any name usage that is defined as controlled in GeniusX
                        if ((transactionContext.TransactionType == TransactionProcessConstants.CopyName)
                            && (component as NameUsage).GetDefinitionComponent().CustomCode01 == IDConstants.NAME_CONTROLLED_IN_GENIUSX)
                        {
                            Name name = component.Context.CopyDictionary[component.Parent.DataId] as Name;
                            this.DefaultNameRefrence(results, component, point, name);
                        }

                        break;
                    }
            }

            return results;
        }

        /// <summary>
        /// Defaults the name refrence.
        /// </summary>
        /// <param name="results">The results.</param>
        /// <param name="component">The component.</param>
        /// <param name="point">The point.</param>
        /// <param name="name">The name.</param>
        private void DefaultNameRefrence(ProcessResultsCollection results, IBusinessComponent component, ProcessInvocationPoint point, Name name)
        {
            if (name != null)
            {
                // We have a valid name
                if (String.IsNullOrEmpty(name.NameReference))
                {
                    // The NameReference is not yet set so first get a Name Code defaulted
                    // this should default the name reference.
                    this.DefaultNameCode(name);
                    // Check the namereference is now set. If not, raise an error to say we couldn't lock the Name Reference and this is why it wasn't updated.
                    if (!String.IsNullOrEmpty(name.NameReference))
                    {
                        if (!LockManager.UpdateReferenceLocks(component.Context.TransactionId, name.NameReference, LockLevel.NameReference, LockType.Update, LockDurationType.Transaction, LockOrigin.InsuranceDirectory, string.Empty))
                        {
                            InsuranceDirectoryHelper.SetProcessResult(results, component, ProcessInvocationPoint.Instantiation, "CREATE_LOCK_FAILED");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This method generates and defaults a new name reference 
        /// The format used is nnnnnnn for  Names
        /// </summary>   
        /// <param name="name">the Name </param>
        private void DefaultNameCode(Name name)
        {
            string nameReferenceSequence = LockManager.AllocateReference(string.Empty, ReferenceType.NameReference, string.Empty, "90000000", 8, "99999999", false);
            name.NameReference = nameReferenceSequence;
        }
    }
}
