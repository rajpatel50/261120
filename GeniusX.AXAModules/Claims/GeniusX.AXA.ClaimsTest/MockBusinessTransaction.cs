using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xiap.Framework.BusinessTransaction;
using Xiap.Framework;
using Xiap.Claims.BusinessTransaction;

namespace GeniusX.AXA.ClaimsTest
{
    /// <summary>
    /// Copied from XIAP as business transactions can't be mocked out using rhino mocks (which is another issue)
    /// </summary>
    public class MockBusinessTransaction : AbstractBusinessTransaction, IClaimsBusinessTransaction
    {
        #region Constructors
        public MockBusinessTransaction(TransactionContext transactionContext)
        {
            SetContext(transactionContext);
        }
        #endregion


        public string[] DoStartProperties { get; set; }


        Xiap.Claims.Data.IClaimsTransactionContext IClaimsBusinessTransaction.ClaimsContext
        {
            get { throw new NotImplementedException(); }
        }

        public System.Data.Objects.ObjectContext ObjectContext
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public override IBusinessComponent NotesComponent
        {
            get { throw new NotImplementedException(); }
        }

        #region IClaimsBusinessTransaction Members

        public Xiap.Claims.BusinessComponent.ClaimsTransactionContext ClaimsContext
        {
            get { throw new NotImplementedException(); }
        }

        #endregion

        public Xiap.Claims.BusinessComponent.ClaimHeader ClaimHeader
        {
            get { throw new NotImplementedException(); }
        }

        public override string Description
        {
            get { throw new NotImplementedException(); }
        }

        public override string SecurityToken
        {
            // TODO: Determine a suitable permission
            get { return "ID.BT.Name.Create"; }
        }

        // No component security
        public override string ComponentSecurityToken
        {
            get { return null; }
        }

        public Xiap.Claims.BusinessComponent.IClaimsQuery ClaimsQuery
        {
            get { throw new NotImplementedException(); }
        }

        public bool CanSaveAndContinue
        {
            get { throw new NotImplementedException(); }
        }

        #region Implemented Methods
        protected override void DoStart(string[] parameters)
        {
            this.DoStartProperties = parameters;
        }

        protected override void DoCanStart(string[] parameters)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Implemented Methods With No Processing
        protected override void DoValidate(Xiap.Framework.ValidationMode mode)
        {
            // No Validate processing required
        }

        protected override void DoComplete()
        {
            // No Complete processing required
        }

        protected override void DoSave()
        {
            // No Save processing required
        }

        protected override void DoPostCommit()
        {
            // No Save processing required
        }

        protected override void DoComponentLoad()
        {
        }

        protected override void DoPostCreate()
        {
        }
        
        public void SetNotesComponent(IBusinessComponent notesComponent)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Not Implemented Methods
        protected override void DoRollback()
        {
        }

        public override void DeleteComponent(Xiap.Framework.IBusinessComponent component, bool validateOnly)
        {
            throw new NotImplementedException();
        }

        protected override void ProcessBusinessData(Xiap.Framework.Data.BusinessTransactionContext context)
        {
            throw new NotImplementedException();
        }

        public override void ProcessBusinessData(Xiap.Framework.Data.BusinessData businessData)
        {
            throw new NotImplementedException();
        }

        protected override void Update(AbstractBusinessTransaction businessTransaction)
        {
            throw new NotImplementedException();
        }

        protected override Xiap.Framework.Data.BusinessTransactionContext GetBusinessData()
        {
            return new Xiap.Framework.Data.BusinessTransactionContext();
        }

        public override Xiap.Framework.Data.BusinessData GetData()
        {
            throw new NotImplementedException();
        }

        public override Xiap.Framework.Data.TransactionDefinition MetaDefinition()
        {
            throw new NotImplementedException();
        }

        public override Xiap.Framework.Data.BusinessData[] Retrieve(Xiap.Framework.Data.BusinessTransactionContext context, string[] componentPath)
        {
            throw new NotImplementedException();
        }
        #endregion      
    
        public void DeleteComponent(IBusinessComponent component, bool validateOnly, bool treatWarningAsErrors)
        {
            throw new NotImplementedException();
        }

        public Dictionary<Guid, Xiap.Framework.Entity.BusinessComponentBase> GetComponentList()
        {
            throw new NotImplementedException();
        }

        public List<Xiap.Framework.DataMapping.Payload> ProcessData(List<Xiap.Framework.DataMapping.Payload> requestedChanges, Xiap.Framework.DataMapping.VirtualProcessRequest virtualProcessRequest, ValidationLevel validationLevel, bool forceFullDataRetrieval, Action action, bool resumeTransaction, bool treatWarningsAsErrors = false)
        {
            throw new NotImplementedException();
        }

        public List<Xiap.Framework.DataMapping.Payload> ProcessData(List<Xiap.Framework.DataMapping.Payload> requestedChanges, Xiap.Framework.DataMapping.VirtualProcessRequest virtualProcessRequest, ValidationLevel validationLevel, bool forceFullDataRetrieval, Action action)
        {
            throw new NotImplementedException();
        }

        public List<Xiap.Framework.DataMapping.Payload> ProcessData(List<Xiap.Framework.DataMapping.Payload> requestedChanges, Xiap.Framework.DataMapping.VirtualProcessRequest virtualProcessRequest, ValidationLevel validationLevel, bool forceFullDataRetrieval)
        {
            throw new NotImplementedException();
        }

        public List<Xiap.Framework.DataMapping.Payload> ProcessData(List<Xiap.Framework.DataMapping.Payload> requestedChanges)
        {
            throw new NotImplementedException();
        }

        public void ReattachEntities()
        {
            throw new NotImplementedException();
        }

        public Xiap.Framework.Data.DtoBase Retrieve(IBusinessComponent componentToRetrieve, Xiap.Framework.Data.BusinessDataVariant dataGroup, Xiap.Framework.Entity.RetrievalType retrievalType, Xiap.Framework.Data.BusinessDataVariant childDataVariant, Xiap.Framework.Data.BusinessDataVariant latestVersionVariant, List<string> childNamesToRetrieve, VersionSelectionParameters versionSelectionParameters = null)
        {
            throw new NotImplementedException();
        }

        public Xiap.Framework.Data.DtoBase Retrieve(IBusinessComponent componentToRetrieve, Xiap.Framework.Data.BusinessDataVariant dataGroup, Xiap.Framework.Entity.RetrievalType retrievalType, Xiap.Framework.Data.BusinessDataVariant childDataVariant, Xiap.Framework.Data.BusinessDataVariant latestVersionVariant, List<string> childNamesToRetrieve)
        {
            throw new NotImplementedException();
        }

        public Xiap.Framework.Data.DtoBase Retrieve(Guid componentDataId, Xiap.Framework.Data.BusinessDataVariant dataGroup, Xiap.Framework.Entity.RetrievalType retrievalType, Xiap.Framework.Data.BusinessDataVariant childDataVariant, Xiap.Framework.Data.BusinessDataVariant latestVersionVariant, List<string> childNamesToRetrieve, VersionSelectionParameters versionSelectionParameters = null)
        {
            throw new NotImplementedException();
        }

        public List<Xiap.Framework.DataMapping.MetadataPayload> RetrieveMetadata(List<Guid> componentDataIds, bool includeLatestVersion)
        {
            throw new NotImplementedException();
        }


      
        public IBusinessComponent ReloadComponent(IBusinessComponent component)
        {
            throw new NotImplementedException();
        }
    }
}
