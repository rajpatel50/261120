using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using XIAP.Frontend.CommonInfrastructure.Model.Events;
using System.ComponentModel;

namespace GeniusX.AXA.FrontendModules.Claims.Model
{
    public class AXAEventModel: EventModel
    {
        public EventHandler<PropertyChangedEventArgs> ClaimEventPropertyChanged;

        /// <summary>
        /// constructor of a class AXAEventModel.
        /// </summary>
        public AXAEventModel()
            :base()
        {
        }

        /// <summary>
        /// property sets the value of default user identity
        /// </summary>
        public string DefaultUserIdentity { get; set; }

        /// <summary>
        /// property sets the value of default task initial user identity
        /// </summary>
        public long? DefaultTaskInitialUserID { get; set; }
    }
}
