import logging
import os
from typing import Annotated, Optional, List
import time
import numpy as np
import vtk

import slicer
from slicer.i18n import tr as _
from slicer.i18n import translate
from slicer.ScriptedLoadableModule import *
from slicer.util import VTKObservationMixin
from slicer.parameterNodeWrapper import (
    parameterNodeWrapper,
    WithinRange,
)


#
# IGTLink4DStream
#


class IGTLink4DStream(ScriptedLoadableModule):
    """Uses ScriptedLoadableModule base class, available at:
    https://github.com/Slicer/Slicer/blob/main/Base/Python/slicer/ScriptedLoadableModule.py
    """

    def __init__(self, parent):
        ScriptedLoadableModule.__init__(self, parent)
        self.parent.title = _("IGTLink4DStream")  # TODO: make this more human readable by adding spaces
        # TODO: set categories (folders where the module shows up in the module selector)
        self.parent.categories = [translate("qSlicerAbstractCoreModule", "Examples")]
        self.parent.dependencies = []  # TODO: add here list of module names that this module requires
        self.parent.contributors = [
            "John Doe (AnyWare Corp.)"]  # TODO: replace with "Firstname Lastname (Organization)"
        # TODO: update with short description of the module and a link to online module documentation
        # _() function marks text as translatable to other languages
        self.parent.helpText = _("""
This is an example of scripted loadable module bundled in an extension.
See more information in <a href="https://github.com/organization/projectname#IGTLink4DStream">module documentation</a>.
""")
        # TODO: replace with organization, grant and thanks
        self.parent.acknowledgementText = _("""
This file was originally developed by Jean-Christophe Fillion-Robin, Kitware Inc., Andras Lasso, PerkLab,
and Steve Pieper, Isomics, Inc. and was partially funded by NIH grant 3P41RR013218-12S1.
""")

        # Additional initialization step after application startup is complete
        slicer.app.connect("startupCompleted()", registerSampleData)


#
# Register sample data sets in Sample Data module
#


def registerSampleData():
    """Add data sets to Sample Data module."""
    # It is always recommended to provide sample data for users to make it easy to try the module,
    # but if no sample data is available then this method (and associated startupCompeted signal connection) can be removed.

    import SampleData

    iconsPath = os.path.join(os.path.dirname(__file__), "Resources/Icons")

    # To ensure that the source code repository remains small (can be downloaded and installed quickly)
    # it is recommended to store data sets that are larger than a few MB in a Github release.

    # IGTLink4DStream1
    SampleData.SampleDataLogic.registerCustomSampleDataSource(
        # Category and sample name displayed in Sample Data module
        category="IGTLink4DStream",
        sampleName="IGTLink4DStream1",
        # Thumbnail should have size of approximately 260x280 pixels and stored in Resources/Icons folder.
        # It can be created by Screen Capture module, "Capture all views" option enabled, "Number of images" set to "Single".
        thumbnailFileName=os.path.join(iconsPath, "IGTLink4DStream1.png"),
        # Download URL and target file name
        uris="https://github.com/Slicer/SlicerTestingData/releases/download/SHA256/998cb522173839c78657f4bc0ea907cea09fd04e44601f17c82ea27927937b95",
        fileNames="IGTLink4DStream1.nrrd",
        # Checksum to ensure file integrity. Can be computed by this command:
        #  import hashlib; print(hashlib.sha256(open(filename, "rb").read()).hexdigest())
        checksums="SHA256:998cb522173839c78657f4bc0ea907cea09fd04e44601f17c82ea27927937b95",
        # This node name will be used when the data set is loaded
        nodeNames="IGTLink4DStream1",
    )

    # IGTLink4DStream2
    SampleData.SampleDataLogic.registerCustomSampleDataSource(
        # Category and sample name displayed in Sample Data module
        category="IGTLink4DStream",
        sampleName="IGTLink4DStream2",
        thumbnailFileName=os.path.join(iconsPath, "IGTLink4DStream2.png"),
        # Download URL and target file name
        uris="https://github.com/Slicer/SlicerTestingData/releases/download/SHA256/1a64f3f422eb3d1c9b093d1a18da354b13bcf307907c66317e2463ee530b7a97",
        fileNames="IGTLink4DStream2.nrrd",
        checksums="SHA256:1a64f3f422eb3d1c9b093d1a18da354b13bcf307907c66317e2463ee530b7a97",
        # This node name will be used when the data set is loaded
        nodeNames="IGTLink4DStream2",
    )


