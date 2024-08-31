using UnityEngine;
using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

public class ReadMessageFromServer
{
    // Costanti per la dimensione dei dati
    private const int HEADER_SIZE = 58;
    private const int MATRIX_SIZE = 16;
    private const int VECTOR3_SIZE = 12;
    private const int UINT_SIZE = 4;

    // Static Matrix4x4
    private static readonly Matrix4x4 matrix = new Matrix4x4();

    //////////////////////////////// READING INCOMING MESSAGE ////////////////////////////////
    // Structure to store header information
    public struct HeaderInfo
    {
        public UInt16 VersionNumber;
        public string MsgType;
        public string DeviceName;
        public UInt64 Timestamp;
        public UInt64 BodySize;
        public UInt64 Crc64;
        public UInt16 ExtHeaderSize;
    }

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


    //////////////////////////////// INCOMING IMAGE MESSAGE ////////////////////////////////
    public struct ImageInfo
    {
        public UInt16 VersionNumber;
        public int ImComp;
        public int ScalarType;
        public int Endian;
        public int ImCoord;
        public UInt16 NumPixX;
        public UInt16 NumPixY;
        public UInt16 NumPixZ;
        public float Xi;
        public float Yi;
        public float Zi;
        public float Xj;
        public float Yj;
        public float Zj;
        public float Xk;
        public float Yk;
        public float Zk;
        public float CenterPosX;
        public float CenterPosY;
        public float CenterPosZ;
        public UInt16 StartingIndexSVX;
        public UInt16 StartingIndexSVY;
        public UInt16 StartingIndexSVZ;
        public UInt16 NumPixSVX;
        public UInt16 NumPixSVY;
        public UInt16 NumPixSVZ;
        public int OffsetBeforeImageContent;
    }

    // Read incoming image's information
    public static ImageInfo ReadImageInfo(byte[] iMSGbyteArrayComplete, uint headerSize, UInt16 extHeaderSize_iMSG)
    {
        // Define the variables stored in the body of the message
        int[] bodyArrayLengths = new int[] { 2, 1, 1, 1, 1, 2, 2, 2, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 2, 2, 2, 2, 2, 2 };

        ImageInfo incomingImageInfo = new ImageInfo();

        int skipTheseBytes = (int)headerSize + (int)extHeaderSize_iMSG;

        for (int index = 0; index < bodyArrayLengths.Length; index++)
        {
            byte[] sectionByteArray = new byte[bodyArrayLengths[index]];
            Buffer.BlockCopy(iMSGbyteArrayComplete, skipTheseBytes, sectionByteArray, 0, bodyArrayLengths[index]);
            skipTheseBytes += bodyArrayLengths[index];

            if (BitConverter.IsLittleEndian && (bodyArrayLengths[index] > 1))
            {
                Array.Reverse(sectionByteArray);
            }

            switch (index)
            {
                case 0:
                    incomingImageInfo.VersionNumber = BitConverter.ToUInt16(sectionByteArray, 0);
                    break;
                case 1:
                    incomingImageInfo.ImComp = sectionByteArray[0];
                    break;
                case 2:
                    incomingImageInfo.ScalarType = sectionByteArray[0];
                    break;
                case 3:
                    incomingImageInfo.Endian = sectionByteArray[0];
                    break;
                case 4:
                    incomingImageInfo.ImCoord = sectionByteArray[0];
                    break;
                case 5:
                    incomingImageInfo.NumPixX = BitConverter.ToUInt16(sectionByteArray, 0);
                    break;
                case 6:
                    incomingImageInfo.NumPixY = BitConverter.ToUInt16(sectionByteArray, 0);
                    break;
                case 7:
                    incomingImageInfo.NumPixZ = BitConverter.ToUInt16(sectionByteArray, 0);
                    break;
                case 8:
                    incomingImageInfo.Xi = BitConverter.ToSingle(sectionByteArray, 0);
                    break;
                case 9:
                    incomingImageInfo.Yi = BitConverter.ToSingle(sectionByteArray, 0);
                    break;
                case 10:
                    incomingImageInfo.Zi = BitConverter.ToSingle(sectionByteArray, 0);
                    break;
                case 11:
                    incomingImageInfo.Xj = BitConverter.ToSingle(sectionByteArray, 0);
                    break;
                case 12:
                    incomingImageInfo.Yj = BitConverter.ToSingle(sectionByteArray, 0);
                    break;
                case 13:
                    incomingImageInfo.Zj = BitConverter.ToSingle(sectionByteArray, 0);
                    break;
                case 14:
                    incomingImageInfo.Xk = BitConverter.ToSingle(sectionByteArray, 0);
                    break;
                case 15:
                    incomingImageInfo.Yk = BitConverter.ToSingle(sectionByteArray, 0);
                    break;
                case 16:
                    incomingImageInfo.Zk = BitConverter.ToSingle(sectionByteArray, 0);
                    break;
                case 17:
                    incomingImageInfo.CenterPosX = BitConverter.ToSingle(sectionByteArray, 0);
                    break;
                case 18:
                    incomingImageInfo.CenterPosY = BitConverter.ToSingle(sectionByteArray, 0);
                    break;
                case 19:
                    incomingImageInfo.CenterPosZ = BitConverter.ToSingle(sectionByteArray, 0);
                    break;
                case 20:
                    incomingImageInfo.StartingIndexSVX = BitConverter.ToUInt16(sectionByteArray, 0);
                    break;
                case 21:
                    incomingImageInfo.StartingIndexSVY = BitConverter.ToUInt16(sectionByteArray, 0);
                    break;
                case 22:
                    incomingImageInfo.StartingIndexSVZ = BitConverter.ToUInt16(sectionByteArray, 0);
                    break;
                case 23:
                    incomingImageInfo.NumPixSVX = BitConverter.ToUInt16(sectionByteArray, 0);
                    break;
                case 24:
                    incomingImageInfo.NumPixSVY = BitConverter.ToUInt16(sectionByteArray, 0);
                    break;
                case 25:
                    incomingImageInfo.NumPixSVZ = BitConverter.ToUInt16(sectionByteArray, 0);
                    break;
                default:
                    break;
            }
        }

        incomingImageInfo.OffsetBeforeImageContent = skipTheseBytes;

        return incomingImageInfo;
    }


