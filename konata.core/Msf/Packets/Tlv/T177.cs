﻿using Konata.Utils;

namespace Konata.Msf.Packets.Tlvs
{
    public class T177 : TlvBase
    {
        private readonly uint _buildTime;
        private readonly string _sdkVersion;

        public T177(uint buildTime, string sdkVersion)
        {
            _buildTime = buildTime;
            _sdkVersion = sdkVersion;
        }

        public override void PutTlvCmd()
        {
            PutUshortBE(0x177);
        }

        public override void PutTlvBody()
        {
            PutByte(1);
            PutUintBE(_buildTime);
            PutString(_sdkVersion);
        }
    }
}
