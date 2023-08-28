using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using CCI.SimplSharp.Library.Components.Common;
using CCI.SimplSharp.Library.IO.Common;
using BoseExSeriesLib.ProtocolSupport;
using BoseExSeriesLib.Enums;
using CCI.SimplSharp.Library.Components.States;
using CCI.SimplSharp.Library.Components.EventArguments;
using CCI.SimplSharp.Library.Components.Registration;
using BoseExSeriesLib.ProtocolSupport.Models;
using BoseExSeriesLib.EventArguments;
using BoseExSeriesLib.Interfaces;
using CCI.SimplSharp.Library.IO.Utilities;
using System.Text.RegularExpressions;
using System.Collections;

namespace BoseExSeriesLib.Components
{
    public class ParameterSetComponent : IComponent<MessageBundle, IResponse>, IRefresh, IDisposable
    {
        // Events
        ////////////////////////////////////////////////////

        public event InitializationEventHandler OnInitializationChange;
        public event LastParameterSetEventHandler OnLastParameterSetChange;

        // Members
        ////////////////////////////////////////////////////

        protected Boolean Ready = false;
        protected IProcessor<MessageBundle> listener = null;

        protected Boolean IsInitialized = false;

        protected Boolean IsQuarantined = false;

        // Properties
        ////////////////////////////////////////////////////

        private UInt16 LastRecalled { get; set; }

        ////////////////////////////////////////////////////
        ////////////////////////////////////////////////////
        ////////////////////////////////////////////////////

        public ParameterSetComponent()
        {
            this.LastRecalled = UInt16.MinValue;
        }

        // Exposed
        ////////////////////////////////////////////////////

        public void Configure(UInt16 CommandProcessorId)
        {
            this.CommandProcessorId = CommandProcessorId;

            this.Id = Registrar.GetNextComponentId(this);
            Registrar.Register(this);

            this.Ready = true;
        }

        public void RecallParameterSet(UInt16 number)
        {
            if (number > 0 && this.IsInitialized)
            {
                string value = number.ToString("X");

                this.listener.Enqueue(this, ProtocolUtil.BuildParameterSetMessage(this, value));
            }
        }

        public void UnRegister()
        {
            Registrar.UnRegister(this);

            ((IInitialize)this).Reinitialize();

            this.updateInitializationChange();
        }


        // Helper Methods
        ////////////////////////////////////////////////////

        private void processIntializationChange(Boolean state)
        {
            if (this.OnInitializationChange != null)
                this.OnInitializationChange(this, new InitializationEventArgs(CrestronSimplPlusHelper.ToCrestronBool(state)));
        }

        private void updateInitializationChange()
        {
            bool state = ((IComponent)this).IsInitialized();
            this.processIntializationChange(state);
        }


        // IComponent<MessageBundle, MessageBundle>
        ////////////////////////////////////////////////////

        void IComponent<MessageBundle, IResponse>.ProcessSubscription(IResponse response)
        {
            if (response is IResponse)
            {
                if (response is ParameterSetResponse)
                {
                    ParameterSetResponse r = (ParameterSetResponse)response;

                    this.LastRecalled = r.Response;

                    if (this.OnLastParameterSetChange != null)
                        this.OnLastParameterSetChange(this, new LastParameterSetEventArgs(this.LastRecalled));
                }
            }
        }

        IProcessor<MessageBundle> IComponent<MessageBundle, IResponse>.Processor
        {
            get { return this.listener; }
        }

        // IResponseHandler<MessageBundle, MessageBundle>
        ////////////////////////////////////////////////////

        void IResponseHandler<MessageBundle, IResponse>.ProcessResponse(IResponse response, MessageBundle request)
        {
            // UNUSED
        }

        // IComponent
        ////////////////////////////////////////////////////

        public UInt16 CommandProcessorId { get; private set; }

        public UInt16 Id { get; private set; } 

        void IComponent.GetInitialized()
        {
            this.IsInitialized = true;

            this.updateInitializationChange();
        }

        void IComponent.UpdateMainProcess(IProcessor processor)
        {
            if (processor is IProcessor)
                this.listener = (IProcessor<MessageBundle>)processor;
        }

        // IInitialize
        ////////////////////////////////////////////////////

        bool IInitialize.IsInitialized()
        {
            return this.IsInitialized;
        }

        void IInitialize.Reinitialize()
        {
            try
            {
                this.IsInitialized = false;
                this.LastRecalled = UInt16.MinValue;

                ((IRefresh)this).Refresh();

                this.updateInitializationChange();
            }
            catch (Exception ex)
            {
                CrestronConsole.PrintLine("StateComponent.Reinitialize.Exception: {0}", ex.Message);
            }
        }

        // IRefresh
        ////////////////////////////////////////////////////

        void IRefresh.Refresh()
        {
            if (this.OnLastParameterSetChange != null)
                this.OnLastParameterSetChange(this, new LastParameterSetEventArgs(this.LastRecalled));
        }

        // IDisposable
        ////////////////////////////////////////////////////

        void IDisposable.Dispose()
        {
            try
            {
                Registrar.UnRegister(this);

                if (this.OnInitializationChange != null)
                    this.OnInitializationChange = null;
             
            }
            catch
            { 
            
            }
        }
    }
}