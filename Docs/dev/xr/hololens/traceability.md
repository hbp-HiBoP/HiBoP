# REVIEW BEFORE COMMIT — derived from closed-source prototype

# Audit HoloLens — traçabilité des preuves

Les chemins `HiBoP_HoloLens/` désignent le dépôt local fermé audité. Aucun contenu propriétaire n'est reproduit ici.

| Affirmation | Source principale |
| --- | --- |
| connexion App Remoting | `HiBoP_HoloLens/Assets/Scripts/HBP/UI/Main/File/ConnectToHoloLens.cs` |
| configuration IP/port/codec/audio | `HiBoP_HoloLens/Assets/Scripts/HBP/HoloLens/ConnectForm.cs` |
| chargement local du projet | `HiBoP_HoloLens/Assets/Scripts/HBP/HoloLens/ProjectManager.cs` |
| état de connexion et formulaires | `HiBoP_HoloLens/Assets/Scripts/HBP/HoloLens/ConfigurationManager.cs` |
| build standalone, pas UWP/ARM | `HiBoP_HoloLens/Assets/Scripts/HBP/Dev/Editor/HBPBuilder.cs` |
| versions MRTK/OpenXR/Input System | `HiBoP_HoloLens/Packages/manifest.json` et lock |
| version Unity | `HiBoP_HoloLens/ProjectSettings/ProjectVersion.txt` |
| manipulation MRTK des scènes | `HiBoP_HoloLens/Assets/Scripts/HBP/HoloLens/Module3D/Base3DScene.cs` |
| manipulation/scan O(N) des sites | `HiBoP_HoloLens/Assets/Scripts/HBP/HoloLens/Module3D/Column3D.cs` |
| plusieurs scènes/visualisations | `HiBoP_HoloLens/Assets/Scripts/HBP/HoloLens/Module3D/Module3DMain.cs` |
| préférences réseau persistées | `HiBoP_HoloLens/Assets/Scripts/HBP/HoloLens/Preferences/UserPreferences.cs` |
| copie historique de HiBoP | historique Git du prototype, commits 2022–2023 |
| renderer sites courant toujours par objets | `HiBoP/Assets/Scripts/HBP/Data/Module3D/Modules/ImplantationManager.cs` et `DisplayedObjects.cs` |
| picking courant par Physics | `HiBoP/Assets/Scripts/HBP/Data/Module3D/Column3D.cs` |
| legacy Input Manager courant | `HiBoP/ProjectSettings/ProjectSettings.asset` |
| frontières trop larges de Core/Data | `HiBoP/Assets/Scripts/HBP/Core/HBP.Core.Runtime.asmdef`, `Data/HBP.Data.Runtime.asmdef` |
| projection dynamique/timeline | `HiBoP/Assets/Scripts/HBP/Data/Module3D/Column3DDynamic.cs` et `Core/Data/Loaded/Processed/Timeline.cs` |
| génération surface/coupes | `HiBoP/Assets/Scripts/HBP/Core/DLL/Generators/SurfaceGenerator.cs`, `CutGenerator.cs` |
| portabilité native | `hbp_core/CMakeLists.txt`, `src/core/parallel_for.cpp`, `src/generators/cut_generator.cpp` |
| ABI/version native | `hbp_core/include/hbp_core.h`, `src/core/hbp_core.cpp` |

## Historique minimal

| Révision | Signification |
| --- | --- |
| `f0e64ff` | initialisation MRTK |
| `39e96c1` | ajout d'anciens binaires natifs desktop |
| `33d2f06` | mise en place du remoting |
| `999350d` | ajout de la copie complète de HiBoP |
| `5a119948` | dernière révision auditée |

## Limites de l'audit

- aucune compilation du prototype ;
- aucun HoloLens connecté ;
- aucun profilage historique disponible ;
- aucune affirmation de compatibilité des dépendances obsolètes avec Unity/Quest actuel ;
- aucune valeur sensible de configuration Git reproduite ;
- les chiffres Quest restent des gates à mesurer, non des conclusions de cet audit.
