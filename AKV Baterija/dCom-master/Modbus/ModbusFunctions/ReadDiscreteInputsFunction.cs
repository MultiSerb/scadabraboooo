using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus read discrete inputs functions/requests.
    /// citanje digitalnih ulaza
    /// </summary>
    public class ReadDiscreteInputsFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadDiscreteInputsFunction"/> class.
        /// </summary>
        /// inicijalizacija
        /// <param name="commandParameters">The modbus command parameters.</param>
        public ReadDiscreteInputsFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            // poziva proveru za ispravnost prosledjenih parametara tj proverava tip
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusReadCommandParameters));
        }

        /// <inheritdoc />
        /// pakovanje zahteva u niz bajtova
        public override byte[] PackRequest()
        {
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
        /// parsira odgovor dobijen od Modbus-a nakon sto se izvrsi
        /// citanje diskretnih ulaza
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            // u ovaj recnik cemo ubaciti rezultate
            Dictionary<Tuple<PointType, ushort>, ushort> resp = new Dictionary<Tuple<PointType, ushort>, ushort>();

            int byteCount = response[8];  // broj bajtova koji sadrzi podatke o ulazima
            ushort startAddress = ((ModbusReadCommandParameters)CommandParameters).StartAddress;
            ushort counter = 0;

            // prolazimo kroz svaki bajt 
            // svaki bit u tom bajtu predstavlja stanje diskretnog tj digitalnog ulaza

            // prolazimo kroz bajtove
            for (int i = 0; i < byteCount; i++)
            {
                byte temp = response[9 + i]; // od response[8] imamo jos byteCount bajtova koji sadrze inf o nasim ulazima
                byte mask = 1;  // napravimo masku da bismo mogli da ocitamo svaki bit iz bajta

                // broj digitalnih ulaza koje citamo
                ushort quantity = ((ModbusReadCommandParameters)CommandParameters).Quantity;

                // prolazimo kroz svaki bit u bajtu
                for (int j = 0; j < 8; j++)
                {
                    ushort value = (ushort)(temp & mask);   // 0 ili 1
                    Tuple<PointType, ushort> tuple = new Tuple<PointType, ushort>(PointType.DIGITAL_INPUT, startAddress++);
                    resp.Add(tuple, value);

                    temp >>= 1;
                    counter++;

                    if (counter >= quantity)
                        break;
                }
            }

            return resp;
        }
    }
}