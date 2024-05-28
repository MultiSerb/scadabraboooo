using Common;
using Modbus;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProcessingModule
{
    /// <summary>
    /// Class containing logic for processing points and executing commands.
    /// </summary>
    public class ProcessingManager : IProcessingManager
    {
        private IFunctionExecutor functionExecutor; // interfejs za izvrsavanje funkcija
        private IStorage storage;   // skladistenje: point type i adresa
        private AlarmProcessor alarmProcessor;  // obrada alarma
        private EGUConverter eguConverter;  // za neke konverzije

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessingManager"/> class.
        /// </summary>
        /// <param name="storage">The point storage.</param>
        /// <param name="functionExecutor">The function executor.</param>
        public ProcessingManager(IStorage storage, IFunctionExecutor functionExecutor)
        {
            this.storage = storage;
            this.functionExecutor = functionExecutor;
            this.alarmProcessor = new AlarmProcessor();
            this.eguConverter = new EGUConverter();
            this.functionExecutor.UpdatePointEvent += CommandExecutor_UpdatePointEvent;
        }

        /// <inheritdoc />
        /// metoda citanja sa uredjaja
        //
        public void ExecuteReadCommand(IConfigItem configItem, ushort transactionId, byte remoteUnitAddress, ushort startAddress, ushort numberOfPoints)
        {
            ModbusReadCommandParameters p = new ModbusReadCommandParameters(6, (byte)GetReadFunctionCode(configItem.RegistryType), startAddress, numberOfPoints, transactionId, remoteUnitAddress);
            IModbusFunction fn = FunctionFactory.CreateModbusFunction(p);
            this.functionExecutor.EnqueueCommand(fn);
        }
        
        /// <inheritdoc />
        /// pisanje na osnovu tipa registra (analogni ili digitalni)
        /// pointAddress = adresa na koju upisujeno value
        public void ExecuteWriteCommand(IConfigItem configItem, ushort transactionId, byte remoteUnitAddress, ushort pointAddress, int value)
        {
            // provera tipa registra i poziv odgovarajuce metode za upis (definisane ispod)
            if (configItem.RegistryType == PointType.ANALOG_OUTPUT)
            {
                ExecuteAnalogCommand(configItem, transactionId, remoteUnitAddress, pointAddress, value);
            }
            else
            {
                ExecuteDigitalCommand(configItem, transactionId, remoteUnitAddress, pointAddress, value);
            }
        }

        /// <summary>
        /// Executes a digital write command.
        /// Izvrsava komandu pisanja za digitalne tacke (Coils)
        /// </summary>
        /// <param name="configItem">The configuration item.</param>
        /// <param name="transactionId">The transaction identifier.</param>
        /// <param name="remoteUnitAddress">The remote unit address.</param>
        /// <param name="pointAddress">The point address.</param>
        /// <param name="value">The value.</param>
        private void ExecuteDigitalCommand(IConfigItem configItem, ushort transactionId, byte remoteUnitAddress, ushort pointAddress, int value)
        {
            // Duzina, function code(na osnovu tipa), outputAddress, value, transactionId, unitId
            ModbusWriteCommandParameters p = new ModbusWriteCommandParameters(6, (byte)ModbusFunctionCode.WRITE_SINGLE_COIL, pointAddress, (ushort)value, transactionId, remoteUnitAddress);
            IModbusFunction fn = FunctionFactory.CreateModbusFunction(p);
            this.functionExecutor.EnqueueCommand(fn);
        }

        /// <summary>
        /// Executes an analog write command.
        /// </summary>
        /// <param name="configItem">The configuration item.</param>
        /// <param name="transactionId">The transaction identifier.</param>
        /// <param name="remoteUnitAddress">The remote unit address.</param>
        /// <param name="pointAddress">The point address.</param>
        /// <param name="value">The value.</param>
        private void ExecuteAnalogCommand(IConfigItem configItem, ushort transactionId, byte remoteUnitAddress, ushort pointAddress, int value)
        {
            value = (int)eguConverter.ConvertToRaw(configItem.ScaleFactor, configItem.Deviation, value);
            ModbusWriteCommandParameters p = new ModbusWriteCommandParameters(6, (byte)ModbusFunctionCode.WRITE_SINGLE_REGISTER, pointAddress, (ushort)value, transactionId, remoteUnitAddress);
            IModbusFunction fn = FunctionFactory.CreateModbusFunction(p); 
            this.functionExecutor.EnqueueCommand(fn);
        }

        /// <summary>
        /// Gets the modbus function code for the point type.
        /// </summary>
        /// <param name="registryType">The register type.</param>
        /// <returns>The modbus function code.</returns>
        private ModbusFunctionCode? GetReadFunctionCode(PointType registryType)
        {
            switch (registryType)
            {
                case PointType.DIGITAL_OUTPUT: return ModbusFunctionCode.READ_COILS;
                case PointType.DIGITAL_INPUT: return ModbusFunctionCode.READ_DISCRETE_INPUTS;
                case PointType.ANALOG_INPUT: return ModbusFunctionCode.READ_INPUT_REGISTERS;
                case PointType.ANALOG_OUTPUT: return ModbusFunctionCode.READ_HOLDING_REGISTERS;
                case PointType.HR_LONG: return ModbusFunctionCode.READ_HOLDING_REGISTERS;
                default: return null;
            }
        }

        /// <summary>
        /// Method for handling received points.
        /// Poziva se kada functionExecutor objekat dobije novu vrednost za tacku
        /// </summary>
        /// <param name="type">The point type.</param>
        /// <param name="pointAddress">The point address.</param>
        /// <param name="newValue">The new value.</param>
        private void CommandExecutor_UpdatePointEvent(PointType type, ushort pointAddress, ushort newValue)
        {
            // pristupamo tacki iz storage na osnovu tipa i adrese
            List<IPoint> points = storage.GetPoints(new List<PointIdentifier>(1) { new PointIdentifier(type, pointAddress) });
            
            // Na osnovu tipa tacke (analog/ digital) pozivamo odgovarajuce metode
            if (type == PointType.ANALOG_INPUT || type == PointType.ANALOG_OUTPUT)
            {
                ProcessAnalogPoint(points.First() as IAnalogPoint, newValue);
            }
            else
            {
                ProcessDigitalPoint(points.First() as IDigitalPoint, newValue);
            }
        }

        /// <summary>
        /// Processes a digital point.
        /// </summary>
        /// <param name="point">The digital point</param>
        /// <param name="newValue">The new value.</param>
        private void ProcessDigitalPoint(IDigitalPoint point, ushort newValue)
        {
            point.RawValue = newValue;  // postavimo novu vrednost za dig tacku
            point.Timestamp = DateTime.Now; // trenutno vreme
            point.State = (DState)newValue; // stanje (za dig to je ON/OFF)
            point.Alarm = alarmProcessor.GetAlarmForDigitalPoint(point.RawValue, point.ConfigItem);

        }

        /// <summary>
        /// Processes an analog point
        /// </summary>
        /// <param name="point">The analog point.</param>
        /// <param name="newValue">The new value.</param>
        private void ProcessAnalogPoint(IAnalogPoint point, ushort newValue)
        {
            point.EguValue = eguConverter.ConvertToEGU(point.ConfigItem.ScaleFactor, point.ConfigItem.Deviation, newValue);
            point.RawValue = newValue;  // nova vr
            point.Timestamp = DateTime.Now; // trenutno vreme
            point.Alarm = alarmProcessor.GetAlarmForAnalogPoint(point.EguValue, point.ConfigItem);
        }

        /// <inheritdoc />
        /// za inicijalizaciju tacke sa zadatom adresom i podrazumevanom vrednoscu
        public void InitializePoint(PointType type, ushort pointAddress, ushort defaultValue)
        {
            // pristup tacki na osnovu tipa i adrese
            /*
                struktura PointIdcentifier ima PointType (enum) i short Address
            GetPoints metoda na osnovu ove strukture dobavlja tacku iz storage-a
                IPoint je interfejs za tacku
             */
            List<IPoint> points = storage.GetPoints(new List<PointIdentifier>(1) { new PointIdentifier(type, pointAddress) });

            // na osnovu tipa tacke Analog/Digital poziva odg metodu za inicijalizaciju
            // iste metode kao i za izmenu
            if (type == PointType.ANALOG_INPUT || type == PointType.ANALOG_OUTPUT)
            {
                ProcessAnalogPoint(points.First() as IAnalogPoint, defaultValue);
            }
            else
            {
                ProcessDigitalPoint(points.First() as IDigitalPoint, defaultValue);
            }
        }
    }
}
