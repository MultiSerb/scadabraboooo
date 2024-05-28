using Common;
using Modbus.FunctionParameters;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus read coil functions/requests.
    /// citanje digitalnih izlaza
    /// </summary>
    public class ReadCoilsFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadCoilsFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        /// // poziva se konstruktor iz klase ModbusFunction
            // i prosledjuju se commandParameters => inicijalizacija klase ModbusFunctions
        public ReadCoilsFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            // provera da li su prosledjeni argumenti ispravni
            // odnosno da je tip argumenata = ModbusReadCommandParameters
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusReadCommandParameters));
            
        }

        /// <inheritdoc/>
        /// pakuje se niz bajtova koji se salje Modbus uredjaju
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
        /// parsira odgovor koji je dobijen od Modbus uredjaja
        /// nakon obradjenog zahteva
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            Dictionary<Tuple<PointType, ushort>, ushort> resp = new Dictionary<Tuple<PointType, ushort>, ushort>();

            int byteCount = response[8];
            // pocetna adresa od koje citamo
            ushort startAddress = ((ModbusReadCommandParameters)CommandParameters).StartAddress;
            ushort counter = 0; // koliko point-a aje procitano

            for (int i = 0; i < byteCount; i++)
            {
                byte temp = response[9 + i];    // trenutna vrednost bajta iz response niza
                byte mask = 1;

                // broj point-a koje treba da procitamo
                ushort quantity = ((ModbusReadCommandParameters)CommandParameters).Quantity;

                for (int j = 0; j < 8; j++)
                {
                    ushort value = (ushort)(temp & mask);// izdvaja svaku vrednost bita iz byte-a
                    // tuple je par Tip, adresa
                    Tuple<PointType, ushort> tuple = new Tuple<PointType, ushort>(PointType.DIGITAL_OUTPUT, startAddress++);
                    resp.Add(tuple, value); // vrednost procitanog signala

                    temp >>= 1; // predji na sledeci bit
                    counter++;  

                    // procitali sve => izadji iz petlje
                    if (counter >= quantity)
                        break;
                }
            }

            return resp; // vratimo recnik sa tipom signala, adresi na kojoj se nalazi, i vrednoscu koju ima
        }
    }
}