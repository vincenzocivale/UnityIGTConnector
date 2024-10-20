Here's the updated README with the official links you provided:

---

**UnityIGTConnector**  
This repository contains the implementation of the [OpenIGTLink](https://openigtlink.org/) communication protocol within Unity. This integration enables the reception and transmission of [HEADER](https://openigtlink.org/developers/spec), [TRANSFORM](https://github.com/openigtlink/OpenIGTLink/blob/master/Documents/Protocol/transform.md), [IMAGE](https://github.com/openigtlink/OpenIGTLink/blob/master/Documents/Protocol/image.md), [POLYDATA](https://github.com/openigtlink/OpenIGTLink/blob/master/Documents/Protocol/polydata.md), and TEXT messages within the Unity scene, unlocking new possibilities for mixed reality applications in the medical field.

### Overview  
[OpenIGTLink](https://openigtlink.org/) is an open-source network communication protocol specifically designed for image-guided interventions. Its goal is to provide unified, real-time communication (URTC) in operating rooms for image-guided surgeries, where imaging devices, sensors, surgical robots, and computers from various manufacturers can work cooperatively.

Integrating OpenIGTLink into Unity offers the following benefits:

- **Extensibility**: Allows extending the functionality of any open-source software that supports Python or C++, enabling the visualization and interaction with content within a mixed reality application.
- **Code Reusability**: Avoids the need to re-implement complex functions within Unity, which, being a game engine, is not primarily designed for specialized medical applications.
- **Interoperability**: Facilitates communication between different platforms and devices used in medical environments.
- **Advanced Visualization**: Leverages Unity's graphical capabilities to represent complex medical data in a mixed reality environment.

### Features

- Reception and transmission of OpenIGTLink messages in Unity
- Support for message types:
  - **[TRANSFORM](https://github.com/openigtlink/OpenIGTLink/blob/master/Documents/Protocol/transform.md)**: Enables synchronization of the position and orientation of objects.
  - **[IMAGE](https://github.com/openigtlink/OpenIGTLink/blob/master/Documents/Protocol/image.md)**: Facilitates the reception of volumes reconstructed from biomedical images.
  - **[POLYDATA](https://github.com/openigtlink/OpenIGTLink/blob/master/Documents/Protocol/polydata.md)**: Supports the transmission of various types of meshes (e.g., surface models).

- Integration with the UnityVolumeRendering project for volume rendering.
- Compatibility with HoloLens 2 for mixed reality surgical applications.


This update adds more clarity about the function of each message type and their role within the system. Let me know if you'd like any further revisions!

### Motivation  
This project stems from the need to create advanced surgical support applications using HoloLens 2, leveraging features already present in other surgical support systems. Integrating OpenIGTLink into Unity allows for:

- Receiving real-time data from specialized medical software like 3D Slicer
- Visualizing this data in a mixed reality environment
- Interacting with medical information in an intuitive and immersive way
- Facilitating collaboration between different systems and devices in the operating room

### 3D Slicer in Unity (usage example)  
A first example of usage for this implementation is the integration with the UnityVolumeRendering project in combination with the 3D Slicer software. This combination allows for:

- Receiving volumes from software like 3D Slicer via OpenIGTLink
- Perform effective volume segmentation using machine learning models trained in Python
- Visualizing these volumes within the Unity scene
- Leveraging the volumetric rendering capabilities of UnityVolumeRendering

This approach enables the creation of advanced surgical support applications without needing to reimplement features that already exist in other systems.

### Projects That Inspired This Work

This project builds upon the following previous works and resources:

- [SlicerIGT](https://github.com/SlicerIGT/SlicerIGT.git): A 3D Slicer extension providing modules for image-guided therapy research, serving as a foundation for many OpenIGTLink integrations.
- [UnityVolumeRendering](https://github.com/mlavik1/UnityVolumeRendering.git): A project focused on volume rendering in Unity, which has been integrated into this implementation to visualize medical imaging data.
- [OpenIGTLink](https://github.com/openigtlink/OpenIGTLink): The official repository for the OpenIGTLink protocol, which forms the backbone of this communication system.
- [OpenIGTLink-Unity](https://github.com/franklinwk/OpenIGTLink-Unity.git): An initial implementation of OpenIGTLink in Unity, supporting only the sending and receiving of transform messages.
- [HoloLens2and3DSlicer-PedicleScrewPlacementPlanning](https://github.com/BSEL-UC3M/HoloLens2and3DSlicer-PedicleScrewPlacementPlanning.git): A project demonstrating the use of HoloLens 2 in combination with 3D Slicer, also limited to transform messages.