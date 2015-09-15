using System;
using System.Collections.Generic;
using GeniusX.AXA.DesignStudio.Framework.DataLoad.Controllers;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Xiap.DesignStudio.Framework.DataLoad.Controllers;
using Xiap.DesignStudio.Framework.Modules;

namespace GeniusX.AXA.DesignStudio.Framework.DataLoad
{
   public class DataLoadModule : AbstractDesignStudioModule
    {
        public DataLoadModule(IUnityContainer container, IRegionManager region)
            : base(container, region)
        {
            this.Container.RegisterInstance<IDesignStudioModule>("DataLoadModule", this);
        }

        protected override void RegisterTypesAndServices()
        {
            this.Container.RegisterType<IDataLoadController,IOLoadController>("IOSynchronizeController");
        }

        public override void Start(Dictionary<string, string> param, Action<object> callback)
        {
        }
    }
}
    