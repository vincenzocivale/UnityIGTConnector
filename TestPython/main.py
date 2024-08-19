import pyigtl
import vtk
import logging
import socket


# Funzione per ottenere l'indirizzo IP locale
def get_local_ip():
    hostname = socket.gethostname()
    local_ip = socket.gethostbyname(hostname)
    return local_ip


# Stampa dell'indirizzo IP locale
print("Server in ascolto su IP:", get_local_ip())

# Configura il logging
logging.basicConfig(level=logging.INFO)

# Leggi il mesh
reader = vtk.vtkXMLPolyDataReader()
reader.SetFileName("Red_ring.vtp")
reader.Update()
polydata = reader.GetOutput()


# Funzione per ottenere il nome del tipo di attributo
def get_attribute_type_name(data_array):
    if isinstance(data_array, vtk.vtkDataArray):
        # Restituisce una stringa che rappresenta il tipo di array, es. 'Float32', 'Int32', etc.
        return data_array.GetDataTypeAsString()
    else:
        return "Unknown"


# Funzione per ottenere il numero di componenti dell'attributo
def get_num_components(data_array):
    return data_array.GetNumberOfComponents()


# Funzione per stampare le informazioni degli attributi del polydata
def print_attribute_info(polydata):
    point_data = polydata.GetPointData()
    num_arrays = point_data.GetNumberOfArrays()

    logging.info(f"Number of attributes: {num_arrays}")

    for i in range(num_arrays):
        data_array = point_data.GetArray(i)
        attribute_type_name = get_attribute_type_name(data_array)
        num_components = get_num_components(data_array)

        # Stampa il tipo e il numero di componenti dell'attributo
        logging.info(f"Attribute {i}: Type = {attribute_type_name}, Number of Components = {num_components}")


# Esegui la funzione di stampa delle informazioni sugli attributi del polydata
print_attribute_info(polydata)

# Crea il server OpenIGTLink
port = 18944
server = pyigtl.OpenIGTLinkServer(port=port)
server.start()

logging.info(f"Server listening on port {port}...")

try:
    # Invia il polydata al server OpenIGTLink
    polydata_message = pyigtl.PolyDataMessage(polydata, device_name='Mesh')
    server.send_message(polydata_message, wait=True)
    logging.info("Polydata sent successfully.")
except Exception as e:
    logging.error(f"An error occurred while sending the polydata: {e}")
finally:
    logging.info("Server closed.")
    server.stop()
