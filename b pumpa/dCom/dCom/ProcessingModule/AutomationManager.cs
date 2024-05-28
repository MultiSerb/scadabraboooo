using Common;
using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Net.Sockets;
using System.Threading;

namespace ProcessingModule
{
    /// <summary>
    /// Class containing logic for automated work.
    /// </summary>
    public class AutomationManager : IAutomationManager, IDisposable
    {
        private Thread automationWorker;
        private AutoResetEvent automationTrigger;
        private IStorage storage;
        private IProcessingManager processingManager;
        private int delayBetweenCommands;
        private IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutomationManager"/> class.
        /// </summary>
        /// <param name="storage">The storage.</param>
        /// <param name="processingManager">The processing manager.</param>
        /// <param name="automationTrigger">The automation trigger.</param>
        /// <param name="configuration">The configuration.</param>
        public AutomationManager(IStorage storage, IProcessingManager processingManager, AutoResetEvent automationTrigger, IConfiguration configuration)
        {
            this.storage = storage;
            this.processingManager = processingManager;
            this.configuration = configuration;
            this.automationTrigger = automationTrigger;
            this.delayBetweenCommands = configuration.DelayBetweenCommands;
        }

        /// <summary>
        /// Initializes and starts the threads.
        /// </summary>
		private void InitializeAndStartThreads()
        {
            InitializeAutomationWorkerThread();
            StartAutomationWorkerThread();
        }

        /// <summary>
        /// Initializes the automation worker thread.
        /// </summary>
		private void InitializeAutomationWorkerThread()
        {
            automationWorker = new Thread(AutomationWorker_DoWork);
            automationWorker.Name = "Aumation Thread";
        }

        /// <summary>
        /// Starts the automation worker thread.
        /// </summary>
		private void StartAutomationWorkerThread()
        {
            automationWorker.Start();
        }


        private void AutomationWorker_DoWork()
        {
            EGUConverter egu = new EGUConverter();
            PointIdentifier fuel = new PointIdentifier(PointType.ANALOG_OUTPUT,1000); // kolicina goriva u reze
            PointIdentifier pump01 = new PointIdentifier(PointType.DIGITAL_OUTPUT,3000);
            PointIdentifier pump02 = new PointIdentifier(PointType.DIGITAL_OUTPUT, 3001);
            PointIdentifier pump03 = new PointIdentifier(PointType.DIGITAL_OUTPUT, 3002);
            PointIdentifier V1 = new PointIdentifier(PointType.DIGITAL_OUTPUT,2000);
            List<PointIdentifier> list = new List<PointIdentifier> { fuel, pump01, pump02, pump03,V1};

            int fuel_value = 0; 
            while (!disposedValue)
            {
                List<IPoint> points = storage.GetPoints(list);
                // pretvaranje u ing jedinice 
                fuel_value = (int)egu.ConvertToEGU(points[0].ConfigItem.ScaleFactor, points[0].ConfigItem.Deviation, points[0].RawValue);

                if (points[4].RawValue == 0)
                {
                    processingManager.ExecuteWriteCommand(points[1].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress,pump01.Address, 0);
                    processingManager.ExecuteWriteCommand(points[2].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, pump02.Address, 0);
                    processingManager.ExecuteWriteCommand(points[3].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, pump03.Address, 0);
                    fuel_value += 10; 
                }

                if (points[1].RawValue == 1 )
                {
                    processingManager.ExecuteWriteCommand(points[4].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, V1.Address, 1);
                    //pumpa 1
                    fuel_value -= 1; 

                }
                if (points[2].RawValue == 1 )
                {
                    processingManager.ExecuteWriteCommand(points[4].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, V1.Address, 1);
                    // pumpa 2 
                    fuel_value -= 1;
                }
                if (points[3].RawValue == 1 ) 
                {
                    processingManager.ExecuteWriteCommand(points[4].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, V1.Address, 1);
                    // pumpa 3
                    fuel_value -= 3;
                }

                if (points[4].RawValue == 0 && fuel_value >= 990)
                {
                    fuel_value = 1000;
                    processingManager.ExecuteWriteCommand(points[0].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, fuel.Address, fuel_value);
                    processingManager.ExecuteWriteCommand(points[4].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, V1.Address, 1); // zavrce se ventil da se pumpa ne puni vise
                }
                else
                {
                    processingManager.ExecuteWriteCommand(points[0].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, fuel.Address, fuel_value);
                }

                automationTrigger.WaitOne();
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls


        /// <summary>
        /// Disposes the object.
        /// </summary>
        /// <param name="disposing">Indication if managed objects should be disposed.</param>
		protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }
                disposedValue = true;
            }
        }


        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public void Start(int delayBetweenCommands)
        {
            this.delayBetweenCommands = delayBetweenCommands * 1000;
            InitializeAndStartThreads();
        }

        /// <inheritdoc />
        public void Stop()
        {
            Dispose();
        }
        #endregion
    }
}