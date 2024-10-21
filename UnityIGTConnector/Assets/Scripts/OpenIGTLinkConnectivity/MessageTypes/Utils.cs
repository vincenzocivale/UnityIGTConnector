using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ByteReader
{
    public static UInt16 ReadUInt16(byte[] data, int offset)
    {
        if (BitConverter.IsLittleEndian) Array.Reverse(data, offset, 2);
        return BitConverter.ToUInt16(data, offset);
    }

    public static UInt32 ReadUInt32(byte[] data, int offset)
    {
        if (BitConverter.IsLittleEndian) Array.Reverse(data, offset, 4);
        return BitConverter.ToUInt32(data, offset);
    }

    public static float ReadFloat(byte[] data, int offset)
    {
        if (BitConverter.IsLittleEndian) Array.Reverse(data, offset, 4);
        return BitConverter.ToSingle(data, offset);
    }

    public static uint[] ReadUIntArray(byte[] data, uint size, ref int offset)
    {
        int count = (int)size / sizeof(uint);
        uint[] array = new uint[count];

        for (int i = 0; i < count; i++)
        {
            array[i] = ReadUInt32(data, offset);
            offset += sizeof(uint);
        }

        return array;
    }
}

