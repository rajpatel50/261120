using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Practices.Unity;
using Xiap.Framework.Data;
using XIAP.Frontend.Infrastructure;
using XIAP.Frontend.Infrastructure.Tree;
using XIAP.FrontendModules.Infrastructure.NavTree;
using XIAP.FrontendModules.Common.ClaimService;
using System.Linq;
using GeniusX.AXA.FrontendModules.Claims.Model;
using Xiap.Metadata.Data.Enums;

namespace GeniusX.AXA.FrontendModules.Claims.Navigation
{
    public class AXAClaimNameInvolvementGroupNodeBehaviour : INodeCreationBehaviour, INodeChildrenLoadingBehaviour
    {
        private StaticValues.NameInvolvementType_ClaimNameInvolvement claimNameInvolvementType;

        [Dependency]
        public StaticValues.NameInvolvementType_ClaimNameInvolvement ClaimNameInvolvementType
        {
            get
            {
                return this.claimNameInvolvementType;
            }

            set
            {
                this.claimNameInvolvementType = value;
            }
        }

        public IEnumerable<TreeNodeData<ActionKey>> CreateNodes(ITransactionController transactionController, TreeStructureStore definition, DtoBase parentDto)
        {
            return this.CreateNodesForDefinition(transactionController, definition, parentDto);
        }

        public void LoadChildren(ITransactionController transactionController, TreeNodeData<ActionKey> node)
        {
            node.IsLoaded = true;
        }

        private ObservableCollection<TreeNodeData<ActionKey>> CreateNodesForDefinition(ITransactionController transactionController, TreeStructureStore definition, DtoBase dto)
        {
            AXAClaimModel model = (AXAClaimModel)transactionController.Model;
            ObservableCollection<TreeNodeData<ActionKey>> nodes = new ObservableCollection<TreeNodeData<ActionKey>>();
            INodeAvailabilityBehaviour nodeAvailabilityBehavior = transactionController.Container.Resolve<INodeAvailabilityBehaviour>(definition.AvailabilityBehaviour);
            if (nodeAvailabilityBehavior.IsAvailable(transactionController, definition, dto))
            {
                var groupDefinition = definition.Clone();
                groupDefinition.Parent = definition.Parent;
                var groupingNode = transactionController.CreateNavigationData(groupDefinition, dto);
                groupingNode.Context = transactionController.Model;
                model.RefreshProperty();
                nodes.Add(groupingNode);
            }

            return nodes;
        }
    }
}
