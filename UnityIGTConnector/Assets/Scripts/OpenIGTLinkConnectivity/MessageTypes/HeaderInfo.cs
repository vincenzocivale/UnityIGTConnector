using System;
using System.Text;

public class HeaderInfo
{
    public uint headerSize = 58;
    public UInt16 VersionNumber { get; set; }
    public string MsgType { get; set; }
    public string DeviceName { get; set; }
    public UInt64 Timestamp { get; set; }
    public UInt64 BodySize { get; set; }
    public UInt64 Crc64 { get; set; }
    public UInt16 ExtHeaderSize { get; set; }

    // Metodo statico per creare HeaderInfo da un array di byte
    public static HeaderInfo ReadHeaderInfo(byte[] iMSGbyteArray)
    {
        // Definisci la dimensione di ciascuna componente dell'header
        byte[] byteArray_Version = new byte[2];
        byte[] byteArray_MsgType = new byte[12];
        byte[] byteArray_DeviceName = new byte[20];
        byte[] byteArray_TimeStamp = new byte[8];
        byte[] byteArray_BodySize = new byte[8];
        byte[] byteArray_CRC = new byte[8];
        byte[] byteArray_ExtHeaderSize = new byte[2];

        // Definisci l'offset per saltare i byte necessari e raggiungere la variabile successiva
        int version_SP = 0;
        int msgType_SP = version_SP + byteArray_Version.Length;
        int deviceName_SP = msgType_SP + byteArray_MsgType.Length;
        int timeStamp_SP = deviceName_SP + byteArray_DeviceName.Length;
        int bodySize_SP = timeStamp_SP + byteArray_TimeStamp.Length;
        int crc_SP = bodySize_SP + byteArray_BodySize.Length;
        int extHeaderSize_SP = crc_SP + byteArray_CRC.Length;

        // Memorizza le informazioni nelle variabili
        Buffer.BlockCopy(iMSGbyteArray, version_SP, byteArray_Version, 0, byteArray_Version.Length);
        Buffer.BlockCopy(iMSGbyteArray, msgType_SP, byteArray_MsgType, 0, byteArray_MsgType.Length);
        Buffer.BlockCopy(iMSGbyteArray, deviceName_SP, byteArray_DeviceName, 0, byteArray_DeviceName.Length);
        Buffer.BlockCopy(iMSGbyteArray, timeStamp_SP, byteArray_TimeStamp, 0, byteArray_TimeStamp.Length);
        Buffer.BlockCopy(iMSGbyteArray, bodySize_SP, byteArray_BodySize, 0, byteArray_BodySize.Length);
        Buffer.BlockCopy(iMSGbyteArray, crc_SP, byteArray_CRC, 0, byteArray_CRC.Length);
        Buffer.BlockCopy(iMSGbyteArray, extHeaderSize_SP, byteArray_ExtHeaderSize, 0, byteArray_ExtHeaderSize.Length);

        // Se il messaggio è Little Endian, convertilo in Big Endian
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(byteArray_Version);
            Array.Reverse(byteArray_TimeStamp);
            Array.Reverse(byteArray_BodySize);
            Array.Reverse(byteArray_CRC);
            Array.Reverse(byteArray_ExtHeaderSize);
        }

        // Converte gli array di byte nel tipo di dati corrispondente
        UInt16 versionNumber_iMSG = BitConverter.ToUInt16(byteArray_Version, 0);
        string msgType_iMSG = Encoding.ASCII.GetString(byteArray_MsgType).TrimEnd('\0'); // Rimuove caratteri nulli
        string deviceName_iMSG = Encoding.ASCII.GetString(byteArray_DeviceName).TrimEnd('\0'); // Rimuove caratteri nulli
        UInt64 timestamp_iMSG = BitConverter.ToUInt64(byteArray_TimeStamp, 0);
        UInt64 bodySize_iMSG = BitConverter.ToUInt64(byteArray_BodySize, 0);
        UInt64 crc_iMSG = BitConverter.ToUInt64(byteArray_CRC, 0);
        UInt16 extHeaderSize_iMSG = BitConverter.ToUInt16(byteArray_ExtHeaderSize, 0);

        // Memorizza tutti i valori nella struttura HeaderInfo
        HeaderInfo incomingHeaderInfo = new HeaderInfo
        {
            VersionNumber = versionNumber_iMSG,
            MsgType = msgType_iMSG,
            DeviceName = deviceName_iMSG,
            Timestamp = timestamp_iMSG,
            BodySize = bodySize_iMSG,
            Crc64 = crc_iMSG,
            ExtHeaderSize = extHeaderSize_iMSG
        };

        return incomingHeaderInfo;
    }

    public void PrintDebugInfo()
    {
        UnityEngine.Debug.Log("Header Information:");
        UnityEngine.Debug.Log("Version Number: " + VersionNumber);
        UnityEngine.Debug.Log("Message Type: " + MsgType);
        UnityEngine.Debug.Log("Device Name: " + DeviceName);
        UnityEngine.Debug.Log("Timestamp: " + Timestamp);
        UnityEngine.Debug.Log("Body Size: " + BodySize);
        UnityEngine.Debug.Log("CRC64: " + Crc64.ToString("X16")); // CRC64 in formato esadecimale
        UnityEngine.Debug.Log("Extended Header Size: " + ExtHeaderSize);
    }

}

