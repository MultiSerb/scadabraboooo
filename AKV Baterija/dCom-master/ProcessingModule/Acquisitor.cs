using Common;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ProcessingModule
{
    /// <summary>
    /// Class containing logic for periodic polling.= Periodicno citanje podataka
    /// </summary>
    /// 

    public class Acquisitor : IDisposable
	{
		private AutoResetEvent acquisitionTrigger;
        private IProcessingManager processingManager; // za upravljanje obradom podataka
        private Thread acquisitionWorker;   // nit za akvizitora
		private IStateUpdater stateUpdater; // azuriranje stanja
		private IConfiguration configuration;   // konfiguracija

        /// <summary>
        /// Initializes a new instance of the <see cref="Acquisitor"/> class.
        /// </summary>
        /// <param name="acquisitionTrigger">The acquisition trigger.</param>
        /// <param name="processingManager">The processing manager.</param>
        /// <param name="stateUpdater">The state updater.</param>
        /// <param name="configuration">The configuration.</param>
		public Acquisitor(AutoResetEvent acquisitionTrigger, IProcessingManager processingManager, IStateUpdater stateUpdater, IConfiguration configuration)
		{
			this.stateUpdater = stateUpdater;
			this.acquisitionTrigger = acquisitionTrigger;
			this.processingManager = processingManager;
			this.configuration = configuration;
			this.InitializeAcquisitionThread();
			this.StartAcquisitionThread();
		}

		#region Private Methods

        /// <summary>
        /// Initializes the acquisition thread.
        /// </summary>
		private void InitializeAcquisitionThread()
		{
            // kreiramo novu nit
			this.acquisitionWorker = new Thread(Acquisition_DoWork);
			this.acquisitionWorker.Name = "Acquisition thread";
		}

        /// <summary>
        /// Starts the acquisition thread.
        /// </summary>
		private void StartAcquisitionThread()
		{
			acquisitionWorker.Start(); // pokreni kreiranu nit
		}

        /// <summary>
        /// Acquisitor thread logic.
        /// Periodicno prikupljanje podataka sa uredjaja
        /// uz pomoc modbus-a
        /// </summary>
		private void Acquisition_DoWork()
        {
            //TO DO: IMPLEMENT
            {
                // dobavljamo konfiguracione elemente iz nasig config fajlova
                // IConfigItem interfejs koji sadrzi sve potrebne kolone za item
                // IConfiguration interfejs sa metodama za upravljanje elementima iz configuracije
                List<IConfigItem> configItems = configuration.GetConfigurationItems();

                while (true)
                {
                    // cekamo triger, to jue AutoResetEvent
                    // postavlja se kada je potrebno izvrsiti akviziciju podataka
                    acquisitionTrigger.WaitOne();

                    // prolazimo kroz sve stavke iz konfiguracionog fajla
                    foreach (IConfigItem configItem in configItems)
                    {
                        configItem.SecondsPassedSinceLastPoll++; // povecava se vreme

                        // ako je proslo vreme AquisitionInterval za tu konfiguraciju, citamo nove podatke sa uredjaja
                        // AquisitionInterval = vreme osvezavanja, # = 1s , iz teksta citamo
                        if (configItem.SecondsPassedSinceLastPoll == configItem.AcquisitionInterval)
                        {
                            processingManager.ExecuteReadCommand(
                                configItem,
                                configuration.GetTransactionId(),
                                configuration.UnitAddress,
                                configItem.StartAddress,
                                configItem.NumberOfRegisters
                            );

                            configItem.SecondsPassedSinceLastPoll = 0; // reset vremena za ponovnu iteraciju
                        }
                    }
                }
            }
        }

        #endregion Private Methods

        /// <inheritdoc />
        public void Dispose()
		{
			acquisitionWorker.Abort();
        }
	}
}