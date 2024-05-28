using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus read input registers functions/requests.
    /// Citanje aanalognih ulaza
    /// </summary>
    public class ReadInputRegistersFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadInputRegistersFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        /// nova instanca ReadInputRegister funkcije
        public ReadInputRegistersFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            // provera tipa prosledjenog parametra (ModbusReadCommPar/ ModbusWriteCommPar)
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusReadCommandParameters));
        }

        /// <inheritdoc />
        public override byte[] PackRequest()
        {
            // niz bajtova puni se poljima klase ModbusCommandParameters
            byte[] req = new byte[12];

            req[0] = BitConverter.GetBytes(CommandParameters.TransactionId)[1];
            req[1] = BitConverter.GetBytes(CommandParameters.TransactionId)[0];
            req[2] = BitConverter.GetBytes(CommandParameters.ProtocolId)[1];
            req[3] = BitConverter.GetBytes(CommandParameters.ProtocolId)[0];
            req[4] = BitConverter.GetBytes(CommandParameters.Length)[1];
            req[5] = BitConverter.GetBytes(CommandParameters.Length)[0];
            req[6] = CommandParameters.UnitId;
            req[7] = CommandParameters.FunctionCode;
            req[8] = BitConverter.GetBytes(((ModbusReadCommandParameters)CommandParameters).StartAddress)[1];
            req[9] = BitConverter.GetBytes(((ModbusReadCommandParameters)CommandParameters).StartAddress)[0];
            req[10] = BitConverter.GetBytes(((ModbusReadCommandParameters)CommandParameters).Quantity)[1];
            req[11] = BitConverter.GetBytes(((ModbusReadCommandParameters)CommandParameters).Quantity)[0];

            return req;
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            Dictionary<Tuple<PointType, ushort>, ushort> resp = new Dictionary<Tuple<PointType, ushort>, ushort>();

            int byteCount = response[8]; // broj bajtova iz response-a koji sadrzi neke inf o analognim ulazima
            ushort startAddress = ((ModbusReadCommandParameters)CommandParameters).StartAddress;

            // cita se svaki par bajtova 
            for (int i = 0; i < byteCount; i += 2)
            {
                // pretvaramo u 16bitnu vrednost sa BitConverter.ToUInt16
                // i onda redosled bajtova konvertujemo tako da odgovara host-u
                ushort value = (ushort)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(response, 9 + i));
                Tuple<PointType, ushort> tuple = new Tuple<PointType, ushort>(PointType.ANALOG_INPUT, startAddress++);
                resp.Add(tuple, value);
            }

            return resp;
        }
    }
}