    //////////////////////////////// INCOMING TRANSFORM MESSAGE ////////////////////////////////
    // Extract transform information
    public static Matrix4x4 ExtractTransformInfo(byte[] messageBytes, GameObject go, int scaleMultiplier, uint headerSize)
    {
        float[] m = new float[MATRIX_SIZE];
        int offset = (int)headerSize + 12;

        for (int i = 0; i < MATRIX_SIZE; i++)
        {
            m[i] = ReadFloat(messageBytes, offset + i * 4);
        }

        matrix.SetRow(0, new Vector4(m[0], m[3], m[6], m[9] / scaleMultiplier));
        matrix.SetRow(1, new Vector4(m[1], m[4], m[7], m[10] / scaleMultiplier));
        matrix.SetRow(2, new Vector4(m[2], m[5], m[8], m[11] / scaleMultiplier));
        matrix.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

        return matrix;
    }

    //////////////////////////////// INCOMING STRING MESSAGE ////////////////////////////////
    
    public struct StringInfo
    {
        public ushort encoding;
        public ushort length;
        public byte[] data;
        public string text;
    }

    public static StringInfo ReadStringInfo(byte[] messageBytes, uint headerSize, UInt16 extHeaderSize)
    {
        StringInfo stringInfo = new StringInfo();
        int offset = (int)(headerSize + extHeaderSize);

        stringInfo.encoding = ReadUInt16(messageBytes, offset);
        stringInfo.length = ReadUInt16(messageBytes, offset + 2);

        stringInfo.data = new byte[stringInfo.length];

        Buffer.BlockCopy(messageBytes, offset + 4, stringInfo.data, 0, stringInfo.length);


        // Reference: https://www.iana.org/assignments/character-sets/character-sets.xhtml
        switch (stringInfo.encoding)
        {
            case 1:
                stringInfo.text = Encoding.ASCII.GetString(stringInfo.data); // da ricontrollare
                break;
            case 3:
                stringInfo.text = Encoding.ASCII.GetString(stringInfo.data);
                break;
            case 106:
                stringInfo.text = Encoding.UTF8.GetString(stringInfo.data);
                break;
            default:
                UnityEngine.Debug.LogError("Text format not implemented: " + stringInfo.encoding);
                stringInfo.text = null;
                break;
        }

        return stringInfo;
    }



