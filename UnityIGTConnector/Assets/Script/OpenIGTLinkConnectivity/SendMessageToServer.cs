using System.Linq;
using System.Text;
using System;
using UnityEngine;

public class SendMessageToServer : MonoBehaviour
{
    public static void SendMessage(ModelInfo model_InspectorInfo, int scaleMultiplier, CRC64 crcGenerator, string CRC, SocketHandler socketForUnityAndHoloLens, string messageType)
    {
        ////////////// Get myModel properties
        string modelName = model_InspectorInfo._name;
        GameObject modelGO = model_InspectorInfo._gameObject;
        string numberOfScrews = (GameObject.FindGameObjectsWithTag("Screw").Length).ToString();
        string modelNumber = model_InspectorInfo._number.ToString();
        string modelColor = model_InspectorInfo._color;
        string fileName;

        fileName = modelName;

        // Define Header in hexadecimal
        string deviceName = modelName + "_T";

        // Define the version in hexadecimal
        string oigtlVersion = "0002";

        string body = "";
        string hexExtHeader = "";
        string hexMetaBody = "";

        // Gestione del corpo del messaggio in base al tipo
        if (messageType == "TRANSFORM")
        {
            // Elements of the matrix
            string m00Hex;
            string m01Hex;
            string m02Hex;
            string m03Hex;
            string m10Hex;
            string m11Hex;
            string m12Hex;
            string m13Hex;
            string m20Hex;
            string m21Hex;
            string m22Hex;
            string m23Hex;

            // Get rotation of myOBJ and add a minus (-) sign to x axis to convert from Unity to Slicer coordinate system
            var myOBJRotation = modelGO.transform.localRotation.eulerAngles;
            var adaptedRotationFromDeviceToSlicer = new Vector3(-myOBJRotation.x, myOBJRotation.y, -myOBJRotation.z);
            var rotationForSlicer = Quaternion.Euler(adaptedRotationFromDeviceToSlicer);

            // Obtain a 4x4 matrix with all the pose information of myOBJ, including the minus (-) sign in the x axis of rotation
            Matrix4x4 matrix = Matrix4x4.TRS(modelGO.transform.localPosition, rotationForSlicer, modelGO.transform.localScale);

            float m00 = matrix.GetRow(0)[0];
            byte[] m00Bytes = BitConverter.GetBytes(m00);
            float m01 = matrix.GetRow(0)[1];
            byte[] m01Bytes = BitConverter.GetBytes(m01);
            float m02 = matrix.GetRow(0)[2];
            byte[] m02Bytes = BitConverter.GetBytes(m02);
            float m03 = matrix.GetRow(0)[3];
            byte[] m03Bytes = BitConverter.GetBytes(m03 * scaleMultiplier);

            float m10 = matrix.GetRow(1)[0];
            byte[] m10Bytes = BitConverter.GetBytes(m10);
            float m11 = matrix.GetRow(1)[1];
            byte[] m11Bytes = BitConverter.GetBytes(m11);
            float m12 = matrix.GetRow(1)[2];
            byte[] m12Bytes = BitConverter.GetBytes(m12);
            float m13 = -matrix.GetRow(1)[3];                   // (-) because of Unity coordinate system
            byte[] m13Bytes = BitConverter.GetBytes(m13 * scaleMultiplier);

            float m20 = matrix.GetRow(2)[0];
            byte[] m20Bytes = BitConverter.GetBytes(m20);
            float m21 = matrix.GetRow(2)[1];
            byte[] m21Bytes = BitConverter.GetBytes(m21);
            float m22 = matrix.GetRow(2)[2];
            byte[] m22Bytes = BitConverter.GetBytes(m22);
            float m23 = matrix.GetRow(2)[3];
            byte[] m23Bytes = BitConverter.GetBytes(m23 * scaleMultiplier);

            // If little endian, reverse the bytes
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(m00Bytes);
                Array.Reverse(m01Bytes);
                Array.Reverse(m02Bytes);
                Array.Reverse(m03Bytes);
                Array.Reverse(m10Bytes);
                Array.Reverse(m11Bytes);
                Array.Reverse(m12Bytes);
                Array.Reverse(m13Bytes);
                Array.Reverse(m20Bytes);
                Array.Reverse(m21Bytes);
                Array.Reverse(m22Bytes);
                Array.Reverse(m23Bytes);
            }

            // Convert bytes to hexadecimal
            m00Hex = BitConverter.ToString(m00Bytes).Replace("-", "");
            m01Hex = BitConverter.ToString(m01Bytes).Replace("-", "");
            m02Hex = BitConverter.ToString(m02Bytes).Replace("-", "");
            m03Hex = BitConverter.ToString(m03Bytes).Replace("-", "");
            m10Hex = BitConverter.ToString(m10Bytes).Replace("-", "");
            m11Hex = BitConverter.ToString(m11Bytes).Replace("-", "");
            m12Hex = BitConverter.ToString(m12Bytes).Replace("-", "");
            m13Hex = BitConverter.ToString(m13Bytes).Replace("-", "");
            m20Hex = BitConverter.ToString(m20Bytes).Replace("-", "");
            m21Hex = BitConverter.ToString(m21Bytes).Replace("-", "");
            m22Hex = BitConverter.ToString(m22Bytes).Replace("-", "");
            m23Hex = BitConverter.ToString(m23Bytes).Replace("-", "");

            // Create string with all the matrix elements
            body = m00Hex + m10Hex + m20Hex + m01Hex + m11Hex + m21Hex + m02Hex + m12Hex + m22Hex + m03Hex + m13Hex + m23Hex;

            // Create Metadata information
            string[] md_keyNames = { "ModelName", "ModelColor", "ModelNumber", "NumOfScrews" };
            string[] md_keyValues = { fileName, modelColor, modelNumber, numberOfScrews };

            // Get VALUE_ENCODING
            UInt16 encodingValue_UINT16 = Convert.ToUInt16(3);
            byte[] value_encoding_BYTES = BitConverter.GetBytes(encodingValue_UINT16);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(value_encoding_BYTES);
            }
            string VALUE_ENCODING = BitConverter.ToString(value_encoding_BYTES).Replace("-", "");

