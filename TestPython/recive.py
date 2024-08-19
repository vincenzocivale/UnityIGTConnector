import pyigtl
import vtk
import logging
import time

# Configura il logging
logging.basicConfig(level=logging.INFO)

# Crea il client OpenIGTLink
client = pyigtl.OpenIGTLinkClient()
client.start()

max_retries = 5
for attempt in range(max_retries):
    try:
        input_message = client.wait_for_message("Mesh", timeout=10)
        if input_message:
            polydata = input_message.polydata
            logging.info("Polydata received successfully.")

            # Salva il polydata in un file per verifica
            writer = vtk.vtkPLYWriter()
            writer.SetFileName("received_cube.ply")
            writer.SetInputData(polydata)
            writer.Write()
            logging.info("Polydata saved to 'received_cube.ply'.")
            break
        else:
            logging.warning("No message received within the timeout period.")
    except Exception as e:
        logging.error(f"An error occurred while receiving the polydata: {e}")
        time.sleep(2)  # Attendi prima di riprovare
else:
    logging.error("Failed to receive polydata after multiple attempts.")
