using System;
using Xiap.Claims.BusinessComponent;
using Xiap.Framework;
using Xiap.Framework.Data.InsuranceDirectory;
using Xiap.Framework.ProcessHandling;
using Xiap.Metadata.Data.Enums;

namespace GeniusX.AXA.Claims.BusinessLogic
{
    /// <summary>
    /// For the Driver Name involvement, if the Apply Driver Access setting is true (CustomBoolean04), it sets the Excess value on the 
    /// Claim Header (CustomNumberic10) from the Driver Excess value on the Driver Name Involvement (CustomNumeric10), 
    /// taking into account the age of the driver.
    /// It also sets Driver Excess Applied? And Apply Driver Excess? Settings accordingly 
    /// (CustomBoolean03 and CustomBoolean04 respectively) on the Driver Name Involvement.
    /// </summary>
    public class DriverExcessValidationPlugin : AbstractComponentPlugin
    {
        /// <summary>
        /// Starting point of Plugin invocation 
        /// </summary>
        /// <param name="component">IBusiness Component</param>
        /// <param name="point">Process Invocation Point</param>
        /// <param name="pluginId">Plugin ID</param>
        /// <returns>Process Results Collection </returns>
        public override ProcessResultsCollection ProcessComponent(Xiap.Framework.IBusinessComponent component, Xiap.Framework.ProcessInvocationPoint point, int pluginId)
        {
            PluginHelper<IBusinessComponent> pluginHelper = new PluginHelper<IBusinessComponent>(point, (ClaimHeader)component, new ProcessResultsCollection());

            switch (pluginHelper.InvocationPoint)
            {
                case ProcessInvocationPoint.PreValidationDefaulting:
                    {
                        this.UpdateDriverExcessAmount(pluginHelper);
                        break;
                    }
            }

            return pluginHelper.ProcessResults;
        }

        /// <summary>
        /// Update Driver Excess Amount
        /// </summary>
        /// <param name="pluginHelper">pluginH elper</param>
        private void UpdateDriverExcessAmount(PluginHelper<IBusinessComponent> pluginHelper)
        {
            ClaimHeader claimHeader = pluginHelper.Component as ClaimHeader;

            // Get the current Driver Name Involvement on the Claim
            var driverNameInvolvement = ClaimsProcessHelper.GetLatestNameInvolvement(claimHeader, StaticValues.NameInvolvementType.Driver);

            if (driverNameInvolvement != null)
            {
                // Cast our driverNameInvolvement to an object
                ClaimNameInvolvement objDriverNameInvolvement = driverNameInvolvement as ClaimNameInvolvement;

                // If we have a driver NI, and 'Apply Driver Excess?' is true (CustomBoolean04)
                // and it's been changed to true in this update OR we have changed the amount of excess for the driver 
                // entered against the Name Involvement (CustomNumeric10)
                if (objDriverNameInvolvement != null && objDriverNameInvolvement.CustomBoolean04 == true && (objDriverNameInvolvement.DirtyPropertyList.ContainsKey(ClaimConstants.CUSTOM_BOOLEAN_04) || objDriverNameInvolvement.DirtyPropertyList.ContainsKey(ClaimConstants.CUSTOM_NUMERIC_10)))   
                {
                    DateTime? driverDOB;
                    // If claim header has 'Large Loss' (CustomBoolean03) unchecked AND the Driver has a value against excess amount
                    // AND the driver's date of birth is entered AND we haven't applied driver XS already (CustomBoolean03)?
                    if (claimHeader.CustomBoolean03.GetValueOrDefault(false) == false   
                    && objDriverNameInvolvement.CustomNumeric10.GetValueOrDefault() != 0   
                    && this.IsDriverDateOfBirthEmpty(objDriverNameInvolvement, out driverDOB) == false
                    && objDriverNameInvolvement.CustomBoolean03 != true)   
                    {
                        // Was the driver under 26 when the date of loss occurred?
                        if (this.DriverAgeDifferenceLessThan26(driverDOB.Value, claimHeader.DateOfLossFrom.Value) == true)
                        {
                            // If the 'excess' value on the ClaimHeader is not yet set, set it from the Driver Name Involvement excess value.
                            // Else, add the current driver's excess value to the claim header value.
                            if (claimHeader.CustomNumeric10 == null)   
                            {
                                claimHeader.CustomNumeric10 = objDriverNameInvolvement.CustomNumeric10.Value;   // UI Label = Excess
                            }
                            else
                            {
                                claimHeader.CustomNumeric10 += objDriverNameInvolvement.CustomNumeric10.Value;   // UI Label = Excess
                            }

                            // Note we've applied the excess for this driver.
                            objDriverNameInvolvement.CustomBoolean03 = true;   // UI Label = Driver XS Applied?
                        }
                        else
                        {
                            // Don't apply the driver excess
                            objDriverNameInvolvement.CustomBoolean04 = false;   // UI Label = Apply Driver Excess?
                        }
                    }
                    else
                    {
                        // Don't apply the driver excess
                        objDriverNameInvolvement.CustomBoolean04 = false;   // UI Label = Apply Driver Excess?
                    }
                }
            }
        }

        /// <summary>
        /// Check Driver Date of birth. Return it via the output parameter if found
        /// </summary>
        /// <param name="driverNameInvolvement">Claim Name Involvement</param>
        /// <param name="driverDOB">output Date Of Birth</param>
        /// <returns>True /False </returns>
        private bool IsDriverDateOfBirthEmpty(ClaimNameInvolvement driverNameInvolvement, out DateTime? driverDOB)
        {
            if (driverNameInvolvement.NameID.GetValueOrDefault(0) == 0)
            {
                driverDOB = null;
                return true;
            }

            IInsuranceDirectoryService ids = ObjectFactory.Resolve<IInsuranceDirectoryService>();

            IPerson person = ids.GetPersonForName(driverNameInvolvement.NameID.Value, DateTime.Now);

            if (person != null && person.DateOfBirth != null)
            {
                driverDOB = person.DateOfBirth.Value;
                return false;
            }

            driverDOB = null;
            return true;
        }
        /// <summary>
        /// Check Driver Age was less than 26 compared on the DateOfLossFrom date
        /// If there isn't a Date of Loss From value, use the current date.
        /// </summary>
        /// <param name="driverDOB">Driver DOB</param>
        /// <param name="dateOfLossFrom">Date Of Loss From</param>
        /// <returns>True if driver was younger than 26 at Date of Loss From date, else returnes false</returns>
        public bool DriverAgeDifferenceLessThan26(DateTime driverDOB, DateTime dateOfLossFrom)
        {
            double ageInYears;
            DateTime dt = new DateTime();

            if (dateOfLossFrom == null)
            {
                dateOfLossFrom = DateTime.Now;
            }

            ageInYears = dt.Years(driverDOB, dateOfLossFrom);

            if (ageInYears < 26)
            {
                return true;
            }

            return false;
        }
    }
}
