# Phase 0 — Contrat produit et baseline d'inflation

## Statut et portée

**Statut : validée le 27 août 2026 pour le corpus MNI et synthétique.**

Cette phase fournit les artefacts reproductibles préalables à l'algorithme
d'inflation. Elle ne modifie aucun comportement produit et ne contient aucune
donnée clinique.

Les artefacts normatifs sont :

- `corpus.json`, inventaire des surfaces et attentes d'admissibilité ;
- `reference-metrics.json`, mesures géométriques et classification des `.trm` ;
- `external-reference-parameters.json`, commandes et mesures des oracles ;
- `product-contract.json`, machine d'état, invariants et textes français ;
- `external-references/workbench/`, sorties et manifeste Workbench 2.2.1 ;
- `fixtures/`, surfaces synthétiques déterministes générées par le collecteur ;
- `transforms/`, oracles rigide, uniforme et anisotrope.

## Reproduction

Depuis la racine de HiBoP :

```powershell
python Tools/InflationBaseline/phase0_baseline.py
python Tools/InflationBaseline/phase0_baseline.py --verify
python Tools/InflationBaseline/render_mni_comparison.py
```

Le script requiert Python 3.10 ou ultérieur, NumPy et NiBabel. Ces deux
dépendances figurent déjà dans les dépendances des fixtures natives HiBoP.
`--verify` régénère le corpus dans un répertoire temporaire, exécute les
assertions de gate et compare le rapport logique au rapport versionné. Les
versions Python et la disponibilité des outils externes, propres à la machine
ayant produit la référence, sont conservées mais neutralisées dans cette
comparaison géométrique.

## Corpus

Le corpus couvre les deux hémisphères MNI embarqués et leurs variantes inflated
historiques, trois substituts non cliniques de surfaces patient à densités
différentes, une surface ouverte, une surface dégénérée, une surface
non-manifold et une surface à deux composantes connexes.

Les substituts patient servent aux tests déterministes et aux mesures de charge.
Ils ne prétendent pas représenter une anatomie réelle. Les contraintes de
confidentialité excluent les GIFTI patient du dépôt et de cette baseline : le
MNI est l'oracle anatomique accepté pour la phase 0. Cette décision permet de
fermer la gate technique, sans prétendre démontrer la généralisation clinique à
tous les maillages patient.

Chaque entrée archive le SHA-256 du fichier et de sa topologie, les nombres de
sommets/triangles/composantes, les défauts topologiques, la bounding box, le
centroïde, l'aire, le rayon RMS et les percentiles P50/P90/P95/P99/max des
longueurs d'arêtes et aires de triangles.

## Références externes

Les variantes `MNI_*white_inflated.gii` embarquées constituent l'oracle local
immédiat et leur identité topologique avec l'anatomique est vérifiée dans le
rapport. Leur origine algorithmique n'étant pas attestée dans le dépôt, elles
ne sont pas étiquetées comme sorties Workbench ou FreeSurfer.

Les références ont été générées avec Connectome Workbench 2.2.1, commit
`01164ffa47f2778088bd6ff472ec9cd9a57f5b42`, depuis l'installation locale
`C:\HBP\Utility\workbench`. La commande reproductible est :

```powershell
python Tools/InflationBaseline/phase0_baseline.py `
  --generate-workbench-references `
  --workbench-executable C:\HBP\Utility\workbench\bin_windows64\wb_command.exe
```

Les sept surfaces admissibles ont produit leurs variantes inflated et
very-inflated avec un code de sortie nul. Le manifeste conserve la version et le
hash de l'exécutable, les commandes exactes, `iterations-scale = 1.0`, les
hashes d'entrée, stdout et stderr. `reference-metrics.json` conserve les hashes
et métriques des sorties ainsi que les ratios par rapport à la source.

| Hémisphère | Sommets | Triangles | Aire Workbench/anatomique | Aire historique/anatomique | Étendue XYZ Workbench/anatomique |
| --- | ---: | ---: | ---: | ---: | --- |
| Gauche | 33 036 | 66 068 | 0,511136 | 0,511182 | 1 / 1 / 1 |
| Droit | 33 263 | 66 522 | 0,533895 | 0,512362 | 1 / 1 / 1 |

La topologie est strictement identique dans tous les cas. L'inspection de
`mni-comparison.png` confirme que les deux références rendent les sillons plus
visibles. Workbench produit une enveloppe plus lisse et conserve exactement
l'étendue XYZ ; la référence historique est plus contractée et conserve plus de
relief résiduel. Les positions ne sont donc pas interchangeables, même lorsque
les aires globales sont proches.

FreeSurfer n'est pas installé. Il reste une seconde référence facultative : son
absence ne bloque pas cette gate, puisque la référence Workbench et les deux
hémisphères MNI sont maintenant archivés et reproductibles.

## Transformations et repère de calcul

Les `.trm` sont classés à partir des valeurs singulières de leur matrice
linéaire : rigide si elles valent toutes 1, uniforme si elles sont égales entre
elles, anisotrope sinon. Le déterminant et une éventuelle inversion
d'orientation sont aussi enregistrés.

Le contrat des phases suivantes est : inflation dans le repère GIFTI natif,
puis application du même `.trm` à l'anatomique et à l'inflated. Une surface
disponible uniquement en mémoire est nécessairement gonflée dans son repère
courant ; cette provenance devra être exposée dans son rapport.

## Machine d'état produit

`product-contract.json` formalise six états et traite la présence de coupes
comme une dimension indépendante de leur effet sur l'enveloppe affichée. La
propriété centrale cible est :

```text
CutsAffectDisplayedBrain =
    StableRepresentation == Anatomical
    && Transition == None
    && Cuts.Count > 0
```

Créer, modifier ou supprimer une coupe reste autorisé dans tous les états. Les
coupes continuent d'alimenter les vues anatomiques en mode inflated, mais le
clipping du cerveau est nul en mode inflated et pendant les transitions. Au
retour à l'anatomique, les coupes courantes prennent effet uniquement lorsque
l'état final est atteint.

Les sites et électrodes restent à leurs coordonnées anatomiques. Leur influence
et toutes les projections scientifiques utilisent la surface anatomique de
référence. Les quatre textes français définitifs à intégrer à l'interface sont
versionnés dans le même contrat.

## Gate de sortie

La phase 0 est satisfaite parce que :

- `phase0_baseline.py --verify` réussit ;
- les deux paires MNI ont des nombres de sommets/triangles et un hash de
  topologie strictement identiques ;
- les fixtures attendues ouvertes, dégénérées, non-manifold et multicomposantes
  présentent effectivement les caractéristiques annoncées ;
- les trois classes de `.trm` sont observées ;
- les références Workbench MNI réussissent, gardent exactement leur topologie
  et possèdent leurs hashes et métriques archivés ;
- le contrat produit contient exactement les six états et n'interdit aucune
  opération de coupe.

La limitation résiduelle est explicite : cette baseline valide le comportement
sur MNI et sur les oracles synthétiques, pas la qualité anatomique sur un corpus
patient réel. Cette restriction de données ne bloque pas la phase suivante,
mais devra rester visible dans les conclusions du prototype.
