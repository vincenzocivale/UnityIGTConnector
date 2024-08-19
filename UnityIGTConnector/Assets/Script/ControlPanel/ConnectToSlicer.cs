using System.Collections;
using UnityEngine;
using TMPro; // Per TextMeshPro
using UnityEngine.UI; // Per il pulsante della UI

public class ConnectToSlicer : MonoBehaviour
{
    /// OPENIGTLINK CONTROL VARIABLES ///
    int port; // Port of the computer running Slicer
    Coroutine listeningRoutine; // Coroutine to control the listening part (3D Slicer -> Unity)
    Coroutine sendingRoutine; // Coroutine to control the sending part (Unity -> 3D Slicer)
    OpenIGTLinkConnect connectToServer; // Variable that connects to the OpenIGTLinkConnect script and enables the connection between Unity and 3D Slicer

    /// UI ELEMENTS ///
    public TMP_InputField ipInputField; // Campo di input per l'IP

    bool isConnected;

    void Start()
    {
        /// OPENIGTLINK CONTROL VARIABLES ///
        connectToServer = GameObject.Find("OpenIGTLinkConnectHandler").GetComponent<OpenIGTLinkConnect>();
        port = connectToServer.port; // Port of the computer running Slicer

    }

    /// CONNECT TO SLICER ///
    // Questa funzione viene chiamata quando l'utente preme il pulsante di connessione
    public void OnConnectButtonClick()
    {
        // Ottieni l'IP dall'input field
        string ipString = ipInputField.text;
        Debug.Log("Connect button clicked. Attempting to connect to IP: " + ipString + " on port: " + port);

        // Avvia la connessione con Slicer
        isConnected = connectToServer.OnConnectToSlicerClick(ipString, port);

        // Se la connessione ha successo, continua
        if (isConnected)
        {
            Debug.Log("Successfully connected to Slicer.");

            listeningRoutine = StartCoroutine(connectToServer.ListenSlicerInfo());
            sendingRoutine = StartCoroutine(connectToServer.SendTransformInfo());
        }
        else
        {
            Debug.Log("Failed to connect to Slicer.");
        }
    }

    // Questa funzione viene chiamata quando l'utente preme il pulsante di disconnessione
    public void OnDisconnectButtonClick()
    {
        Debug.Log("Disconnect button clicked. Attempting to disconnect.");

        // Se ci sono coroutine di ascolto o invio attive, fermale
        try
        {
            if (listeningRoutine != null)
            {
                StopCoroutine(listeningRoutine);
                Debug.Log("Stopped listening coroutine.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error stopping listening coroutine: " + e.Message);
        }

        try
        {
            if (sendingRoutine != null)
            {
                StopCoroutine(sendingRoutine);
                Debug.Log("Stopped sending coroutine.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error stopping sending coroutine: " + e.Message);
        }

        // Disconnetti dal server
        connectToServer.OnDisconnectClick();
        Debug.Log("Disconnected from Slicer.");
    }
}
