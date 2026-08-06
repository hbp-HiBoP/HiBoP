# Validation et captures de référence

## 1. Philosophie

La validation combine :

- tests déterministes de données/couleurs ;
- comparaisons d'images ;
- tests fonctionnels Unity ;
- profilage ;
- revue visuelle humaine.

Une comparaison pixel à pixel globale n'est pas un objectif de migration,
notamment entre modèles d'éclairage. Elle est en revanche pertinente pour les
patchs scientifiques et certains exports offscreen.

## 2. Convention d'artefacts

Chemin recommandé, hors `Assets` si les images ne doivent pas être importées :

```text
.test-results/rendering/
  baseline-birp/
  urp-parity/
  urp-scientific/
  reports/
```

Nom :

```text
<pipeline>__<platform>__<gpu>__<case>__<columns>x<views>__<width>x<height>__<state>.png
```

Chaque série possède un fichier JSON ou Markdown compagnon indiquant :

- commit ;
- version Unity ;
- build/editor ;
- OS, API graphique, GPU, driver ;
- résolution et scaling OS ;
- niveau de qualité ;
- scène/configuration ;
- caméra ;
- nombre de colonnes, vues et sites ;
- paramètres atlas/activité/coupes/transparence/contours ;
- date et auteur du verdict humain.

## 3. Protocole de capture

1. Utiliser un dataset/version figé et identifiable. La référence réelle
   privilégiée est `visu_full_test` / visualisation `Small` lorsqu'elle est
   disponible sur la machine.
2. Charger une configuration enregistrée, pas une manipulation approximative.
3. Attendre la fin des chargements, calculs et animations.
4. Fixer timeline, caméra, taille de vue et thème.
5. Capturer deux frames consécutives ; elles doivent être identiques hors
   éléments explicitement animés.
6. Capturer la vue à l'écran/offscreen.
7. Exporter le PNG individuel.
8. Conserver l'image brute sans passage par un logiciel qui applique un profil
   ou réencode la couleur.
9. Exécuter le rapport de comparaison.
10. Demander le verdict humain pour les cas signalés dans le contrat.

## 4. Matrice de cas

| ID | Cas | Variantes obligatoires |
| --- | --- | --- |
| ANAT-01 | cerveau anatomique opaque | face, profil, zoom |
| ANAT-02 | cerveau transparent | alpha faible/moyen/fort |
| CUT-01 | coupe anatomique | 1 plan, plusieurs plans, strong cut |
| ATLAS-01 | atlas mesh + coupe | neutre, hover, sélection |
| FMRI-01 | fMRI | négatif, zéro, positif, seuils |
| ACT-01 | activité | deux instants et interpolation |
| SITE-01 | sites | petit, usuel, 30 000 |
| ROI-01 | ROI | normal, sélectionné, transparent |
| EDGE-01 | contours | off/on, opaque/transparent |
| EXP-01 | export individuel | fond alpha 0 |
| EXP-02 | export composite | fond #282828 |
| GRID-01 | grille | 1×1, 8×3, 9×3 ; extrême comme robustesse |

## 5. Tests colorimétriques déterministes

Créer un fixture affichant des patchs avec les mêmes valeurs par :

- couleur uniforme shader ;
- vertex color ;
- texture sRGB ;
- texture Linear ;
- palette/LUT atlas ;
- texture de coupe générée.

Inclure au minimum :

- noir, blanc et gris 0.5 ;
- primaires et secondaires ;
- couleurs de palette HiBoP représentatives ;
- couleurs proches des seuils fMRI/iEEG ;
- alpha 0, 0.25, 0.5, 0.75 et 1.

### Tests d'invariance

Pour chaque patch scientifique opaque :

- rendre avec trois orientations de normale ;
- rendre avec trois orientations de lumière ;
- rendre avec deux intensités de lumière ;
- rendre sur mesh et coupe ;
- comparer le RGB central, hors anti-aliasing.

Le RGB doit rester dans la tolérance du contrat.

## 6. Comparaisons d'images

Le rapport devrait produire :

- image de référence ;
- image candidate ;
- différence absolue ;
- heatmap ;
- pourcentage de pixels au-dessus de plusieurs seuils ;
- histogrammes RGB/luminance ;
- SSIM comme indicateur secondaire ;
- échantillons `ΔE00` pour les zones/palettes scientifiques.

### Masques

Utiliser des masques séparés :

- fond ;
- anatomie ;
- overlay scientifique ;
- bords anti-aliasés ;
- UI éventuelle.

Les seuils peuvent différer selon le masque. Ne jamais laisser une bonne moyenne
sur le fond masquer une erreur importante sur une petite région scientifique.

## 7. Tests Unity

### EditMode

- conversion sRGB/Linear de palettes ;
- mapping valeur -> colormap ;
- seuils et clamp ;
- création des RenderTextureDescriptor ;
- sérialisation des réglages de rendu ;
- allocation/libération des ressources gérées par des classes testables.

### PlayMode

- chargement d'une vue URP ;
- changement atlas/activité ;
- ajout/retrait de plans de coupe ;
- changement opaque/transparent ;
- redimensionnement répété ;
- export PNG et lecture de l'alpha ;
- recomposition d'un PNG straight alpha sur fond blanc et `#282828` ;
- création/suppression de colonnes et vues ;
- contours on/off ;
- sélection d'un site.

Les tests async doivent suivre les règles de `AGENTS.md` : `async Task`, `await`
direct, aucun `.Wait()`, `.Result`, busy-wait ou assertion NUnit async bloquante.

## 8. Contrôles mémoire

Pour 100 cycles de redimensionnement/création/suppression :

- le nombre de RenderTextures vivantes doit revenir au niveau attendu ;
- la mémoire GPU ne doit pas croître continuellement ;
- aucun matériau dynamique ne doit croître sans borne ;
- aucun renderer/site orphelin ne doit rester ;
- les ressources temporaires d'export doivent être détruites.

## 9. Performance

Chaque capture de performance doit utiliser :

- build Development avec Profiler approprié, et build non Development pour la
  confirmation finale ;
- même résolution et VSync/target frame rate ;
- warm-up documenté ;
- durée minimale suffisante pour médiane, P95 et P99 ;
- marqueurs séparés pour mise à jour scientifique, rendu des vues, sites,
  génération/copie des coupes et export.

Le protocole détaillé se trouve dans `06-performance-plan.md`.

## 10. Validation humaine

Formulaire minimal :

```text
Cas :
Référence :
Candidate :
Verdict : accepté / accepté avec écart documenté / refusé
Anatomie :
Couleurs scientifiques :
Coupes :
Transparence :
Contours :
Export :
Commentaire :
Validateur et date :
```

Un refus doit être associé à une capture et à une description observable, pas
seulement « différent ».

La validation Built-in/URP du cerveau est perceptuelle. Une comparaison pixel
par pixel globale n'est jamais une gate. Les comparaisons strictes sont
réservées aux patchs scientifiques, aux mappings et aux invariants d'export.

## 11. Unity MCP pendant l'implémentation

Lorsque l'éditeur est ouvert :

1. lire `mcpforunity://instances`, `editor/state`, `project/info` et les
   ressources pertinentes ;
2. sélectionner l'instance HiBoP si plusieurs instances existent ;
3. vérifier que la compilation est terminée ;
4. utiliser les outils Unity MCP pour tests et captures ;
5. lire la console avant et après les runs.

Quand l'éditeur est fermé, utiliser le CLI officiel de la version indiquée par
`ProjectSettings/ProjectVersion.txt`. Toute commande lançant `Unity.exe` doit
être exécutée hors sandbox conformément à `AGENTS.md`.
