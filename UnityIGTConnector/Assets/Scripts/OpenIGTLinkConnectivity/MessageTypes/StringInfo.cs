using System;
using System.Text;

public struct StringInfo
{
    public ushort encoding;
    public ushort length;
    public byte[] data;
    public string text;
}

public static class StringInfoReader
{
    public static StringInfo ReadStringInfo(byte[] messageBytes, uint headerSize, UInt16 extHeaderSize)
    {
        StringInfo stringInfo = new StringInfo();
        int offset = (int)(headerSize + extHeaderSize);

        // Leggi il formato di encoding e la lunghezza
        stringInfo.encoding = ByteReader.ReadUInt16(messageBytes, offset);
        stringInfo.length = ByteReader.ReadUInt16(messageBytes, offset + 2);

        // Crea un array di byte per i dati della stringa
        stringInfo.data = new byte[stringInfo.length];
        Buffer.BlockCopy(messageBytes, offset + 4, stringInfo.data, 0, stringInfo.length);

        // Decodifica i dati in base al tipo di encoding
        // Reference: https://www.iana.org/assignments/character-sets/character-sets.xhtml
        switch (stringInfo.encoding)
        {
            case 1:
            case 3:
                // ASCII
                stringInfo.text = Encoding.ASCII.GetString(stringInfo.data);
                break;
            case 106:
                // UTF-8
                stringInfo.text = Encoding.UTF8.GetString(stringInfo.data);
                break;
            default:
                // Se l'encoding non è supportato
                UnityEngine.Debug.LogError("Text format not implemented: " + stringInfo.encoding);
                stringInfo.text = string.Empty;  // Puoi restituire una stringa vuota se encoding non è supportato
                break;
        }

        return stringInfo;
    }

}
