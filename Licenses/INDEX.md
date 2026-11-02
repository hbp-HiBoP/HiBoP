# Licenses and credits

HiBoP's own code is distributed under [BSD-3-Clause](../LICENSE).
Its dependencies retain their separate licenses and copyright notices.

## HiBoP dependencies

These components are used directly by the Unity/C# application. Copies of their
notices are collected in `hibop/`; notices supplied alongside the original
packages and source files are also retained.

| Component | Use | License / notice |
| --- | --- | --- |
| DotNetZip | ZIP archives | [Ms-PL](hibop/DotNetZip.txt) |
| Unity Standalone File Browser | Desktop file dialogs | [MIT](hibop/StandaloneFileBrowser.txt) |
| Unity UI Extensions | UI controls | [MIT](hibop/unity-ui-extensions.txt) |
| UniTask | Asynchronous operations | [MIT](hibop/UniTask.txt) |
| Newtonsoft.Json | JSON serialization | [MIT](hibop/Newtonsoft.Json.txt); [Unity package terms](hibop/Unity.Newtonsoft.Json.txt) |
| ExcelDataReader | Spreadsheet import | [MIT](hibop/ExcelDataReader.txt) |
| BouncyCastle | Cryptography and certificates | [MIT](hibop/BouncyCastle.txt) |
| Makaretu mDNS / DNS | Local device discovery | [mDNS MIT](hibop/QuestMdns.txt), [DNS MIT](hibop/Dns.txt) |
| Common.Logging | Logging used by discovery components | [Apache-2.0](hibop/Common.Logging.txt) |

Unity and its packages retain their respective terms and bundled notices.

## Current native libraries

These are separate projects. HiBoP's BSD-3-Clause license does not apply to
their own code, and contributor credits do not replace existing copyright notices.

| Library | Use | Current status |
| --- | --- | --- |
| hbp_core | Scientific geometry, surfaces, volumes, projection and transfer codecs | Closed-source and proprietary; a future BSD-3-Clause release is being considered, with no current BSD grant |
| hbp_math | Statistical calculations | Closed-source and proprietary; a future BSD-3-Clause release is being considered, with no current BSD grant |
| EEGFormat | Recording-format readers | Closed-source and proprietary; will remain proprietary. Contributors: **Benjamin BONTEMPS and Florian SIPP** |

Their incorporated third-party components retain the following terms:

| Component | Used by / purpose | License |
| --- | --- | --- |
| zlib | hbp_core / compression | Zlib |
| Expat | hbp_core / XML parsing | MIT |
| nifti_clib, gifti_clib | hbp_core / medical imaging formats | Public-domain notices |
| nlohmann/json | hbp_core / JSON parsing | MIT |
| earcut.hpp | hbp_core / polygon triangulation | ISC |
| Fast Quadric Mesh Simplification, MC33++ | hbp_core / mesh processing and generation | MIT |
| Zstandard | hbp_core / compression | BSD-3-Clause |
| LZ4 | hbp_core / compression | BSD-2-Clause |
| Mbed TLS | hbp_core / cryptography | Apache-2.0 |
| UTF8-CPP | EEGFormat / text encoding | Boost Software License 1.0 |

Full notices: [hbp_core](native/hbp_core/THIRD_PARTY_NOTICES.md) and
[EEGFormat / UTF8-CPP](native/EEGFormat/UTF8-CPP.txt).
The native CI importer refreshes these copies from the selected source revisions
and copies standalone project licenses when present. hbp_math currently has no
standalone license file to copy; its proprietary status is recorded above.

## Historical native dependencies

The old hbp_export binaries remain under `Assets/Plugins/Editor/LegacyNative`
for editor comparisons and are excluded from product builds. Their existing
notices are retained in `native-legacy/`:

| Component | Historical notice |
| --- | --- |
| Boost | [Boost Software License](native-legacy/boost.txt) |
| OpenCV | [BSD notice](native-legacy/opencv.txt) |
| OpenCV Plot contribution | [BSD notice](native-legacy/plot_opencv_contrib.txt) |
| GPC | [Non-commercial license](native-legacy/gpc.txt) |
| zlib | [Zlib notice](native-legacy/zlib.txt) |
| nlohmann/json | [MIT](native-legacy/json.txt) |
| NIfTI | [NIfTI notice](native-legacy/nifti1.txt) |
| Fast Quadric Mesh Simplification | [Historical attribution](native-legacy/FastQuadricMeshSimplification.txt) |

These historical copies describe the former integration, not the current
hbp_core dependencies.
