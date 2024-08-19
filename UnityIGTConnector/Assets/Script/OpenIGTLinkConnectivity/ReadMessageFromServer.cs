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

    // Read incoming message's header
    public static HeaderInfo ReadHeaderInfo(byte[] messageBytes)
    {
        HeaderInfo header = new HeaderInfo
        {
            VersionNumber = ReadUInt16(messageBytes, 0),
            MsgType = Encoding.ASCII.GetString(messageBytes, 2, 12),
            DeviceName = Encoding.ASCII.GetString(messageBytes, 14, 20),
            Timestamp = ReadUInt64(messageBytes, 34),
            BodySize = ReadUInt64(messageBytes, 42),
            Crc64 = ReadUInt64(messageBytes, 50),
            ExtHeaderSize = ReadUInt16(messageBytes, 58)
        };

        return header;
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
        public float Xi, Yi, Zi;
        public float Xj, Yj, Zj;
        public float Xk, Yk, Zk;
        public float CenterPosX, CenterPosY, CenterPosZ;
        public UInt16 StartingIndexSVX;
        public UInt16 StartingIndexSVY;
        public UInt16 StartingIndexSVZ;
        public UInt16 NumPixSVX;
        public UInt16 NumPixSVY;
        public UInt16 NumPixSVZ;
        public int OffsetBeforeImageContent;
    }

    // Read incoming image's information
    public static ImageInfo ReadImageInfo(byte[] messageBytes, uint headerSize, UInt16 extHeaderSize)
    {
        ImageInfo imageInfo = new ImageInfo();
        int offset = (int)(headerSize + extHeaderSize - 2);

        imageInfo.VersionNumber = ReadUInt16(messageBytes, offset);
        imageInfo.ImComp = messageBytes[offset + 2];
        imageInfo.ScalarType = messageBytes[offset + 3];
        imageInfo.Endian = messageBytes[offset + 4];
        imageInfo.ImCoord = messageBytes[offset + 5];
        imageInfo.NumPixX = ReadUInt16(messageBytes, offset + 6);
        imageInfo.NumPixY = ReadUInt16(messageBytes, offset + 8);
        imageInfo.NumPixZ = ReadUInt16(messageBytes, offset + 10);
        imageInfo.Xi = ReadFloat(messageBytes, offset + 12);
        imageInfo.Yi = ReadFloat(messageBytes, offset + 16);
        imageInfo.Zi = ReadFloat(messageBytes, offset + 20);
        imageInfo.Xj = ReadFloat(messageBytes, offset + 24);
        imageInfo.Yj = ReadFloat(messageBytes, offset + 28);
        imageInfo.Zj = ReadFloat(messageBytes, offset + 32);
        imageInfo.Xk = ReadFloat(messageBytes, offset + 36);
        imageInfo.Yk = ReadFloat(messageBytes, offset + 40);
        imageInfo.Zk = ReadFloat(messageBytes, offset + 44);
        imageInfo.CenterPosX = ReadFloat(messageBytes, offset + 48);
        imageInfo.CenterPosY = ReadFloat(messageBytes, offset + 52);
        imageInfo.CenterPosZ = ReadFloat(messageBytes, offset + 56);
        imageInfo.StartingIndexSVX = ReadUInt16(messageBytes, offset + 60);
        imageInfo.StartingIndexSVY = ReadUInt16(messageBytes, offset + 62);
        imageInfo.StartingIndexSVZ = ReadUInt16(messageBytes, offset + 64);
        imageInfo.NumPixSVX = ReadUInt16(messageBytes, offset + 66);
        imageInfo.NumPixSVY = ReadUInt16(messageBytes, offset + 68);
        imageInfo.NumPixSVZ = ReadUInt16(messageBytes, offset + 70);
        imageInfo.OffsetBeforeImageContent = offset;

        return imageInfo;
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

    //////////////////////////////// INCOMING POLYDATA MESSAGE ////////////////////////////////
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

/*        UnityEngine.Debug.Log($"Number of Points: {polyDataInfo.NumPoints}");
        UnityEngine.Debug.Log($"Number of Vertices: {polyDataInfo.NumVertices}");
        UnityEngine.Debug.Log($"Size of Vertices Data: {polyDataInfo.SizeVertices}");
        UnityEngine.Debug.Log($"Number of Lines: {polyDataInfo.NumLines}");
        UnityEngine.Debug.Log($"Size of Lines Data: {polyDataInfo.SizeLines}");
        UnityEngine.Debug.Log($"Number of Polygons: {polyDataInfo.NumPolygons}");
        UnityEngine.Debug.Log($"Size of Polygons Data: {polyDataInfo.SizePolygons}");
        UnityEngine.Debug.Log($"Number of Triangle Strips: {polyDataInfo.NumTriangleStrips}");
        UnityEngine.Debug.Log($"Size of Triangle Strips Data: {polyDataInfo.SizeTriangleStrips}");
        UnityEngine.Debug.Log($"Number of Attributes: {polyDataInfo.NumAttributes}");*/

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

/*        // Stampa le liste
        UnityEngine.Debug.Log("Vertices:");
        foreach (var vertex in polyDataInfo.Vertices)
        {
            UnityEngine.Debug.Log(vertex);
        }

        UnityEngine.Debug.Log("Lines:");
        foreach (var line in polyDataInfo.Lines)
        {
            UnityEngine.Debug.Log(line);
        }

        UnityEngine.Debug.Log("Polygons:");
        foreach (var polygon in polyDataInfo.Polygons)
        {
            UnityEngine.Debug.Log(polygon);
        }

        UnityEngine.Debug.Log("Triangle Strips:");
        foreach (var strip in polyDataInfo.TriangleStrips)
        {
            UnityEngine.Debug.Log(strip);
        }*/

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

        UnityEngine.Debug.Log("Number of attributes is: " + numAttributes);

        for (int i = 0; i < numAttributes; i++)
        {
            attributeHeader.TypeAttributes[i] = ReadUInt16(messageBytes, offset + i * 6);
            attributeHeader.NAttributes[i] = ReadUInt32(messageBytes, offset + i * 6 + 2);
            UnityEngine.Debug.Log("Attribute type is: " + attributeHeader.GetAttributeType(i));
            UnityEngine.Debug.Log("Attribute number of components is: " + attributeHeader.GetNumberOfComponents(i));
            UnityEngine.Debug.Log("Number of components: " + attributeHeader.NAttributes[i]);

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