    ///////////////////////////////// INCOMING POLYDATA MESSAGE ////////////////////////////////
    public struct PolyDataInfo
    {
        public uint NumPoints;
        public uint NumVertices;
        public uint SizeVertices;
        public uint NumLines;
        public uint SizeLines;
        public uint NumPolygons;
        public uint SizePolygons;
        public uint NumTriangleStrips;
        public uint SizeTriangleStrips;
        public uint NumAttributes;

        public Vector3[] Points;
        public uint[] Vertices;
        public uint[] Lines;
        public uint[] Polygons;
        public uint[] TriangleStrips;
        public AttributeHeader AttributeHeader;
        public AttributeNames AttributeNames;
        public AttributeData AttributeData;
    }

    public struct AttributeHeader
    {
        public ushort[] TypeAttributes;
        public uint[] NAttributes;

        public byte GetAttributeType(int index)
        {
            return (byte)(TypeAttributes[index] & 0x00FF);
        }

        public byte GetNumberOfComponents(int index)
        {
            return (byte)((TypeAttributes[index] >> 8) & 0x00FF);
        }

        public AttributeHeader(uint numAttributes)
        {
            TypeAttributes = new ushort[numAttributes];
            NAttributes = new uint[numAttributes];
        }
    }

    public struct AttributeNames
    {
        public string[] Names;
        public byte Padding;
    }

    public struct AttributeData
    {
        public float[][] Data;

        public AttributeData(uint numAttributes)
        {
            Data = new float[numAttributes][];
        }
    }


    public static PolyDataInfo ReadPolyDataInfo(byte[] messageBytes, uint headerSize, UInt16 extHeaderSize)
    {
        PolyDataInfo polyDataInfo = new PolyDataInfo();
        int offset = (int)(headerSize + extHeaderSize);

        polyDataInfo.NumPoints = ReadUInt32(messageBytes, offset);
        polyDataInfo.NumVertices = ReadUInt32(messageBytes, offset + 4);
        polyDataInfo.SizeVertices = ReadUInt32(messageBytes, offset + 8);
        polyDataInfo.NumLines = ReadUInt32(messageBytes, offset + 12);
        polyDataInfo.SizeLines = ReadUInt32(messageBytes, offset + 16);
        polyDataInfo.NumPolygons = ReadUInt32(messageBytes, offset + 20);
        polyDataInfo.SizePolygons = ReadUInt32(messageBytes, offset + 24);
        polyDataInfo.NumTriangleStrips = ReadUInt32(messageBytes, offset + 28);
        polyDataInfo.SizeTriangleStrips = ReadUInt32(messageBytes, offset + 32);
        polyDataInfo.NumAttributes = ReadUInt32(messageBytes, offset + 36);

        offset += 40; // Avanza dopo la lettura dei campi standard

        // Leggi i punti
        polyDataInfo.Points = new Vector3[polyDataInfo.NumPoints];
        for (int i = 0; i < polyDataInfo.NumPoints; i++)
        {
            float x = ReadFloat(messageBytes, offset);
            float y = ReadFloat(messageBytes, offset + 4);
            float z = ReadFloat(messageBytes, offset + 8);
            polyDataInfo.Points[i] = new Vector3(x, y, z);
            offset += VECTOR3_SIZE;
        }

        // Leggi le liste
        polyDataInfo.Vertices = ReadUIntArray(messageBytes, polyDataInfo.SizeVertices, ref offset);
        polyDataInfo.Lines = ReadUIntArray(messageBytes, polyDataInfo.SizeLines, ref offset);
        polyDataInfo.Polygons = ReadUIntArray(messageBytes, polyDataInfo.SizePolygons, ref offset);
        polyDataInfo.TriangleStrips = ReadUIntArray(messageBytes, polyDataInfo.SizeTriangleStrips, ref offset);

        // Leggi l'header degli attributi usando la nuova funzione
        polyDataInfo.AttributeHeader = ReadAttributeHeader(messageBytes, ref offset, polyDataInfo.NumAttributes);

        // Leggi i nomi degli attributi
        polyDataInfo.AttributeNames = ReadAttributeNames(messageBytes, ref offset, polyDataInfo.NumAttributes);

        // Leggi i dati degli attributi
        polyDataInfo.AttributeData = ReadAttributeData(messageBytes, ref offset, polyDataInfo.AttributeHeader);

        //PrintPolyDataAttributes(polyDataInfo);

        return polyDataInfo;
    }




