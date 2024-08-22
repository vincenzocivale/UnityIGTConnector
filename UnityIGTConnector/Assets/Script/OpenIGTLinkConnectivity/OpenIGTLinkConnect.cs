// This code is based on the one provided in: https://github.com/franklinwk/OpenIGTLink-Unity
// Modified by Alicia Pose Díez de la Lastra, from Universidad Carlos III de Madrid

// OpenIGTLinkConnect: Gestisce la connessione e la comunicazione con 3D Slicer via OpenIGTLink.
// Start: Configura il CRC e le texture.
// OnConnectToSlicerClick e ConnectToSlicer: Gestiscono la connessione a Slicer.
// SendTransformInfo e ListenSlicerInfo: Gestiscono l'invio e la ricezione di messaggi.
// ApplyTransformToGameObject e ApplyImageInfo: Applicano trasformazioni e immagini agli oggetti di gioco.

using UnityEngine;
using System;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Collections;
using System.Threading;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Runtime;

using Microsoft.MixedReality.Toolkit.UI;
using System.Diagnostics;


public class OpenIGTLinkConnect : MonoBehaviour
{
    ///////// CONNECT TO 3D SLICER PARAMETERS /////////
    uint headerSize = 58; // Size of the header of every OpenIGTLink message
    private SocketHandler socketForUnityAndHoloLens; // Socket to connect to Slicer
    bool isConnected; // Boolean to check if the socket is connected
    public string ipString; // IP address of the computer running Slicer
    public int port; // Port of the computer running Slicer
    

    ///////// GENERAL VARIABLES /////////
    int scaleMultiplier = 1000; // Help variable to transform meters to millimeters and vice versa
    
       
    ///////// SEND /////////
    /*public List<ModelInfo> infoToSend; // Array of Models to send to Slicer*/
    
    /// CRC ECMA-182 to send messages to Slicer ///
    CRC64 crcGenerator;
    string CRC;
    ulong crcPolynomial;
    string crcPolynomialBinary = "0100001011110000111000011110101110101001111010100011011010010011";


    ///////// LISTEN /////////

    /// Image transfer information ///
    /*[HideInInspector] public GameObject movingPlane; // Plane to display image on
    Material mediaMaterial; // Material of the plane*/
    

    /*GameObject fixPlane; // Fix plane to display image on
    Material fixPlaneMaterial; // Material of the plane*/

    public GameObject ImageDisplay;
    Texture2D mediaTexture; // Texture of the plane
    Material ImageDisplayMaterial; // Material of the plane


    void Start()
    {
        // Initialize CRC Generator
        crcGenerator = new CRC64();
        crcPolynomial = Convert.ToUInt64(crcPolynomialBinary, 2);
        crcGenerator.Init(crcPolynomial);

        // Initialize texture parameters for image transfer of the moving plane
        /*movingPlane.transform.localScale = Vector3.Scale(transform.localScale, new Vector3(movingPlane.transform.localScale.x, -movingPlane.transform.localScale.y, movingPlane.transform.localScale.z));
        mediaMaterial = movingPlane.GetComponent<MeshRenderer>().material;
        mediaTexture = new Texture2D(512, 512, TextureFormat.Alpha8, false);
        mediaMaterial.mainTexture = mediaTexture;*/

        // Initialize texture parameters for image transfer of the fix plane
        /*fixPlane = GameObject.Find("FixedImagePlane").transform.Find("FixPlane").gameObject;
        fixPlane.transform.localScale = Vector3.Scale(transform.localScale, new Vector3(fixPlane.transform.localScale.x, -fixPlane.transform.localScale.y, fixPlane.transform.localScale.z));
        fixPlaneMaterial = fixPlane.GetComponent<MeshRenderer>().material;
        fixPlaneMaterial.mainTexture = mediaTexture;*/
    }

    // This function is called when the user activates the connectivity switch to start the communication with 3D Slicer
    public bool OnConnectToSlicerClick(string ipString, int port)
    {
        isConnected = ConnectToSlicer(ipString, port);
        return isConnected;
    }

    // Create a new socket handler and connect it to the server with the ip address and port provided in the function
    bool ConnectToSlicer(string ipString, int port)
    {
        socketForUnityAndHoloLens = new SocketHandler();

        bool isConnected = socketForUnityAndHoloLens.Connect(ipString, port);

        return isConnected;
        
    }

    // Routine that continuously sends the transform information of every model in infoToSend to 3D Slicer
    public IEnumerator SendTransformInfo()
    {
        while (true)
        {
            /*UnityEngine.Debug.Log("Sending...");*/
            yield return null; // If you had written yield return new WaitForSeconds(1); it would have waited 1 second before executing the code below.
            // Loop foreach element in infoToSend
            /*foreach (ModelInfo element in infoToSend)
            {
                SendMessageToServer.SendTransformMessage(element, scaleMultiplier, crcGenerator, CRC, socketForUnityAndHoloLens);
            }*/
        }
    }

