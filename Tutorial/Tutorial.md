



# HiBoP Tutorial: Getting Started
This tutorial is aimed to help new users getting started into HiBoP with tested sample data. It consists of a step-by-step list of actions to perform in order to get everything working with the provided sample data. For more information about a specific feature, please refer to the documentation attached with the release of the software.
## Getting sample data (1~2 minutes)
For this tutorial, we provide you with sample data that you will add to a project and visualize within HiBoP in order to showcase how you can do it with your own data. The sample data contains anatomical and functional data for two patients on one experiment. You can download it [here](https://github.com/hbp-HiBoP/HiBoP/raw/master/Tutorial/sample_dataset.zip).

The zip file contains two folders (one for each patient), each containing:
* Files describing the anatomy of the patient
	* **patient[AB]_Mesh_GreyMatterLeft.gii -** Left part of the pial surface
	* **patient[AB]_Mesh_GreyMatterRight.gii -** Right part of the pial surface
	* **patient[AB]_Mesh_WhiteMatterLeft.gii -** Left part of the white matter surface
	* **patient[AB]_Mesh_WhiteMatterRight.gii -** Right part of the white matter surface
	*  **patient[AB]_Mesh_Transformation.trm -** Transformation matrix (allows meshes to be in the same reference system as the MRI)
	* **patient[AB]_MRI.nii -** MRI of the patient
	* **patient[AB]_Electrodes_Patient.tsv -** Description of the sites in the patient reference system
	* **patient[AB]_Electrodes_MNI.tsv -** Description of the sites in the MNI reference system
* Files containing functional data:
	* **patient[AB]_VISU.vhdr -** Header of the BrainVision file for the task *VISU*
	* **patient[AB]_VISU.vmrk-** Markers file for the task *VISU* (describing the events of the experiment)
	* **patient[AB]_VISU.eeg -** Binary data of the BrainVision file for the task *VISU*

After having downloaded the zip file, extract it wherever you want. In the rest of the document, we will call this folder the *Database*.
## Creating and adding data to the project (10~15 minutes)
### Creating the HiBoP project
The first step is to create a new project for the sample data.
1. On the top menu, click on *File > New Project...*
2. A new window opens, allowing to create a new project
3. Fill all the required fields
   * In the *Name* field, type "SampleProject"
   * In the *Location* field, either type the location where you want the project to be saved, or use the folder icon to browse to a folder (this operation will then be called "browse to"). For this tutorial, you can set the location to the same folder as the *Database*
   * In the *Patients* field, browse to the *Database* folder. This is a link to the folder containing the anatomical data.
   * In the *Localizers* field, browse to the *Database* folder. This is a link to the folder containing the functional data.
4. Click on *OK* to create the project
### Adding anatomical data
The next step is to add *Patients* to the project in order to visualize them. A patient contains all the anatomical data, but no functional data. The next procedure explains how to add the patient *patientA* to the project; just replace every occurences of "patientA" with "patientB" in order to add the second patient to the project.
1. On the top menu, click on *Patient > Patients...*
2. A new window opens, allowing to add, remove and edit patients of the project
3. Click on the *+* button on the left side of the window to create a new patient
4. Select "From scratch" and click on *OK* (on a side note, everytime you are presented with this dialog asking you to select the method you want to use to create an item, select "From scratch", unless otherwise specified. You could be faster using other options, but we recommend you to always create items from scratch in this tutorial in order to get accustomed to the user interface)
5. A new window opens, allowing the modification of the patient
6. Fill all the required fields
	* In the *Name* field, type "PatientA"
	* In the *Place* field, type "Earth"
	* In the *Date* field, type "2000"
7. Add the two brain meshes to the patient
	1. While selecting the *Meshes* tab, click on the *+* button on the left side of the window
	2. A new window opens, allowing the modification of the brain mesh
	3. Fill all the required fields
		* In the *Name* field, type "Grey matter"
		* In the *Type* field, select "LeftRight" (this is because the mesh in split into two files, one for each hemisphere)
		* In the *Mesh* fields, browse to the left part of the pial surface ("*Database*/patientA/patientA_Mesh_GreyMatterLeft.gii") for the most left field, and to the right part of the pial surface ("*Database*/patientA/patientA_Mesh_GreyMatterRight.gii") for the most right field
		* Leave the *MarsAtlas* fields empty
		* In the *Transformation* field, browse to the transformation file ("*Database*/patientA/patientA_Mesh_Transformation.trm")
	4. Click on *OK* to save the newly created mesh
	5. Repeat steps 1. to 4. and replace every occurences of "Grey matter" by "White matter" to add the second brain mesh to the patient.
8. Add the MRI to the patient
	1. Click on the *MRIs* tab on the right of the *Meshes* tab
	2. Click on the *+* button on the left side
	3. A new window opens, allowing the modification of the MRI
	4. Fill all the required fields
		* In the *Name* field, type "MRI"
		* In the *MRI* field, browse to the MRI of the patient ("*Database*/patientA/patientA_MRI.nii")
	5. Click on *OK* to save the newly created MRI
9. Add the sites to the patient
	1. Click on the *Sites* tab on the right of the *MRIs* tab
	2. Click on the *+* button on the left side
	3. This time, select "From file" to import the sites
	4. Click on *OK* and browse to the patient electrodes coordinates file ("*Database*/patientA/patientA_Electrodes_Patient.pts")
	5. Repeat steps 1. to 4. to also load "*Database*/patientA/patientA_Electrodes_MNI.pts" (coordinates of the electrodes in MNI reference system) and "*Database*/patientA/patientA_Electrodes.csv" (tags for the sites)
10. Click on *OK* to save the patient
11. Repeat steps 3. to 10. and replace every occurences of "patientA" by "patientB" to add the second patient
12. Click *OK* to add the two patients to the project

At this point, and after each milestone of this tutorial, it is recommended to save the modifications. To do so, simply click on *File > Save Project* on the top menu.
### Creating the protocol
The next step is to create a *Protocol* to tell HiBoP how to epoch the data. Each protocol contains a list of blocs (one bloc for each condition of the experiment), which contain a list of sub-blocs, which contain a list of events. For more information on protocols and how they are defined, please refer to the documentation attached to the release of the software. The sample data you downloaded concerns the following experiment, also known as *VISU*:

> The subject is presented with an image of a specific type (fruit,
> face, landscape...) and has to click on a button whenever he sees a
> fruit. Each event (presentation of a type of image or the response of
> the subject) is coded by an integer in the markers file:
> * **10** House
> * **20** Face
> * **30** Animal
> * **40** Landscape
> * **50** Object
> * **60** Pseudoword
> * **70** Consonant
> * **80** Fruit
> * **1** and **2** Response to a fruit

1. On the top menu, click on *Experience > Protocols...*
2. A new window opens, allowing to add, remove and edit protocols of the project
3. Click on the *+* button on the left side of the window to create a new protocol
4. A new window opens, allowing the modification of a protocol
5. Fill all the required fields
	* In the *Name* field, type "VISU"
6. Add the blocs to this protocol (for the purpose of this tutorial, we will only add two blocs: fruit and face, but feel free to add more blocs if you are curious)
	1. Click on the *+* button on the left side of the window
	2. A new window opens, allowing the modification of a bloc
	3. Fill all the required fields
		* In the *Name* field, type "FRUIT"
		* In the *Sort* field, type "Main_RESPONSE_LATENCY" (this means that the trials will be sorted by the latency of the "RESPONSE" event of the "Main" sub-bloc)
	4. Click on the *+* button on the left side to add the sub-bloc
	5. A new window opens, allowing the modification of the sub-bloc
	6. Fill all the required fields
		* In the *Name* field, type "Main"
		* In the *Window* field, set the time window to **-500ms to 1000ms** using either the sliders or the input fields on the right
		* In the *Baseline* field, set the time window to **-200ms to 0ms** using either the sliders or the input fields on the right
	7. While selecting the *Events* tab, click on the *+* button on the left of the window to add a new event
	8. A new window opens, allowing the modification of an event
	9. Fill the required fields
		* In the *Name* field, type "FRUIT"
		* In the *Type* field, "Main" should be selected
		* In the *Codes* field, type "80"
	10. Click on *OK* to save the event
	11. While selecting the *Events* tab, click on the *+* button on the left of the window to add another event
	12. A new window opens, allowing the modification of an event
	13. Fill the required fields
		* In the *Name* field, type "RESPONSE"
		* In the *Type* field, select "Secondary"
		* In the *Codes* field, type "1,2"
	14. Click on *OK* to save the event
	15. Click on *OK* to save the sub-bloc
	16. Click on *OK* to save the bloc
	17. Repeat steps 1. to 16. to add the bloc "FACE". The differences are between the two blocs are:
		* The name of the bloc is "FACE"
		* The sort of the bloc is left empty
		* There is only one event in the sub-bloc called "FACE" and with the code "20" (there is no "RESPONSE" event)
7. Click on *OK* to save the protocol
8. Click on *OK* to add the protocol to the project
### Adding functional data
The next step is to link the functional data and the protocol you just created into a *Dataset*.
1. On the top menu, click on *Experience > Datasets...*
2. A new window opens, allowing to add, remove and edit datasets of the project
3. Click on the *+* button on the left side of the window to add a new dataset
4. Fill all the required fields
	* In the *Name* field, type "VISU_dataset"
	* In the *Protocol* field, select "VISU"
5. Add the two data (one for each patient) to this dataset
	1. Click on the *+* button on the left side of the window
	2. A new window opens, allowing to edit a data
	3. Fill the required fields
		* In the *Name* field, type "gamma"
		* In the *Type* field, select "iEEG"
		* In the *Patient* field, select "PatientA"
		* In the *Normalization* field, select "Auto"
		* In the *Type* field, select "BrainVision"
		* In the *Header* field, browse to the BrainVision header file of the patientA for this experiment ("*Database*/patientA/patientA_VISU.vhdr")
		* Click on *OK* to save the data
	4. Repeat steps 1. to 3. to add the data for PatientB by replacing every occurences of "PatientA" by "PatientB"
6. Click on *OK* to save the dataset
7. Click on *OK* to add the dataset to the project
### Creating the visualization
The last step is to tell HiBoP what you want to visualize: this is done by creating a *Visualization*.
1. On the top menu, click on *Visualization*
2. A new window opens, allowing to add, remove, display and edit visualizations of the project
3. Click on the *+* button on the left side of the window to add a new visualization
4. Fill all the required fields
	* In the *Name* field, type "VISU"
5. Add the patients to the visualization
	1. Click on the *+* button on the left side of the window
	2. A new window opens, allowing to add patients of this project to the visualization
	3. Select the two patients by clicking on them
	4. Click on *OK* to add the two patients to the visualization
6. Add the columns to the visualization
	1. Click on the *+* button below the *Columns* label
	2. A new tab is added to the window, allowing to edit a column
	3. Fill the required fields
		* In the *Name* field, type "FRUIT"
		* In the *Type* field, select "iEEG"
		* In the *Protocol* field, select "VISU"
		* In the *Bloc* field, select "FRUIT"
		* In the *Dataset* field, select "VISU_dataset"
		* In the *Data* field, select "gamma"
	4. Click one more time on the *+* button to add the second column, and fill the required fields
		* In the *Name* field, type "FACE"
		* In the *Type* field, select "iEEG"
		* In the *Protocol* field, select "VISU"
		* In the *Bloc* field, select "FACE"
		* In the *Dataset* field, select "VISU_dataset"
		* In the *Data* field, select "gamma"
7. Click on *OK* to save the visualization
8. Click on *OK* to add the visualizations to the project
9. On the top menu, click on *File > Save Project* to save the complete project

If you followed all of this carefully, you are now ready to visualize the data within HiBoP.  
## Visualizing the data (10~15 minutes)
The first step is to display the previously created visualization.
1. On the top menu, click on *Visualization*
2. Select the visualization by clicking on it
3. Click on *Display* at the bottom of the window

From now on, almost every features are represented by icons. If you want to know what a button does, put the mouse on an icon and do not move it for a short period of time: this will display a small tooltip telling you what the button does.

Once all data is loaded, the visualization should be displayed. If you did not alter the user preferences beforehand, another computation should start right after the opening of the visualization window. This corresponds to the projection of the iEEG activity on the brain mesh. Once this is finished, the MNI brain should be colored with the activity regarding the selected colormap. The camera is currently showing the right hemisphere; however, the patients of this tutorial only have electrodes in the left hemisphere. Use the mouse and right click while dragging in order to move the MNI mesh to see the left hemisphere. You can click on the *Right Hemisphere* button on the top toolbar. This will trigger the computation once again.

If the activity is not projected on the mesh, you can either change your preferences using the *Edit > User Preferences...* menu or you can manually compute them using the following procedure:
1. On the left side menu, click on the *EEG settings* button
2. This will change the top toolbar to the *EEG settings* toolbar
3. Then, click on the *Project the EEG data on the brain* button; this will trigger the aforementioned computation

Once the activity is computed, you can visualize it dynamically using the timeline. Click on the *Timeline* button on the left menu to display the timeline toolbar. There you can either use the slider to visualize each sample of the activity, or click on the *Play* button to make the timeline play. You can alter the speed by changing the number of samples to be displayed each second, and you can make the timeline loop using the *Loop* button. Do not forget to click on the *Apply changes to all columns* button in order to synchronize the timeline for the two conditions.

Put the timeline slider on sample number 69 (578.13 milliseconds after the image was presented to the subject) for both columns. You should clearly see a difference in the activity between the two conditions in one specific area: it is completely activating (red) for the FRUIT condition and completely deactivating (blue) for the FACE condition. This seems to be a interesting region, so the best is to define a region of interest in that particular area:
1. Click on the *Regions of Interest* button on the left menu
2. This will change the top toolbar to the *Regions of Interest* toolbar
3. Click on the *+* button to add a new ROI
4. You can change the name of the ROI if you want
5. Click next to the interesting region to spawn a ROI sphere
6. You can control the size of the ROI sphere using the mouse wheel and you can drag and drop the ROI sphere to adjust its position: use these tools to match the interesting region
7. If you spawned unwanted spheres, you can select an unwanted sphere by clicking on it in the 3D view or by selecting it in the spheres dropdown on the toolbar and press the *-* button on the right of the dropdown to remove it
8. Once you are finished defining the ROI, click on any other button on the left menu to leave the ROI toolbar (for instance, you can click on the *Scene Settings* button)

The next step is to inspect the different sites of this ROI. However the mesh is currently hiding most of the sites of this ROI: you will need to cut the mesh in order to see the sites. To do so:

1. Drag and drop the limit between the *Sites* and the *Cuts* panels on the right to open the *Cuts* panel
2. Click on the *Add Cut* button to add a cut to the visualization
3. This should create an axial-oriented cut at the middle of the mesh
4. You can change the direction of the cut using the dropdown and the flip toggle below the cut image on the Cuts panel
5. You can change the position of the cut using the slider below the cut image

You should now be able to see the sites within the region of interest. Expand the *Graphs and Trial Matrices* panel and click on a site in the 3D view. This will display the activity of the selected site inside this panel. You can go from site to site using the left and right arrow keys. The panel contains:
 *  A graph containing curves representing:
	 * The mean values of the trials of the FRUIT condition for the selected site
	 * The mean values of the trials of the FACE condition for the selected site
	 * The mean values of the ROI of the FRUIT condition
	 * The mean values of the ROI of the FACE condition
 *  A trial matrix containing the values of each trial of the two conditions for the selected site

After inspecting the sites of the ROI, two sites seem really interesting: PatientA's Q'9 and Q'10. These two sites are close but have completely different responses. We can ask: why is that the case? One way to progress towards the answer of this question is to closely inspect the anatomy of the patient.
1. Select Q'9 or Q'10 in the 3D view
2. On the left side menu, click on the *Sites* button
3. This will change the top toolbar to the *Sites* toolbar
4. On the toolbar, click on the *Load the patient the selected site belongs to* button
5. This will load a single patient visualization containing only PatientA
6. Once the loading is finished, click on the *Eye* button close to the "VISU" label at the bottom of the software to temporary hide the multi-patient visualization
7. In the single-patient visualization, select Q'10 in the 3D view
8. On the toolbar, click on the *Cut the brain around the selected site* button
9. This will create three cuts intersecting at the site position

If you have a close look to Q'9 and Q'10 positions within PatientA's brain, you can see that they may belong to two different brain areas. That can be one reason why their activity is so different.

All in all, this second part of the tutorial was made to demonstrate one use case of HiBoP and teach about some features of the software. All other features are explained in the documentation. If you followed this tutorial closely, you should be able to visualize your own data within HiBoP.