    //////////////////////////////// METODI DI SUPPORTO ////////////////////////////////
    private static UInt16 ReadUInt16(byte[] data, int offset)
    {
        if (BitConverter.IsLittleEndian) Array.Reverse(data, offset, 2);
        return BitConverter.ToUInt16(data, offset);
    }

    private static UInt32 ReadUInt32(byte[] data, int offset)
    {
        if (BitConverter.IsLittleEndian) Array.Reverse(data, offset, 4);
        return BitConverter.ToUInt32(data, offset);
    }

    private static UInt64 ReadUInt64(byte[] data, int offset)
    {
        if (BitConverter.IsLittleEndian) Array.Reverse(data, offset, 8);
        return BitConverter.ToUInt64(data, offset);
    }

    private static float ReadFloat(byte[] data, int offset)
    {
        if (BitConverter.IsLittleEndian) Array.Reverse(data, offset, 4);
        return BitConverter.ToSingle(data, offset);
    }

    private static uint[] ReadUIntArray(byte[] data, uint size, ref int offset)
    {
        int count = (int)size / UINT_SIZE;
        uint[] array = new uint[count];

        for (int i = 0; i < count; i++)
        {
            array[i] = ReadUInt32(data, offset);
            offset += UINT_SIZE;
        }

        return array;
    }

    public static AttributeHeader ReadAttributeHeader(byte[] messageBytes, ref int offset, uint numAttributes)
    {
        AttributeHeader attributeHeader = new AttributeHeader(numAttributes);

        /*UnityEngine.Debug.Log("Number of attributes is: " + numAttributes);*/

        for (int i = 0; i < numAttributes; i++)
        {
            attributeHeader.TypeAttributes[i] = ReadUInt16(messageBytes, offset + i * 6);
            attributeHeader.NAttributes[i] = ReadUInt32(messageBytes, offset + i * 6 + 2);
            /*UnityEngine.Debug.Log("Attribute type is: " + attributeHeader.GetAttributeType(i));
            UnityEngine.Debug.Log("Attribute number of components is: " + attributeHeader.GetNumberOfComponents(i));
            UnityEngine.Debug.Log("Number of components: " + attributeHeader.NAttributes[i]);*/

            offset += 6;
        }

        return attributeHeader;
    }

    public static AttributeNames ReadAttributeNames(byte[] messageBytes, ref int offset, uint numAttributes)
    {
        AttributeNames attributeNames = new AttributeNames
        {
            Names = new string[numAttributes]
        };

        for (int i = 0; i < numAttributes; i++)
        {
            // Trova la lunghezza del nome dell'attributo terminato da carattere nullo
            int nameLength = 0;
            while (messageBytes[offset + nameLength] != 0)
            {
                nameLength++;
            }

            // Leggi il nome dell'attributo
            attributeNames.Names[i] = System.Text.Encoding.ASCII.GetString(messageBytes, offset, nameLength);

            // Aumenta l'offset per saltare il carattere nullo e fare il padding
            offset += nameLength + 1;  // +1 per il carattere nullo

            // Padding per allineare la dimensione a un numero pari di byte
            if ((nameLength + 1) % 2 != 0)
            {
                offset += 1;  // Padding per allineare la dimensione a un numero pari di byte
            }
        }

        return attributeNames;
    }


    public static AttributeData ReadAttributeData(byte[] messageBytes, ref int offset, AttributeHeader attributeHeader)
    {
        uint numAttributes = (uint)attributeHeader.TypeAttributes.Length;
        AttributeData attributeData = new AttributeData(numAttributes);

        for (int i = 0; i < numAttributes; i++)
        {
            int attributeSize = (int)(attributeHeader.NAttributes[i] * attributeHeader.GetNumberOfComponents(i));
            attributeData.Data[i] = new float[attributeSize];

            for (int j = 0; j < attributeSize; j++)
            {
                attributeData.Data[i][j] = ReadFloat(messageBytes, offset);
                offset += 4; // Float is 4 bytes
            }
        }

        return attributeData;
    }



}