    public IEnumerator ListenSlicerInfo()
    {
        while (true)
        {
            /*UnityEngine.Debug.Log("Listening...");*/
            yield return null;

            ////////// READ THE HEADER OF THE INCOMING MESSAGES //////////
            byte[] iMSGbyteArray = socketForUnityAndHoloLens.Listen(headerSize);

            if (iMSGbyteArray.Length >= (int)headerSize)
            {
                ////////// READ THE HEADER OF THE INCOMING MESSAGES //////////
                ReadMessageFromServer.HeaderInfo iHeaderInfo = ReadMessageFromServer.ReadHeaderInfo(iMSGbyteArray);

                ////////// READ THE BODY OF THE INCOMING MESSAGES //////////
                uint BodySize = Convert.ToUInt32(iHeaderInfo.BodySize);

                // Process the message when it is complete (that means, we have received as many bytes as the body size + the header size)
                if (iMSGbyteArray.Length >= (int)BodySize + (int)headerSize)
                {
                    // Compare different message types and act accordingly
                    if ((iHeaderInfo.MsgType).Contains("TRANSFORM"))
                    {
                        /*// Extract the transform matrix from the message
                        Matrix4x4 matrix = ReadMessageFromServer.ExtractTransformInfo(iMSGbyteArray, movingPlane, scaleMultiplier, headerSize);
                        // Apply the transform matrix to the object
                        ApplyTransformToGameObject(matrix, movingPlane);*/
                    }
                    else if ((iHeaderInfo.MsgType).Contains("IMAGE"))
                    {
                        UnityEngine.Debug.Log("Image recived");

                        // Read and display the image content to our preview plane
                        DisplayImageInfo(iMSGbyteArray, iHeaderInfo, ImageDisplay);
                        UnityEngine.Debug.Log("Image displayed");
                    }
                    else if ((iHeaderInfo.MsgType).Contains("POLYDATA"))
                    {
                        UnityEngine.Debug.Log("Recived polydata message");
                        // Extract POLYDATA information from the message
                        ReadMessageFromServer.PolyDataInfo polyDataInfo = ReadMessageFromServer.ReadPolyDataInfo(iMSGbyteArray, headerSize, iHeaderInfo.ExtHeaderSize);
                        // Create a GameObject from the POLYDATA
                        CreateGameObjectFromPolyData(polyDataInfo);
                    }
                }
            }
        }
    }

    /// Apply transform information to GameObject ///
    void ApplyTransformToGameObject(Matrix4x4 matrix, GameObject gameObject)
    {
        Vector3 translation = matrix.GetColumn(3);
        //gameObject.transform.localPosition = new Vector3(-translation.x, translation.y, translation.z);
        //Vector3 rotation= matrix.rotation.eulerAngles;
        //gameObject.transform.localRotation = Quaternion.Euler(rotation.x, -rotation.y, -rotation.z);
        if (translation.x > 10000 || translation.y > 10000 || translation.z > 10000)
        {
            gameObject.transform.position = new Vector3(0, 0, 0.5f);
            UnityEngine.Debug.Log("Out of limits. Default position assigned.");
        }
        else
        {
            gameObject.transform.localPosition = new Vector3(-translation.x, translation.y, translation.z);
            Vector3 rotation= matrix.rotation.eulerAngles;
            gameObject.transform.localRotation = Quaternion.Euler(rotation.x, -rotation.y, -rotation.z);
        }
    }

    //////////////////////////////// INCOMING IMAGE MESSAGE ////////////////////////////////
    void DisplayImageInfo(byte[] iMSGbyteArray, ReadMessageFromServer.HeaderInfo iHeaderInfo, GameObject ImageDisplay)
    {
        ReadMessageFromServer.ImageInfo iImageInfo = ReadMessageFromServer.ReadImageInfo(iMSGbyteArray, headerSize, iHeaderInfo.ExtHeaderSize);

        if (iImageInfo.NumPixX > 0 && iImageInfo.NumPixY > 0)
        {
            // Calcola la dimensione attesa dell'immagine
            int expectedImageSize = iImageInfo.NumPixX * iImageInfo.NumPixY;

            mediaTexture = new Texture2D(iImageInfo.NumPixX, iImageInfo.NumPixY, TextureFormat.Alpha8, false);

            ImageDisplayMaterial = ImageDisplay.GetComponent<MeshRenderer>().material;

            // Verifica che l'array di origine abbia abbastanza dati
            if (iMSGbyteArray.Length < iImageInfo.OffsetBeforeImageContent + expectedImageSize)
            {
                UnityEngine.Debug.LogError("L'array iMSGbyteArray non contiene abbastanza dati per l'immagine.");
                return;
            }

            // Define the array that will store the image's pixels
            byte[] bodyArray_iImData = new byte[expectedImageSize];
            byte[] bodyArray_iImDataInv = new byte[bodyArray_iImData.Length];

            Buffer.BlockCopy(iMSGbyteArray, iImageInfo.OffsetBeforeImageContent, bodyArray_iImData, 0, bodyArray_iImData.Length);

            // Invert the values of the pixels to have a dark background
            for (int i = 0; i < bodyArray_iImData.Length; i++)
            {
                bodyArray_iImDataInv[i] = (byte)(255 - bodyArray_iImData[i]);
            }
            // Load the pixels into the texture and the material
            mediaTexture.LoadRawTextureData(bodyArray_iImDataInv);
            mediaTexture.Apply();

            ImageDisplayMaterial.mainTexture = mediaTexture;
        }
        else
        {
            UnityEngine.Debug.Log("Void image");
        }
    }



