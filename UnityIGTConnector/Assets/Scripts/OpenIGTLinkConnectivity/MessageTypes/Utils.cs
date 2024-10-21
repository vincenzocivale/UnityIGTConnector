using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ByteReader
{
    public static UInt16 ReadUInt16(byte[] data, int offset)
    {
        byte[] buffer = new byte[2];
        Array.Copy(data, offset, buffer, 0, 2);

        if (BitConverter.IsLittleEndian) Array.Reverse(buffer);

        return BitConverter.ToUInt16(buffer, 0);
    }

    public static UInt32 ReadUInt32(byte[] data, int offset)
    {
        byte[] buffer = new byte[4];
        Array.Copy(data, offset, buffer, 0, 4);

        if (BitConverter.IsLittleEndian) Array.Reverse(buffer);

        return BitConverter.ToUInt32(buffer, 0);
    }

    public static float ReadFloat(byte[] data, int offset)
    {
        byte[] buffer = new byte[4];
        Array.Copy(data, offset, buffer, 0, 4);

        if (BitConverter.IsLittleEndian) Array.Reverse(buffer);

        return BitConverter.ToSingle(buffer, 0);
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

