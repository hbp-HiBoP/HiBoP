# HiBoP XR — audit du prototype HoloLens

**Version :** 0.2  
**Statut :** audit statique et historique terminé ; compilation/appareil non exécutés

## 1. Résultat principal

Le prototype est un fork complet de HiBoP exécuté sur PC avec Microsoft OpenXR App Remoting. Le HoloLens reçoit des pixels et entrées, pas un modèle de rendu ni des résultats scientifiques sérialisés. Il ne fournit donc aucun protocole, client autonome ou port native ARM réutilisable.

Les livrables détaillés sont dans `hololens/` et commencent volontairement par `REVIEW BEFORE COMMIT — derived from closed-source prototype`.

## 2. Identification

| Contrôle | Résultat |
| --- | --- |
| version Unity | 2021.3.13f1 |
| dernière révision | `5a119948`, master, 14 août 2023 |
| XR | Microsoft Mixed Reality OpenXR 1.7.0 |
| interaction | MRTK2 2.8.3 |
| Input System | package 1.4.4, mixed avec code legacy |
| cible réellement scriptée | standalone desktop |
| build autonome UWP/ARM | non trouvé |
| `hbp_core` actuel | absent, créé en 2026 |
| plugins natifs | anciens binaires desktop x86_64 |
| asmdefs | aucun asmdef versionné |
| compilation | non tentée, non nécessaire pour la conclusion architecturale |
| appareil | non connecté |

## 3. Questions obligatoires

1. **Calcul local casque ?** Non. Calcul dans le processus Unity PC.
2. **`hbp_core` UWP/ARM ?** Non. Seuls anciens plugins desktop ont été trouvés.
3. **Données sources, résultats ou pixels ?** Pixels/entrées via App Remoting ; aucun message HiBoP.
4. **Coupes ?** Calcul local dans le processus PC.
5. **Timeline ?** Événements/état locaux au même processus.
6. **Plusieurs cerveaux ?** Liste de scènes et colonnes, avec meshes/objets clonés.
7. **Corrections communes ?** Copie manuelle de modifications HiBoP, puis dérive.

## 4. Traçages end-to-end

| Parcours | Déclencheur | État/calcul | Frontière | Échec/stale |
| --- | --- | --- | --- | --- |
| connexion | formulaire IP/port | `AppRemoting.Connect` | runtime Microsoft | UI revient au formulaire |
| ouverture projet | fichier local | `ProjectManager.LoadProject` | aucune | erreur locale |
| visualisation | choix local | `Module3DMain.LoadScenes` | aucune | erreur locale |
| mesh/sites | chargement/managers | objets Unity PC | pixels seulement | aucune révision |
| grab/scale | input MRTK remoted | transform GameObject PC | entrée remoting | état implicite |
| sélection site | pinch/joints | scan linéaire des sites | entrée remoting | pas de stale |
| coupe | manipulation locale | hbp_export/logic PC | pixels seulement | pas de coalescence |
| timeline | UI/lecture | événements C# locaux | pixels seulement | pas de backlog réseau |
| multi-scènes | projet | liste `Base3DScene` | pixels seulement | aucune compatibilité |
| perte connexion | remoting state | formulaire connexion | runtime Microsoft | pas de resync métier |

Le thread Unity principal possède et applique la majorité des objets. Les APIs natives s'exécutent côté PC. Il n'existe pas de frontière interprocess métier à analyser.

## 5. Renderer et interactions

### Confirmé

- `ObjectManipulator` MRTK sur scènes/colonnes/ROI ;
- interaction mains basée sur joints/pinch ;
- GameObject/MeshRenderer par site ;
- scan O(N) pour site proche ;
- mesh cloné par colonne pour UV mutables ;
- plusieurs visualisations/colonnes ;
- coupes et activité rendues par le renderer HiBoP copié.

### Non prouvé

- 37 500 sites ;
- performance, mémoire ou thermique ;
- contrôleurs ;
- reconnexion sémantique ;
- sécurité/appairage ;
- build et runtime autonome ;
- hôte remoting sur macOS/Linux.

## 6. Dette à ne pas reprendre

- App Remoting ;
- copie massive Core/UI/assets ;
- monolithe `Assembly-CSharp` ;
- dépendances MRTK2 ;
- objets/colliders individuels par site ;
- O(N) par frame ;
- meshes clonés ;
- IDs/indices implicites ;
- absence de scopes, revisions et compatibilité ;
- valeur fixe de cinq vues ;
- préférences réseau en fichier plat comme modèle de confiance.

## 7. Cartographie de réutilisation

| Domaine | Décision |
| --- | --- |
| logique scientifique | réutiliser depuis HiBoP actuel, Desktop |
| concepts multi-cerveaux/ROI | adapter |
| interactions MRTK | réécrire via XRI |
| renderer surface | réimplémenter sous `XR/Assets/` à partir du comportement et des goldens actuels, sans déplacer ni copier le code |
| renderer sites | réécrire |
| protocole | créer |
| client casque | créer |
| ancien fork HoloLens | abandonner comme base de code |

## 8. Limites et suivi

L'audit n'a pas lancé Unity, car les outils Unity MCP n'étaient pas disponibles dans la session et qu'une compilation du prototype obsolète n'aurait pas répondu aux décisions Quest. Aucun fichier du dépôt fermé n'a été modifié.

Les performances et l'UX historique non documentées restent inconnues, pas « satisfaisantes ». Elles sont remplacées par les spikes reproductibles de `04-feasibility-risks-and-spikes.md`.