    // Called when the user disconnects Unity from 3D Slicer using the connectivity switch
    public void OnDisconnectClick()
    {
        socketForUnityAndHoloLens.Disconnect();
        UnityEngine.Debug.Log("Disconnected from the server");
    }


    // Execute this function when the user exits the application
    void OnApplicationQuit()
    {
        // Release the socket.
        if (socketForUnityAndHoloLens != null)
        {
            socketForUnityAndHoloLens.Disconnect();
        }
    }

    public static GameObject CreateGameObjectFromPolyData(ReadMessageFromServer.PolyDataInfo polyDataInfo)
    {
        GameObject newObject = new GameObject("PolyDataObject");
        Mesh mesh = new Mesh();

        if (polyDataInfo.NumPoints == 0 || polyDataInfo.Points == null)
        {
            UnityEngine.Debug.LogError("No points found in PolyDataInfo.");
            return null;
        }

        // Imposta i vertici del mesh
        Vector3[] vertices = new Vector3[polyDataInfo.NumPoints];
        for (int i = 0; i < polyDataInfo.NumPoints; i++)
        {
            vertices[i] = polyDataInfo.Points[i];
        }
        mesh.vertices = vertices;

        // Imposta i triangoli del mesh
        if (polyDataInfo.NumPolygons == 0 || polyDataInfo.Polygons == null)
        {
            UnityEngine.Debug.LogError("No polygons found in PolyDataInfo.");
            return null;
        }

        List<int> triangles = new List<int>();
        uint polygonIndex = 0;

        while (polygonIndex < polyDataInfo.Polygons.Length)
        {
            if (polygonIndex >= polyDataInfo.Polygons.Length)
            {
                UnityEngine.Debug.LogError($"polygonIndex {polygonIndex} is out of bounds for polygons array with length {polyDataInfo.Polygons.Length}.");
                break;
            }

            uint verticesCount = polyDataInfo.Polygons[polygonIndex];
            polygonIndex++;

            if (polygonIndex + verticesCount > polyDataInfo.Polygons.Length)
            {
                UnityEngine.Debug.LogError("Invalid polygon data encountered.");
                break;
            }

            for (int i = 0; i < verticesCount - 2; i++)
            {
                triangles.Add((int)polyDataInfo.Polygons[polygonIndex]);
                triangles.Add((int)polyDataInfo.Polygons[polygonIndex + i + 1]);
                triangles.Add((int)polyDataInfo.Polygons[polygonIndex + i + 2]);
            }

            polygonIndex += verticesCount;
        }
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        // Cerca un attributo che rappresenti i colori
        Color[] colors = null;
        for (int i = 0; i < polyDataInfo.NumAttributes; i++)
        {
            byte attributeType = polyDataInfo.AttributeHeader.GetAttributeType(i);
            byte numComponents = polyDataInfo.AttributeHeader.GetNumberOfComponents(i);

            // Verifica se l'attributo corrente è un colore RGB o RGBA
            if ((attributeType == 10 || attributeType == 11) && (numComponents == 3 || numComponents == 4))
            {
                colors = new Color[polyDataInfo.NumPoints];
                for (int j = 0; j < polyDataInfo.NumPoints; j++)
                {
                    float r = polyDataInfo.AttributeData.Data[i][j * numComponents];
                    float g = polyDataInfo.AttributeData.Data[i][j * numComponents + 1];
                    float b = polyDataInfo.AttributeData.Data[i][j * numComponents + 2];
                    float a = (numComponents == 4) ? polyDataInfo.AttributeData.Data[i][j * numComponents + 3] : 1.0f;

                    colors[j] = new Color(r, g, b, a);
                }
                break; // Esci dal loop una volta trovato l'attributo del colore
            }
        }

        if (colors != null)
        {
            mesh.colors = colors;
        }
        else
        {
            UnityEngine.Debug.LogWarning("No color attributes found. Defaulting to white.");
        }



        MeshRenderer meshRenderer = newObject.AddComponent<MeshRenderer>();
        MeshFilter meshFilter = newObject.AddComponent<MeshFilter>();

        meshFilter.mesh = mesh;

        // Imposta il materiale
        meshRenderer.material = new Material(Shader.Find("Standard"));

        /*// Aggiungi il componente ContinuousRotation
        ContinuousRotation rotationScript = newObject.AddComponent<ContinuousRotation>();
        rotationScript.rotationSpeed = 30f; 
*/
        return newObject;
    }

    







}
