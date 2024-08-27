using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityVolumeRendering
{
    [Serializable]
    public enum DataContentFormat
    {
        Int8,
        Uint8,
        Int16,
        Uint16,
        Int32,
        Uint32,
        Float32,
        Float64
    }

    [Serializable]
    public enum Endianness
    {
        LittleEndian,
        BigEndian
    }

    public class OpenIGTImporter
    {
        string filePath;
        private int dimX;
        private int dimY;
        private int dimZ;
        private DataContentFormat contentFormat;
        private Endianness endianness;
        private int skipBytes;
        public OpenIGTImporter(byte[] iMSGbyteArrayComplete, ReadMessageFromServer.ImageInfo imageInfo)
        {
            this.filePath = null;
            this.dimX = (int)imageInfo.NumPixSVX;
            this.dimY = (int)imageInfo.NumPixSVY;
            this.dimZ = (int)imageInfo.NumPixSVZ;
            this.skipBytes = imageInfo.OffsetBeforeImageContent;
            switch (imageInfo.ScalarType)
            {
                case 2:
                    this.contentFormat = DataContentFormat.Int8;
                    break;
                case 3: // uint8
                    this.contentFormat = DataContentFormat.Uint8;
                    break;
                case 4: // int16
                    this.contentFormat = DataContentFormat.Int16;
                    break;
                case 5: // uint16
                    this.contentFormat = DataContentFormat.Uint16;
                    break;
                case 6: // int32
                    this.contentFormat = DataContentFormat.Int32;
                    break;
                case 7: // uint32
                    this.contentFormat = DataContentFormat.Uint32;
                    break;
                case 10: // float32
                    this.contentFormat = DataContentFormat.Float32;
                    break;
                case 11: // float64
                    this.contentFormat = DataContentFormat.Float64;
                    break;
                default:
                    throw new Exception("ScalarType non supportato");
            }

            if (imageInfo.Endian == 1)
            {
                this.endianness = Endianness.BigEndian;
            }
            else
            {
                if (imageInfo.Endian == 2)
                {
                    this.endianness = Endianness.LittleEndian;
                }
            }
        }

        public VolumeDataset Import(byte[] iMSGbyteArrayComplete, ReadMessageFromServer.ImageInfo imageInfo)
        {
            if (imageInfo.ImComp != 1)
            {
                UnityEngine.Debug.LogError("Non è possibile effettuare volume rendering per immagini con rgb");
            }
            VolumeDataset dataset = ScriptableObject.CreateInstance<VolumeDataset>();
            ImportInternal(dataset, iMSGbyteArrayComplete, imageInfo);

            return dataset;
        }

        public async Task<VolumeDataset> ImportAsync(byte[] iMSGbyteArrayComplete, ReadMessageFromServer.ImageInfo imageInfo)
        {
            VolumeDataset dataset = ScriptableObject.CreateInstance<VolumeDataset>();

            await Task.Run(() => ImportInternal(dataset, iMSGbyteArrayComplete, imageInfo));

            return dataset;
        }

        private void ImportInternal(VolumeDataset dataset, byte[] iMSGbyteArrayComplete, ReadMessageFromServer.ImageInfo imageInfo)
        {
            dataset.datasetName = null;
            dataset.filePath = filePath;
            dataset.dimX = dimX;
            dataset.dimY = dimY;
            dataset.dimZ = dimZ;

            int bytesPerScalar = imageInfo.ScalarType switch
            {
                2 => 1,
                3 => 1,
                4 => 2,
                5 => 2,
                6 => 4,
                7 => 4,
                10 => 4,
                11 => 8,
                _ => throw new Exception("ScalarType non supportato")
            };


            int uDimension = dimX * dimY * dimZ;
            dataset.data = new float[uDimension];

            byte[] imageData = new byte[dimX * dimY * dimZ * bytesPerScalar];
            Buffer.BlockCopy(iMSGbyteArrayComplete, imageInfo.OffsetBeforeImageContent, imageData, 0, uDimension);

            int index = 0;
            int imageDataIndex = 0;
            for (int z = 0; z < dimZ; z++)
            {
                for (int y = 0; y < dimY; y++)
                {
                    for (int x = 0; x < dimX; x++)
                    {
                        int flippedIndex = x + (dimY - 1 - y) * dimX + z * dimX * dimY;
                        dataset.data[flippedIndex] = ConvertScalarToFloat(imageData, imageDataIndex, bytesPerScalar, imageInfo.ScalarType);
                        index++;
                        imageDataIndex += bytesPerScalar;
                    }
                }
            }


            //SaveFloatArrayToCsv(dataset.data, "Assets/value.csv");

            dataset.FixDimensions();
            //dataset.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
        }

        private static float ConvertScalarToFloat(byte[] data, int startIndex, int bytesPerScalar, int scalarType)
        {
            switch (scalarType)
            {
                case 2: // int8
                    return ((float)data[startIndex] / 127f) * 256f; // Normalizza rispetto al massimo valore di int8 (127) e scala a 256
                case 3: // uint8
                    return ((float)data[startIndex] / 255f) * 256f; // Normalizza rispetto al massimo valore di uint8 (255) e scala a 256
                case 4: // int16
                    return ((float)BitConverter.ToInt16(data, startIndex) / 32767f) * 256f; // Normalizza rispetto al massimo valore di int16 (32767) e scala a 256
                case 5: // uint16
                    return ((float)BitConverter.ToUInt16(data, startIndex) / 65535f) * 256f; // Normalizza rispetto al massimo valore di uint16 (65535) e scala a 256
                case 6: // int32
                    return ((float)BitConverter.ToInt32(data, startIndex) / 2147483647f) * 256f; // Normalizza rispetto al massimo valore di int32 (2147483647) e scala a 256
                case 7: // uint32
                    return ((float)BitConverter.ToUInt32(data, startIndex) / 4294967295f) * 256f; // Normalizza rispetto al massimo valore di uint32 (4294967295) e scala a 256
                case 10: // float32
                    return BitConverter.ToSingle(data, startIndex) * 256f; // float32 è già normalizzato (assumendo sia tra 0 e 1), scala a 256
                case 11: // float64
                    return (float)(BitConverter.ToDouble(data, startIndex) * 256f); // float64 è già normalizzato (assumendo sia tra 0 e 1), scala a 256
                default:
                    throw new Exception("ScalarType non supportato");
            }
        }
    }
}
