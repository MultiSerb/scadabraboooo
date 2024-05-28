using Common;
using System;
using System.Collections.Generic;
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
			EGUConverter eguConverter = new EGUConverter();
			PointIdentifier T1 = new PointIdentifier(PointType.DIGITAL_OUTPUT, 5000);
			PointIdentifier T2 = new PointIdentifier(PointType.DIGITAL_OUTPUT, 5001);
			PointIdentifier T3 = new PointIdentifier(PointType.DIGITAL_OUTPUT, 5002);
			PointIdentifier T4 = new PointIdentifier(PointType.DIGITAL_OUTPUT, 5003);
			PointIdentifier T5 = new PointIdentifier(PointType.DIGITAL_OUTPUT, 5004);
			PointIdentifier K = new PointIdentifier(PointType.ANALOG_OUTPUT, 2000);
			PointIdentifier I1 = new PointIdentifier(PointType.DIGITAL_OUTPUT, 4000);
			PointIdentifier I2 = new PointIdentifier(PointType.DIGITAL_OUTPUT, 4001);

			List<PointIdentifier> pointsList = new List<PointIdentifier>() { T1, T2, T3, T4, T5, K, I1, I2 };

			while (!disposedValue)
			{
				List<IPoint> points = storage.GetPoints(pointsList);
				int k = (int)eguConverter.ConvertToEGU(points[5].ConfigItem.ScaleFactor, points[5].ConfigItem.Deviation, points[5].RawValue);
				int t1 = (int)points[0].RawValue;
				int t2 = (int)points[1].RawValue;
				int t3 = (int)points[2].RawValue;
				int t4 = (int)points[3].RawValue;
				int t5 = (int)points[4].RawValue;
				int i1 = (int)points[6].RawValue;
				int i2 = (int)points[7].RawValue;
				int temp = k;

				//if (points[5].Alarm == AlarmType.LOW_ALARM)
				//{
				//	// iskljuci sve T
				//	processingManager.ExecuteWriteCommand(points[0].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, 5000, 0);
    //                processingManager.ExecuteWriteCommand(points[1].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, 5001, 0);
    //                processingManager.ExecuteWriteCommand(points[2].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, 5002, 0);
    //                processingManager.ExecuteWriteCommand(points[3].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, 5003, 0);
    //                processingManager.ExecuteWriteCommand(points[4].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, 5004, 0);

				//	continue;
    //            }

				if (points[5].Alarm == AlarmType.LOW_ALARM)
				{
                    // iskljuci t4
                    processingManager.ExecuteWriteCommand(points[3].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, 5003, 0);

                    // iskljuci t5
                    processingManager.ExecuteWriteCommand(points[4].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, 5004, 0);

					// ukljuci i1
					processingManager.ExecuteWriteCommand(points[6].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, 4000, 1);
                    // ukljuci i2
                    processingManager.ExecuteWriteCommand(points[7].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, 4001, 1);

					//continue;

                }

				if (points[5].RawValue >= points[5].ConfigItem.EGU_Max)
                {
					// iskljuci i1
                    processingManager.ExecuteWriteCommand(points[6].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, 4000, 0);
                    // iskljuci i2
                    processingManager.ExecuteWriteCommand(points[7].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, 4001, 0);

                  //  continue;


                }

                if (t1 == 1)
				{
					temp -= 1;
				}
				if (t2 == 1)
				{
					temp -= 1;
				}
				if (t3 == 1)
				{
					temp -= 1;
				}
				if (t4 == 1)
				{
					temp -= 3;
				}

				if (t5 == 1)
				{
					temp -= 2;
				}
				if (i1 == 1)
				{
					// iskljuci i2 
					//processingManager.ExecuteWriteCommand(points[7].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, 4001, 0);
					temp += 3;
				}
				if (i2 == 1)
				{
                    //processingManager.ExecuteWriteCommand(points[6].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, 4000, 0);

                    temp += 4;
				}

				if (temp != k)
				{
					processingManager.ExecuteWriteCommand(points[5].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, 2000, temp);
				}

				Thread.Sleep(2000);
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
			this.delayBetweenCommands = delayBetweenCommands*1000;
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