#
# IGTLink4DStreamParameterNode
#


@parameterNodeWrapper
class IGTLink4DStreamParameterNode:
    """
    The parameters needed by module.
    """

    inputSequence: slicer.vtkMRMLSequenceNode


#
# IGTLink4DStreamWidget
#

class IGTLink4DStreamWidget(ScriptedLoadableModuleWidget, VTKObservationMixin):
    def __init__(self, parent=None) -> None:
        ScriptedLoadableModuleWidget.__init__(self, parent)
        VTKObservationMixin.__init__(self)
        self.logic = None
        self._parameterNode = None
        self._parameterNodeGuiTag = None

    def setup(self) -> None:
        ScriptedLoadableModuleWidget.setup(self)

        # Inizializza la logica del modulo
        self.logic = IGTLink4DStreamLogic()

        # Carica il widget UI dal file .ui
        uiWidget = slicer.util.loadUI(self.resourcePath("UI/IGTLink4DStream.ui"))
        self.layout.addWidget(uiWidget)
        self.ui = slicer.util.childWidgetVariables(uiWidget)

        # Assicurati che sequenceSelector esista nel file .ui
        if not hasattr(self.ui, 'sequenceSelector'):
            raise AttributeError("Il file .ui non contiene un widget con il nome 'sequenceSelector'")

        # Configura il sequence selector
        self.ui.sequenceSelector.nodeTypes = ["vtkMRMLSequenceNode"]
        self.ui.sequenceSelector.selectNodeUponCreation = True
        self.ui.sequenceSelector.addEnabled = False
        self.ui.sequenceSelector.removeEnabled = False
        self.ui.sequenceSelector.noneEnabled = False
        self.ui.sequenceSelector.showHidden = False
        self.ui.sequenceSelector.showChildNodeTypes = False
        self.ui.sequenceSelector.setMRMLScene(slicer.mrmlScene)
        self.ui.sequenceSelector.setToolTip("Seleziona la sequenza da processare")

        # Configura il pulsante Apply
        self.ui.applyButton.connect("clicked(bool)", self.onApplyButton)
        self.ui.applyButton.enabled = False

        # Assicurati che il nodo dei parametri sia inizializzato
        self.initializeParameterNode()

        # Aggiorna lo stato del pulsante Apply quando la selezione cambia
        self.ui.sequenceSelector.connect("currentNodeChanged(vtkMRMLNode*)", self.updateApplyButtonState)

    def initializeParameterNode(self) -> None:
        """Assicura che il nodo dei parametri esista e sia osservato."""
        self.setParameterNode(self.logic.getParameterNode())

    def setParameterNode(self, inputParameterNode: Optional[IGTLink4DStreamParameterNode]) -> None:
        if self._parameterNode:
            self._parameterNode.disconnectGui(self._parameterNodeGuiTag)
            self.removeObserver(self._parameterNode, vtk.vtkCommand.ModifiedEvent, self.updateApplyButtonState)
        self._parameterNode = inputParameterNode
        if self._parameterNode:
            self._parameterNodeGuiTag = self._parameterNode.connectGui(self.ui)
            self.addObserver(self._parameterNode, vtk.vtkCommand.ModifiedEvent, self.updateApplyButtonState)
            self.updateApplyButtonState()

    def updateApplyButtonState(self, caller=None, event=None) -> None:
        """Abilita o disabilita il pulsante Apply in base allo stato della selezione."""
        self.ui.applyButton.enabled = self.ui.sequenceSelector.currentNode() is not None

    def onApplyButton(self) -> None:
        """Avvia il processamento quando l'utente clicca sul pulsante Apply."""
        with slicer.util.tryWithErrorDisplay("Failed to compute results.", waitCursor=True):
            # Ottieni la sequenza selezionata
            inputSequence = self.ui.sequenceSelector.currentNode()

            # Avvia il processamento
            self.logic.process(inputSequence)


#
# IGTLink4DStreamLogic
#


