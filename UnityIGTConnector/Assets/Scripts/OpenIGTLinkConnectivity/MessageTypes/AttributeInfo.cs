using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attributes
{
    public AttributeHeader Header { get; private set; }
    public AttributeNames Names { get; private set; }
    public AttributeData Data { get; private set; }

    public Attributes(AttributeHeader header, AttributeNames names, AttributeData data)
    {
        Header = header ?? throw new ArgumentNullException(nameof(header));
        Names = names ?? throw new ArgumentNullException(nameof(names));
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }


    public static Attributes ReadFromBytes(byte[] data, int offset, uint numberOfAttributes)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (numberOfAttributes == 0) throw new ArgumentException("Number of attributes cannot be zero.", nameof(numberOfAttributes));

        var header = AttributeHeader.ReadFromBytes(data, ref offset, numberOfAttributes);
        var names = AttributeNames.ReadFromBytes(data, ref offset, numberOfAttributes);
        var dataList = AttributeData.ReadFromBytes(data, ref offset, header, numberOfAttributes);

        return new Attributes(header, names, dataList);
    }
}

public class AttributeHeader
{
    public AttributeType[] Types { get; private set; }
    public uint[] Counts { get; private set; }

    public AttributeHeader(AttributeType[] types, uint[] counts)
    {
        Types = types ?? throw new ArgumentNullException(nameof(types));
        Counts = counts ?? throw new ArgumentNullException(nameof(counts));
    }

    public static AttributeHeader ReadFromBytes(byte[] data, ref int offset, uint numAttributes)
    {
        var types = new AttributeType[numAttributes];
        var counts = new uint[numAttributes];

        for (int i = 0; i < numAttributes; i++)
        {
            types[i] = AttributeType.FromByteArray(data, offset);
            offset += 2;

            counts[i] = ByteReader.ReadUInt32(data, offset);
            offset += 4;
        }

        return new AttributeHeader(types, counts);
    }

    public int GetComponentCount(int index) => Types[index].NumberOfComponents;

    public AttributeType.AttributeTypeValue GetAttributeType(int index) => Types[index].Type;
}

public class AttributeType
{
    private const ushort TypeMask = 0x00FF;
    private const ushort ComponentMask = 0xFF00;

    public enum AttributeTypeValue : byte
    {
        PointData_Scalars = 0x00,
        PointData_Vectors = 0x01,
        PointData_Normals = 0x02,
        PointData_Tensors = 0x03,
        PointData_RGBA = 0x04,
        CellData_Scalars = 0x10,
        CellData_Vectors = 0x11,
        CellData_Normals = 0x12,
        CellData_Tensors = 0x13,
        CellData_RGBA = 0x14
    }

    public AttributeTypeValue Type { get; private set; }
    public byte NumberOfComponents { get; private set; }

    public AttributeType(ushort value)
    {
        Type = (AttributeTypeValue)(value & TypeMask);
        NumberOfComponents = (byte)((value & ComponentMask) >> 8);
        ValidateComponents();
    }

    private void ValidateComponents()
    {
        if ((Type == AttributeTypeValue.PointData_Vectors || Type == AttributeTypeValue.PointData_Normals) && NumberOfComponents != 3)
        {
            throw new ArgumentOutOfRangeException(nameof(NumberOfComponents), "Vectors and Normals must have 3 components.");
        }
        else if (Type == AttributeTypeValue.PointData_Tensors && NumberOfComponents != 9)
        {
            throw new ArgumentOutOfRangeException(nameof(NumberOfComponents), "Tensors must have 9 components.");
        }
        else if (Type == AttributeTypeValue.PointData_RGBA && NumberOfComponents != 4)
        {
            throw new ArgumentOutOfRangeException(nameof(NumberOfComponents), "RGBA must have 4 components.");
        }
    }

    public static AttributeType FromByteArray(byte[] data, int offset)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (offset < 0 || offset + 2 > data.Length) throw new ArgumentOutOfRangeException(nameof(offset));

        ushort value = (ushort)((data[offset + 1] << 8) | data[offset]);
        return new AttributeType(value);
    }
}

public class AttributeNames
{
    public List<string> Names { get; private set; }

    public AttributeNames(List<string> names)
    {
        Names = names ?? throw new ArgumentNullException(nameof(names));
    }

    public static AttributeNames ReadFromBytes(byte[] data, ref int offset, uint numAttributes)
    {
        var names = new List<string>();

        for (int i = 0; i < numAttributes; i++)
        {
            int start = offset;
            while (data[offset] != 0)
            {
                offset++;
            }

            var name = System.Text.Encoding.ASCII.GetString(data, start, offset - start);
            names.Add(name);
            offset++;  // Skip null terminator
        }

        if (offset % 2 != 0) offset++;  // Padding if needed

        return new AttributeNames(names);
    }
}

public class AttributeData
{
    public float[][] Data { get; private set; }

    public AttributeData(uint numAttributes, int[] sizes)
    {
        Data = new float[numAttributes][];
        for (int i = 0; i < numAttributes; i++)
        {
            Data[i] = new float[sizes[i]];
        }
    }

    public static AttributeData ReadFromBytes(byte[] data, ref int offset, AttributeHeader header, uint numAttributes)
    {
        var sizes = new int[numAttributes];

        for (int i = 0; i < numAttributes; i++)
        {
            sizes[i] = (int)(header.Counts[i] * header.GetComponentCount(i));
        }

        var attributeData = new AttributeData(numAttributes, sizes);

        for (int i = 0; i < numAttributes; i++)
        {
            for (int j = 0; j < sizes[i]; j++)
            {
                if (offset + 4 > data.Length)
                {
                    throw new IndexOutOfRangeException("Trying to read beyond the bounds of the byte array.");
                }

                attributeData.Data[i][j] = BitConverter.ToSingle(data, offset);
                offset += 4;
            }
        }

        return attributeData;
    }
}

