# HiBoP: Human Intracranial Brain Observation Player

![](https://github.com/hbp-HiBoP/HiBoP/raw/master/Media/Icon.png "HiBoP Icon")

HiBoP is an application dedicated to the visualization of intracranial brain recordings such as intracranial electroencephalography (iEEG), cortico-cortical evoked potentials (CCEP) and functional magnetic resonance imaging (fMRI). This software is developped using [Unity](https://unity.com/), C# and C++ for Windows, Mac and Linux.

![](https://github.com/hbp-HiBoP/HiBoP/raw/master/Media/GUI.png "HiBoP Main GUI")

![](https://github.com/hbp-HiBoP/HiBoP/raw/master/Media/ROI.png "HiBoP ROI Example")

## Getting Started

These instructions will allow you to run HiBoP on your computer. Once it is running, you can take a look at the [tutorial](https://github.com/hbp-HiBoP/HiBoP/blob/master/Tutorial/Tutorial.md).

### Windows

Unzip the zip file and execute HiBoP.exe.

### Linux

Unzip the zip file to a target directory.
Then, you need to give execution rights to the executable, and execute HiBoP.x86_64.

```
cd <HIBOP_DIR>
chmod 755 ./HiBoP.x86_64
./HiBoP.x86_64
```

### MacOS

Unzip the zip file to a target directory.
Then, you need to give execution rights to the executable, remove external attributes, and execute HiBoP.app.

```
cd <HIBOP_DIR>
chmod -R 755 HiBoP.app
xattr -rc HiBoP.app
```

## Supported file formats

### Anatomical data

#### 3D brain mesh

*  **Brain mesh (required)**: Either one file for the whole brain or one file per hemisphere in GIFTI format (.gii)
*  **Mars Atlas (optional)**: Either one file for the whole brain or one file per hemisphere in GIFTI format (.gii)
*  **Transformation (optional)**: One file specifying the transformation matrix to be applied so the mesh is in the same referential as the anatomical MRI (.trm)

#### MRI

*  **MRI (required)**: One file in NIFTI-1 format (.nii, .nii.gz or .hdr/.img)

#### Electrodes

*  **Electrodes positions (required)**: One file describing the electrodes positions either in IntrAnat format (.pts) or BIDS electrodes format (.tsv)
*  **Electrodes additions information (optional)**: One file giving information about the electrodes (.csv)

### Functional data

#### iEEG and CCEP

*  **Data files (required)**: Supported formats are BrainVision (.vhdr/.vmrk/.eeg), ELAN (.eeg.ent/.eeg), Micromed (.TRC) and EDF (.edf)

#### fMRI

*  **Data files (required)**: One file in NIFTI-1 format (.nii, .nii.gz or .hdr/.img)

## License

This work is licensed under a [CC Attribution-NonCommercial 4.0 International](http://creativecommons.org/licenses/by-nc/4.0/) License.

## Acknowledgements

### Human Brain Project

This open source software code was developed in part or in whole in the [Human Brain Project](https://www.humanbrainproject.eu/en/), funded from the European Union’s Horizon 2020 Framework Programme for Research and Innovation under the Specific Grant Agreement No. 785907 (Human Brain Project SGA2).

### Used third-party resources

#### Unity / C\#

*  [DotNetZip](https://archive.codeplex.com/?p=dotnetzip)
*  [Unity Standalone File Browser](https://github.com/gkngkc/UnityStandaloneFileBrowser)
*  [Unity UI Extensions](https://bitbucket.org/UnityUIExtensions/unity-ui-extensions)

#### C++

*  [Boost](https://www.boost.org/)
*  [zlib](https://www.zlib.net/)
*  [OpenCV](https://opencv.org/) with [Plot contribution](https://github.com/opencv/opencv_contrib/tree/master/modules/plot)
*  [Json for Modern C++](https://github.com/nlohmann/json)
*  [NiftiLib](http://niftilib.sourceforge.net/)
*  [Fast Quadric Mesh Simplification](https://github.com/sp4cerat/Fast-Quadric-Mesh-Simplification)
*  [GPC](http://www.cs.man.ac.uk/~toby/gpc/)