class IGTLink4DStreamLogic(ScriptedLoadableModuleLogic):

    def __init__(self) -> None:
        """Called when the logic class is instantiated. Can be used for initializing member variables."""
        ScriptedLoadableModuleLogic.__init__(self)

    def getParameterNode(self):
        return IGTLink4DStreamParameterNode(super().getParameterNode())

    def create_masked_volume(self,input_volume_node, output_volume_name, lower_threshold, upper_threshold):

        # Assicurati che i nodi di input siano del tipo Volume
        if input_volume_node.GetClassName() != 'vtkMRMLScalarVolumeNode' :
            raise TypeError("I nodi di input devono essere un volume e una mappa di etichette.")

        # Crea un nuovo nodo di maschera  per l'output
        output_volume_node = slicer.mrmlScene.AddNewNodeByClass('vtkMRMLLabelMapVolumeNode', output_volume_name)

        # Convertili in array NumPy per elaborare i dati
        input_array = slicer.util.arrayFromVolume(input_volume_node)

        # Applica la maschera con threshold
        masked_array = np.where( (input_array >= lower_threshold) & (input_array <= upper_threshold),
                                input_array, 0)

        # Copia i dati mascherati nell'array NumPy nel volume di output
        slicer.util.updateVolumeFromArray(output_volume_node, masked_array)

        return output_volume_node

    def process(self, inputSequence: slicer.vtkMRMLSequenceNode) -> None:
        """
        Run the processing algorithm.
        Can be used without GUI widget.
        """

        if not inputSequence:
            raise ValueError("Input sequence is invalid")


        # Trova il nodo del connettore OpenIGTLink
        cnode = slicer.mrmlScene.GetFirstNodeByName("IGTLConnector")
        if cnode is None:
            raise ValueError("IGTLConnector node not found")


        # Trova il nodo di segmentazione denominato "Segmentation"
        segmentationNode = slicer.mrmlScene.GetFirstNodeByName("Segmentation")
        if not segmentationNode:
            raise ValueError("Nodo di segmentazione 'Segmentation' non trovato nella scena.")

        startTime = time.time()
        logging.info("Processing started")

        numberOfVolumes = inputSequence.GetNumberOfDataNodes()
        print(f"Numero di volumi: {numberOfVolumes}")

        for i in range(numberOfVolumes):
            volume_node = inputSequence.GetNthDataNode(i)
            if not volume_node:
                print(f"Errore: Impossibile ottenere il volume all'indice {i}.")
                continue

            nodeName = volume_node.GetName()
            nodeType = volume_node.GetClassName()
            # Assicurati che il volume sia caricato nella scena
            if not slicer.mrmlScene.GetNodeByID(volume_node.GetID()):
                slicer.mrmlScene.AddNode(volume_node)

            # Crea un volume mascherato utilizzando il segmento selezionato
            # Usa un nodo di tipo vtkMRMLLabelMapVolumeNode

            maskedVolumeNode = self.create_masked_volume(volume_node, nodeName + "_Segment_2_masked", 200, 1000)


            # Converti il LabelMapVolumeNode in un ScalarVolumeNode
            scalarVolumeNode = slicer.mrmlScene.AddNewNodeByClass("vtkMRMLScalarVolumeNode",
                                                                  nodeName + "_Segment_2_scalar")
            slicer.modules.volumes.logic().CreateScalarVolumeFromVolume(slicer.mrmlScene, scalarVolumeNode,
                                                                        maskedVolumeNode)

            # Verifica se il volume scalar è stato creato correttamente
            if scalarVolumeNode:
                print(f"Volume scalar {scalarVolumeNode.GetName()} creato con successo.")

                # Imposta il volume per la visualizzazione nella finestra 3D (opzionale)
                slicer.util.setSliceViewerLayers(background=scalarVolumeNode, fit=True)

                # Registra il nodo convertito come uscita verso il connettore OpenIGTLink
                cnode.RegisterOutgoingMRMLNode(scalarVolumeNode)
            else:
                print(f"Errore: Impossibile creare il volume scalar per il segmento.")

        stopTime = time.time()
        logging.info(f"Processing completed in {stopTime - startTime:.2f} seconds")


# IGTLink4DStreamTest
#


class IGTLink4DStreamTest(ScriptedLoadableModuleTest):
    """
    This is the test case for your scripted module.
    Uses ScriptedLoadableModuleTest base class, available at:
    https://github.com/Slicer/Slicer/blob/main/Base/Python/slicer/ScriptedLoadableModule.py
    """

    def setUp(self):
        """Do whatever is needed to reset the state - typically a scene clear will be enough."""
        slicer.mrmlScene.Clear()

    def runTest(self):
        """Run as few or as many tests as needed here."""
        self.setUp()
        self.test_IGTLink4DStream1()

