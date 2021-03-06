﻿using Konata.Utils;

namespace Konata.Msf.Packets.Tlv
{
    public class T16e : TlvBase
    {
        private readonly string _deviceName;

        public T16e(string deviceName) : base()
        {
            _deviceName = deviceName;

            PackGeneric();
        }

        public override void PutTlvCmd()
        {
            PutUshortBE(0x16e);
        }

        public override void PutTlvBody()
        {
            PutString(_deviceName);
        }
    }
}
