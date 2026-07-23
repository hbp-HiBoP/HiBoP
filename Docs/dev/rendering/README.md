# Migration du rendu HiBoP vers URP

## Statut et portée

**Statut :** base de travail validable avant implémentation  
**Projet audité :** HiBoP sous Unity `6000.5.2f1`  
**Date de l'audit initial :** 2026-07-23  
**Pipeline actuel :** Built-in Render Pipeline  
**Pipeline cible recommandé :** Universal Render Pipeline (URP), Universal Renderer, chemin Forward

Ce dossier constitue la référence de conception de la migration du rendu de
HiBoP. Il doit être lu avant toute modification du pipeline, d'un shader, d'un
matériau, d'une caméra, d'une RenderTexture, d'un effet de contours ou d'un
export d'image.

La priorité n'est pas de rendre l'application « plus jolie ». La priorité est de
préserver la lecture scientifique, l'identité visuelle et les fonctionnalités,
puis d'améliorer le rendu et les performances de manière mesurée.

## Décisions déjà prises

Les décisions suivantes sont considérées comme acquises tant qu'elles ne sont
pas explicitement amendées dans ce dossier :

1. URP est la cible par défaut. HDRP et un SRP entièrement personnalisé ne
   répondent pas au compromis plateformes, coût de maintenance et GPU minimal.
2. Le renderer initial sera le Universal Renderer en **Forward**. Deferred et
   Forward+ ne sont pas justifiés par le faible nombre de lumières et
   compliqueraient la parité, le MSAA et les plateformes.
3. Le projet reste en espace colorimétrique **Linear**.
4. Les couleurs d'atlas, de fMRI, d'iEEG et des autres données scientifiques
   sont des données, pas un matériau décoratif. Leur couleur affichée doit être
   indépendante de l'éclairage, de l'orientation de la surface et de la caméra.
5. Le cerveau anatomique peut rester éclairé. Les overlays scientifiques sont
   composités après le calcul de cet éclairage.
6. L'export d'une vue doit correspondre à l'affichage, à l'exception explicite
   du fond 3D qui doit être transparent dans l'export PNG individuel.
7. La livraison se fait en deux temps :
   - migration technique et parité fonctionnelle ;
   - application du contrat scientifique, modernisation visuelle et
     optimisations mesurées.
8. Le chemin de rendu des sites est un chantier dédié. Il peut y avoir jusqu'à
   30 000 sites et il est interdit de remplacer son shader par un shader URP
   générique sans profilage comparatif.
9. Le dimensionnement doit couvrir 8 colonnes × 3 vues en usage réaliste et
   12 colonnes × 5 vues en limite théorique.
10. Les plateformes visées sont Windows, macOS, Linux et la VR. WebGL reste
    provisoire et ne doit pas imposer des compromis tant que son statut n'est
    pas décidé, mais les alternatives nécessaires doivent être documentées.

## Ordre de lecture

1. [01-current-rendering-audit.md](01-current-rendering-audit.md) : état réel du
   projet, preuves et dettes découvertes.
2. [02-visual-and-color-contract.md](02-visual-and-color-contract.md) : règles
   normatives du rendu accepté.
3. [03-urp-target-architecture.md](03-urp-target-architecture.md) : architecture
   technique visée.
4. [04-migration-roadmap.md](04-migration-roadmap.md) : lots, dépendances,
   jalons et conditions de sortie.
5. [05-validation-and-reference-captures.md](05-validation-and-reference-captures.md) :
   validation automatique et contrôles humains.
6. [06-performance-plan.md](06-performance-plan.md) : protocole de mesure et
   optimisations possibles.
7. [07-risk-register.md](07-risk-register.md) : risques, signaux et mitigations.
8. [08-agent-implementation-checklist.md](08-agent-implementation-checklist.md) :
   checklist opérationnelle destinée à l'agent d'implémentation.
9. [09-open-questions.md](09-open-questions.md) : décisions encore ouvertes,
   valeur par défaut temporaire et jalon où elles deviennent bloquantes.

## Hiérarchie des exigences

En cas de conflit, appliquer cet ordre :

1. exactitude scientifique des données et des couleurs ;
2. absence de régression fonctionnelle ;
3. correspondance entre affichage et export ;
4. compatibilité Windows, macOS, Linux et VR ;
5. performances sur le GPU minimal ;
6. proximité avec le rendu Built-in historique ;
7. modernisation esthétique.

Cette hiérarchie signifie notamment qu'une différence volontaire corrigeant
l'influence de la lumière sur une couleur scientifique est acceptable et même
requise, bien qu'elle ne soit pas une parité stricte avec le rendu historique.
Elle doit toutefois être documentée et validée visuellement.

## Gouvernance documentaire

- Toute décision qui modifie le contrat visuel, les plateformes ou les budgets
  doit mettre à jour ce dossier dans le même changement de code.
- Toute hypothèse non encore vérifiée doit rester marquée **À confirmer**.
- Un résultat de profilage doit préciser machine, GPU, résolution, nombre de
  colonnes, nombre de vues, nombre de sites et scène utilisée.
- Une capture de référence doit suivre le protocole de
  `05-validation-and-reference-captures.md`.
- Les éléments de scène et d'UI doivent respecter le workflow prefab-first du
  projet. Une référence manquante ne doit pas être compensée par la création
  d'un `GameObject` au runtime.
- Le Render Pipeline Converter est un outil d'assistance, pas une preuve de
  correction. Les shaders custom et les effets spécifiques exigent une
  migration manuelle.

## Sources Unity

- [Stratégie Unity 2026 pour les render pipelines](https://unity.com/topics/render-pipelines-strategy-for-2026)
- [Render Pipeline Converter dans Unity 6.5](https://docs.unity3d.com/Manual/urp/features/rp-converter.html)
- [Migration manuelle d'un shader Built-in vers URP](https://docs.unity3d.com/Manual/urp/urp-shaders/birp-urp-custom-shader-upgrade-guide.html)
- [Compatibilité du post-processing avec URP](https://docs.unity3d.com/Manual/urp/integration-with-post-processing.html)
- [Comparaison des chemins de rendu URP](https://docs.unity3d.com/Manual/urp/rendering-paths-comparison.html)