            // Create META_HEADER and META_DATA
            string META_HEADER = "";
            string META_DATA = "";
            for (int index = 0; index < md_keyNames.Length; index++)
            {
                byte[] currentKey_BYTES = Encoding.ASCII.GetBytes(md_keyNames[index]);
                byte[] currentValue_BYTES = Encoding.ASCII.GetBytes(md_keyValues[index]);

                // Build META_HEADER
                UInt16 currentValueLength_UINT16 = Convert.ToUInt16(currentKey_BYTES.Length);
                UInt32 currentValueLength_UINT32 = Convert.ToUInt16(currentValue_BYTES.Length);
                byte[] key_size_BYTES = BitConverter.GetBytes(currentValueLength_UINT16);
                byte[] value_size_BYTES = BitConverter.GetBytes(currentValueLength_UINT32);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(key_size_BYTES);
                    Array.Reverse(value_size_BYTES);
                }
                string KEY_SIZE = BitConverter.ToString(key_size_BYTES).Replace("-", "");
                string VALUE_SIZE = BitConverter.ToString(value_size_BYTES).Replace("-", "");
                META_HEADER += KEY_SIZE + VALUE_ENCODING + VALUE_SIZE;

                // Build META_DATA
                string KEY = BitConverter.ToString(currentKey_BYTES).Replace("-", "");
                string VALUE = BitConverter.ToString(currentValue_BYTES).Replace("-", "");
                META_DATA += KEY + VALUE;
            }

            // Get INDEX_COUNT
            UInt16 countIndexes_UINT16 = Convert.ToUInt16(md_keyNames.Count()); // Number of meta data
            byte[] index_count_BYTES = BitConverter.GetBytes(countIndexes_UINT16);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(index_count_BYTES);
            }
            string INDEX_COUNT = BitConverter.ToString(index_count_BYTES).Replace("-", "");

            // Complete the metadata information
            hexMetaBody = INDEX_COUNT + META_HEADER + META_DATA;

            // Build EXTENDED HEADER
            UInt16 extHeaderSize_UINT16 = Convert.ToUInt16(12);
            UInt16 metadataHeaderSize_UINT16 = Convert.ToUInt16((INDEX_COUNT.Length + META_HEADER.Length) / 2);
            UInt32 metadataSize_UINT32 = Convert.ToUInt32(META_DATA.Length / 2);
            UInt32 msgID_UINT32 = Convert.ToUInt32(0);

            byte[] extHeaderSize_BYTES = BitConverter.GetBytes(extHeaderSize_UINT16);
            byte[] metadataHeaderSize_BYTES = BitConverter.GetBytes(metadataHeaderSize_UINT16);
            byte[] metadataSize_BYTES = BitConverter.GetBytes(metadataSize_UINT32);
            byte[] msgID_BYTES = BitConverter.GetBytes(msgID_UINT32);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(extHeaderSize_BYTES);
                Array.Reverse(metadataHeaderSize_BYTES);
                Array.Reverse(metadataSize_BYTES);
                Array.Reverse(msgID_BYTES);
            }
            string EXT_HEADER_SIZE = BitConverter.ToString(extHeaderSize_BYTES).Replace("-", "");
            string META_HEADER_SIZE = BitConverter.ToString(metadataHeaderSize_BYTES).Replace("-", "");
            string META_SIZE = BitConverter.ToString(metadataSize_BYTES).Replace("-", "");
            string MSG_ID = BitConverter.ToString(msgID_BYTES).Replace("-", "");

            hexExtHeader = EXT_HEADER_SIZE + META_HEADER_SIZE + META_SIZE + MSG_ID;
        }

        string hexBodySize = (body.Length / 2).ToString("X").PadLeft(16, '0');
        string messageInHex = oigtlVersion + CRC + hexBodySize + hexExtHeader + hexMetaBody + body;

        socketForUnityAndHoloLens.Send(messageInHex);
    }
}
