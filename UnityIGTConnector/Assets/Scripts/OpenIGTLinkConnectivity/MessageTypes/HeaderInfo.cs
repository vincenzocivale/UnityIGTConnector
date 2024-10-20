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
    public static HeaderInfo ReadHeaderInfo(byte[] byteArray)
    {
        if (byteArray == null || byteArray.Length < 28) // Assicurati che ci siano abbastanza byte
            throw new ArgumentException("Invalid byte array length.");

        HeaderInfo headerInfo = new HeaderInfo();

        // Utilizzo di Buffer.BlockCopy per copiare i byte nelle proprietà numeriche
        headerInfo.VersionNumber = BitConverter.ToUInt16(byteArray, 0);
        headerInfo.MsgType = Encoding.ASCII.GetString(byteArray, 2, 10).Trim('\0'); // 10 byte per il tipo di messaggio
        headerInfo.DeviceName = Encoding.ASCII.GetString(byteArray, 12, 10).Trim('\0'); // 10 byte per il nome del dispositivo
        headerInfo.Timestamp = BitConverter.ToUInt64(byteArray, 22);
        headerInfo.BodySize = BitConverter.ToUInt64(byteArray, 30);
        headerInfo.Crc64 = BitConverter.ToUInt64(byteArray, 38);
        headerInfo.ExtHeaderSize = BitConverter.ToUInt16(byteArray, 46);

        return headerInfo;
    }

}

