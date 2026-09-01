# REVIEW BEFORE COMMIT — derived from closed-source prototype

# Audit HoloLens — composants réutilisables

## Réutilisables depuis HiBoP courant, pas depuis la copie

- logique scientifique et wrappers `hbp_core` côté Desktop ;
- modèles sémantiques après extraction d'un contrat pur ;
- choix `TemporalSample`, après correction/validation de l'interpolation de surface ;
- palettes, matériaux et shaders qui passent le spike Android/OpenXR ;
- IDs et règles de sélection une fois rendus stables ;
- notions de colonne, visualisation, coupe, ROI et timeline.

Le prototype prouve que ces concepts ont une valeur XR, mais son code copié est obsolète et divergent. La source d'extraction doit être le HiBoP courant.

## À adapter

| Composant historique | Valeur | Adaptation |
| --- | --- | --- |
| `ObjectManipulator` sur scène/colonne/ROI | grab, rotation, échelle | actions XRI et état spatial local |
| pinch proche du site | interaction naturelle | index spatial, tolérance liée à l'échelle |
| plusieurs `Base3DScene` | coexistence de visualisations | `Visualization` + `BrainInstance` |
| colonnes affichées séparément | comparaison | assets partagés + buffers par colonne |
| formulaire IP/port | secours réseau utile | appairage sécurisé + IP manuelle |
| feedback de connexion | état utilisateur | machine d'état session/reconnexion |

## À réécrire

- client Quest Android et bootstrap OpenXR ;
- protocole, transport, appairage, snapshot et resync ;
- renderer de sites bufferisé et picking ;
- backend de rendu qui consomme `RenderModel` sans dépendre de `Base3DScene` ;
- pipeline de frames dynamiques et de coupes latest-wins ;
- interactions mains/contrôleurs et UI XR ;
- gestion d'assets par hash et de révisions ;
- isolation de la persistance et redaction des logs.

## À abandonner

- Microsoft OpenXR App Remoting comme chemin produit ;
- copie complète de `Assets/Scripts/HBP` ;
- `Assembly-CSharp` monolithique ;
- anciens binaires `hbp_export` et plugins x86_64 dans un client casque ;
- clone de Mesh complet par colonne pour modifier les UV ;
- GameObject/MeshRenderer/collider par site ;
- scan linéaire des sites pendant chaque frame de pinch ;
- dépendance directe aux joints/gestes MRTK2 ;
- valeur fixe de cinq vues comme limite produit.

## Frontière de package recommandée

```text
Shared/Packages/
  com.crnl.hibop.contracts      C# pur, AOT-safe
  com.crnl.hibop.render-model   types de buffers/assets/résultats
  com.crnl.hibop.protocol       enveloppes, schéma, compatibilité

Assets/                         Desktop uniquement : bridge HiBoP/Core/Data
XR/Assets/                      XR uniquement : client, rendu, OpenXR et Meta
```

Une classe n'entre dans un package partagé que si elle compile et se teste dans les deux projets sans dépendance transitive Desktop ou Meta non déclarée. La baseline limite `Shared/Packages/` aux trois packages ci-dessus et place le renderer P05 sous `XR/Assets/`. Toute extension exige une réouverture explicite de D03 et un ADR distinct ; aucun code HiBoP existant n'est déplacé.
