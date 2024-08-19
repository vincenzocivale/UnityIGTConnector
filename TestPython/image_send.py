"""
============================
Image sending server
============================

Simple application that starts a server that provides an image stream with a bright circle moving

"""

import pyigtl  # pylint: disable=import-error

from time import sleep
import numpy as np
from math import sin

server = pyigtl.OpenIGTLinkServer(port=18944)

image_size = [500, 300]
radius = 60

timestep = 0
while True:

    if not server.is_connected():
        # Wait for client to connect
        sleep(0.1)
        continue

    # Generate image
    timestep += 1
    cx = (1+sin(timestep*0.05)) * 0.5 * (image_size[0]-2*radius)+radius
    cy = (1+sin(timestep*0.06)) * 0.5 * (image_size[1]-2*radius)+radius
    y, x = np.ogrid[-cx:image_size[0]-cx, -cy:image_size[1]-cy]
    mask = x*x + y*y <= radius*radius
    voxels = np.ones((image_size[0], image_size[1], 1))
    voxels[mask] = 255

    # numpy image axes are in kji order, while we generated the image with ijk axes
    voxels = np.transpose(voxels, axes=(2, 1, 0))

    # Send image
    print(f"time: {timestep}   position: ({cx}, {cy})")
    image_message = pyigtl.ImageMessage(voxels, device_name="Image")
    server.send_message(image_message, wait=True)
    # Since we wait until the message is actually sent, the message queue will not be flooded