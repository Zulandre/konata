﻿using System.IO;
using System.Text;
using Konata.Msf.Crypto;
using Konata.Msf.Utils.Crypt;
using Konata.Msf.Packets.Tlv;
using Konata.Msf.Packets.Protobuf;
using ProtoBuf;

namespace Konata.Msf.Packets.Oicq
{
    public class OicqRequestTgtgt : OicqRequest
    {
        private KeyBody _keyBody;
        private TlvBody _tlvBody;

        public OicqRequestTgtgt(uint uin, string password, uint ssoseq, KeyRing keyring)
        {
            _cmd = 0x0810;
            _subCmd = 0x09;

            _keyBody = new KeyBody(keyring._randKey, keyring._defaultPublicKey);
            _tlvBody = new TlvBody(_subCmd, uin, password, ssoseq, keyring._tgtgKey);

            // 構建 oicq_request
            PutByte(0x02); // 頭部 0x02

            EnterBarrier(2, Endian.Big, 4);
            {
                PutUshortBE(8001); // 協議版本 1F 41
                PutUshortBE(_cmd);
                PutUshortBE(1);
                PutUintBE(uin);
                PutByte(0x03);
                PutByte(0x87); // 加密方式id
                PutByte(0x00); // 永遠0
                PutUintBE(2);
                PutUintBE(AppInfo.appClientVersion);
                PutUintBE(0);

                PutPacket(_keyBody);
                PutPacketEncrypted(_tlvBody, TeaCryptor.Instance, keyring._shareKey);
            }
            LeaveBarrier();

            PutByte(0x03); // 尾部 0x03
        }

        private class KeyBody : Packet
        {
            public KeyBody(byte[] randKey, byte[] publicKey) : base()
            {
                PutUshortBE(0x0101);
                PutBytes(randKey);
                PutUshortBE(0x0102);
                PutBytes(publicKey, 2);
            }
        }

        private class TlvBody : Packet
        {
            public TlvBody(ushort subCmd, uint uin, string password, uint ssoseq,
                byte[] tgtgKey) : base()
            {
                // 設備訊息上報
                var deviceReport = new DeviceReport
                {
                    Bootloader = Encoding.UTF8.GetBytes(DeviceInfo.Build.Bootloader),
                    Version = new byte[0], // Encoding.UTF8.GetBytes(DeviceInfo.System.OsVersion),
                    CodeName = Encoding.UTF8.GetBytes(DeviceInfo.Build.CodeName),
                    Incremental = Encoding.UTF8.GetBytes(DeviceInfo.Build.Incremental),
                    Fingerprint = Encoding.UTF8.GetBytes(DeviceInfo.Build.Fingerprint),
                    BootId = new byte[0], // Encoding.UTF8.GetBytes(DeviceInfo.BootId),
                    AndroidId = Encoding.UTF8.GetBytes(DeviceInfo.System.AndroidId),
                    BaseBand = Encoding.UTF8.GetBytes(DeviceInfo.Build.BaseBand),
                    InnerVersion = Encoding.UTF8.GetBytes(DeviceInfo.Build.InnerVersion)
                };
                MemoryStream reportData = new MemoryStream();
                Serializer.Serialize(reportData, deviceReport);

                var passwordMd5 = new Md5Cryptor().Encrypt(Encoding.UTF8.GetBytes(password));

                // 構建 tlv
                TlvPacker tlvs = new TlvPacker();
                tlvs.PutTlv(new T18(AppInfo.appId, AppInfo.appClientVersion, uin));
                tlvs.PutTlv(new T1(uin, DeviceInfo.Network.Wifi.IpAddress));
                tlvs.PutTlv(new T106(AppInfo.appId, AppInfo.subAppId, AppInfo.appClientVersion, uin,
                    new byte[4], true, passwordMd5, 0, tgtgKey, true, DeviceInfo.Guid, LoginType.Password));
                tlvs.PutTlv(new T116(184024956, 66560));
                tlvs.PutTlv(new T100(AppInfo.appId, AppInfo.subAppId, AppInfo.appClientVersion));
                tlvs.PutTlv(new T107(0, 0, 0));
                tlvs.PutTlv(new T142(AppInfo.apkPackageName));
                tlvs.PutTlv(new T144(DeviceInfo.System.AndroidId, reportData.ToArray(), DeviceInfo.System.Os,
                    DeviceInfo.System.OsVersion, DeviceInfo.Network.Type, DeviceInfo.Network.Mobile.OperatorName,
                    DeviceInfo.Network.Wifi.ApnName, true, true, false, DeviceInfo.Guid, 285212672,
                    DeviceInfo.System.ModelName, DeviceInfo.System.Manufacturer, tgtgKey));
                tlvs.PutTlv(new T145(DeviceInfo.Guid));
                tlvs.PutTlv(new T147(AppInfo.appId, AppInfo.apkVersionName, AppInfo.apkSignature));
                // tlvs.PushTlv(new 166());
                tlvs.PutTlv(new T154(ssoseq));
                tlvs.PutTlv(new T141(DeviceInfo.Network.Mobile.OperatorName, DeviceInfo.Network.Type, DeviceInfo.Network.Wifi.ApnName));
                tlvs.PutTlv(new T8());
                tlvs.PutTlv(new T511(new string[]
                {
                    "office.qq.com",
                    "qun.qq.com",
                    "gamecenter.qq.com",
                    "docs.qq.com",
                    "mail.qq.com",
                    "ti.qq.com",
                    "vip.qq.com",
                    "tenpay.com",
                    "qqweb.qq.com",
                    "qzone.qq.com",
                    "mma.qq.com",
                    "game.qq.com",
                    "openmobile.qq.com",
                    "connect.qq.com",
                }));
                tlvs.PutTlv(new T187(DeviceInfo.Network.Wifi.MacAddress));
                tlvs.PutTlv(new T188(DeviceInfo.System.AndroidId));
                // tlvs.PushTlv(Tlv.194());
                tlvs.PutTlv(new T191());
                tlvs.PutTlv(new T202(DeviceInfo.Network.Wifi.ApMacAddress, DeviceInfo.Network.Wifi.Ssid));
                tlvs.PutTlv(new T177(AppInfo.WtLoginSdk.buildTime, AppInfo.WtLoginSdk.sdkVersion));
                tlvs.PutTlv(new T516());
                tlvs.PutTlv(new T521());
                tlvs.PutTlv(new T525(new T536(new byte[] { 0x01, 0x00 })));
                // tlvs.PushTlv(new T544());
                // tlvs.PushTlv(new T545());

                PutUshortBE(subCmd);
                PutBytes(tlvs.GetBytes(true));
            }
        }
    }
}
