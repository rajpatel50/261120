using System;
using System.Linq;
using System.Collections.ObjectModel;
using GeniusX.AXA.FrontendModules.Claims.Resources;
using Microsoft.Practices.Prism.Commands;
using Xiap.Metadata.Data.Enums;
using XIAP.Frontend.CommonInfrastructure.Model.Documents;
using XIAP.Frontend.Infrastructure;
using XIAP.Frontend.Infrastructure.DataMapping;
using XIAP.Frontend.Infrastructure.Menu;
using XIAP.FrontendModules.Common.Documents;

namespace GeniusX.AXA.FrontendModules.Claims.Model
{
    public class AXADocumentModel : DocumentModel
    {
        private RetrievalManager _retrievalManager;
        private bool _isPrintOrFinaliseClicked;

        public AXADocumentModel()
            :base()
        {
            this.PrintAndFinaliseDocumentCommand = new NamedDelegateCommand<IDocumentDetails>("PrintAndFinaliseDocumentCommand", this.OnPrintAndFinaliseDocument, this.CanPrintAndFinaliseDocument);
            this.FinaliseDocumentCommand = new NamedDelegateCommand<IDocumentDetails>("FinaliseDocumentCommand", this.OnFinaliseDocument, this.CanFinaliseDocument);
            this.OnSaveCommand = new DelegateCommand(this.OnSave, this.CanSave);
        }

        public event EventHandler<CommandDataEventArgs<IDocumentDetails>> OnPrintAndFinaliseDocumentClick;
        public event EventHandler<CommandDataEventArgs<IDocumentDetails>> OnFinaliseDocumentClick;
        public event EventHandler OnSaveClick;
        
        public DelegateCommand<IDocumentDetails> FinaliseDocumentCommand { get; private set; }
        public DelegateCommand<IDocumentDetails> PrintAndFinaliseDocumentCommand { get; private set; }
        public DelegateCommand OnSaveCommand { get; private set; }

        public bool IsPrintOrFinaliseClicked
        {
            get
            {
                return this._isPrintOrFinaliseClicked;
            }

            set
            {
                this._isPrintOrFinaliseClicked = value;
                OnPropertyChanged("IsPrintOrFinaliseClicked");
            }
        }

        public RetrievalManager RetrievalManager
        {
            get
            {
                return this._retrievalManager;
            }

            set
            {
                this._retrievalManager = value;
            }
        }

        public void OnSave()
        {
            if (this.OnSaveClick != null)
            {
                this.InvokeEvent(this.OnSaveClick);
            }
        }
      
        public override bool CanRemoveDocument(IDocumentDetails arg)
        {
            if (arg !=null && arg.FileType == (short)StaticValues.FileType.UploadedFile)
            {
                return false;
            }

            return  base.CanRemoveDocument(arg);
        }

        protected override ObservableCollection<IMenuItemCommand> BuildXIAPMenuItems()
        {
            ObservableCollection<IMenuItemCommand> customMenuItems = new ObservableCollection<IMenuItemCommand>();
            customMenuItems = base.BuildXIAPMenuItems();

            customMenuItems.Add(new CommandMenuItem<IDocumentDetails>()
            {
                MenuItemCommand = this.PrintAndFinaliseDocumentCommand,
                Title = StringResources.Documents_Menu_PrintAndFinalise,
                CommandParameterDelegate = () => this.SelectedDocuments.FirstOrDefault()
            });

            customMenuItems.Add(new CommandMenuItem<IDocumentDetails>()
            {
                MenuItemCommand = this.FinaliseDocumentCommand,
                Title = StringResources.Documents_Menu_Finalise,
                CommandParameterDelegate = () => this.SelectedDocuments.FirstOrDefault()
            });

            return customMenuItems;
        }

        protected override void DocumentModel_ExecuteChangedForCommands()
        {
            base.DocumentModel_ExecuteChangedForCommands();
            this.PrintAndFinaliseDocumentCommand.RaiseCanExecuteChanged();
            this.FinaliseDocumentCommand.RaiseCanExecuteChanged();
        }

        public bool CanPrintAndFinaliseDocument(IDocumentDetails arg)
        {
            if (arg == null || this.HasMultipleSelectedDocument(this.SelectedDocuments))
            {
                return false;
            }
            else
            {
                if (arg.FileType != (short)StaticValues.FileType.StandardDocument)
                {
                    return false;
                }

                return this.RecreateEnabled(arg);
            }
        }

        public  bool CanFinaliseDocument(IDocumentDetails arg)
        {
            if (arg == null || this.HasMultipleSelectedDocument(this.SelectedDocuments))
            {
                return false;
            }
            else
            {
                if (arg.FileType != (short)StaticValues.FileType.StandardDocument)
                {
                    return false;
                }

                return this.RecreateEnabled(arg);
            }
        }
        
        public bool CanSave()
        {
            return true;
        }

        public void OnPrintAndFinaliseDocument(IDocumentDetails arg)
        {
            this.InvokeEvent<CommandDataEventArgs<IDocumentDetails>>(this.OnPrintAndFinaliseDocumentClick, new CommandDataEventArgs<IDocumentDetails>(arg));
        }

        public void OnFinaliseDocument(IDocumentDetails arg)
        {
            this.InvokeEvent<CommandDataEventArgs<IDocumentDetails>>(this.OnFinaliseDocumentClick, new CommandDataEventArgs<IDocumentDetails>(arg));
        }
    }
}
