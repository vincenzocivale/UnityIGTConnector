import pandas as pd
from PIL import Image
import numpy as np
import os

def process_csv(file_path, output_path):
    """
    Legge un file CSV, suddivide i dati in base alla virgola, e converte i numeri in notazione scientifica in float.

    :param file_path: Percorso del file CSV di input
    :param output_path: Percorso dove salvare il DataFrame risultante
    """
    # Leggi il file CSV
    df = pd.read_csv(file_path, header=None)  # header=None se non ci sono intestazioni

    # Converti i dati in notazione scientifica a float
    for col in df.columns:
        df[col] = df[col].apply(pd.to_numeric, errors='coerce')  # Converti a float, 'coerce' per gestire errori

    # Salva il DataFrame risultante in un nuovo file CSV (opzionale)
    df.to_csv(output_path, index=False,
              header=False)  # index=False per non salvare l'indice, header=False se non ci sono intestazioni

    return df


def save_images_from_dataframe(df, num_images, image_width, image_height, output_folder):
    """
    Salva le immagini generate dai dati del DataFrame in una cartella specificata.

    :param df: DataFrame contenente i dati RGB e alpha
    :param num_images: Numero di immagini da salvare
    :param image_width: Larghezza di ciascuna immagine
    :param image_height: Altezza di ciascuna immagine
    :param output_folder: Cartella dove salvare le immagini
    """
    # Assicurati che la cartella esista
    if not os.path.exists(output_folder):
        os.makedirs(output_folder)

    # Calcola il numero di pixel per immagine
    pixels_per_image = image_width * image_height


    for i in range(num_images):
        # Estrai i dati per l'immagine corrente
        start_index = i * pixels_per_image
        end_index = start_index + pixels_per_image
        image_data = df.iloc[start_index:end_index]

        # Crea un array di pixel per l'immagine
        pixel_array = np.zeros((image_height, image_width, 4), dtype=np.uint8)

        # Riempie l'array di pixel con i dati RGB e alpha
        pixel_array[:, :, 0] = image_data['r'].values.reshape((image_height, image_width))
        pixel_array[:, :, 1] = image_data['g'].values.reshape((image_height, image_width))
        pixel_array[:, :, 2] = image_data['b'].values.reshape((image_height, image_width))
        pixel_array[:, :, 3] = image_data['a'].values.reshape((image_height, image_width))

        # Crea una nuova immagine con Pillow
        image = Image.fromarray(pixel_array, 'RGBA')

        # Salva l'immagine
        image_path = os.path.join(output_folder, f'image_{i + 1}.png')
        image.save(image_path)


# Chiama la funzione
df_result = pd.read_csv("output.csv", columns=['r', 'g', 'b', 'a'])
save_images_from_dataframe(df_result, 130, 256, 256, "output_images")



