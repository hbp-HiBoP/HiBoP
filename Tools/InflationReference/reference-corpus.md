# Corpus de référence pour l’inflation

Ce dossier regroupe les outils Python et les données de comparaison géométrique
produites le 27 août 2026. Il est indépendant de Unity et de `hbp_core` : vérifier
ce corpus ne teste pas l’algorithme actuel de HiBoP. Son
[contrat scientifique et ses tests produit](../../Docs/dev/surface-inflation.md)
sont documentés séparément. Le calcul actuel utilise les buffers anatomiques
déjà transformés, version 2; les transformations du corpus restent des cas de
référence et ne prescrivent pas une nouvelle lecture GIfTI/TRM au runtime.

## Contenu et reproduction

Tous les chemins ci-dessous sont relatifs à `reference-data/`.

| Artefact | Utilité |
|---|---|
| `corpus.json` | Entrées et admissibilité attendue |
| `reference-metrics.json` | Hashes, topologie, bounds, centre, aire, RMS, distributions d’arêtes/aires |
| `external-reference-parameters.json` | Paramètres et provenance des oracles |
| `synthetic-surfaces/` | Surfaces synthétiques déterministes |
| `transforms/` | Oracles rigide, uniforme et anisotrope |
| `workbench-results/` | Manifeste, surfaces inflated/very-inflated et image comparative |

Depuis la racine HiBoP, avec les dépendances Python du script disponibles :

```powershell
python Tools/InflationReference/analyze_reference_corpus.py --verify
python Tools/InflationReference/render_mni_comparison.py --output <image.png>
```

Le collecteur requiert Python 3.10+, NumPy et NiBabel; le rendu requiert également
Matplotlib. `--verify` régénère les surfaces synthétiques et les métriques dans
un répertoire temporaire, contrôle leur cohérence et compare le rapport en
neutralisant les versions/disponibilités d’outils propres à la machine. Il
analyse les résultats Workbench conservés sans les recalculer. Les trois TRM
synthétiques restent en LF via `.gitattributes`, afin de préserver leurs hashes
de référence sur Windows et Unix.

Sans option, le collecteur réécrit les surfaces synthétiques et
`reference-metrics.json`; examiner les écarts avant de remplacer une référence.
Le rendu écrit dans `workbench-results/mni-comparison.png` si `--output` est
omis. Utiliser une sortie distincte pour inspecter une nouvelle image sans
remplacer la preuve existante. Pour régénérer les résultats Workbench explicitement :

```powershell
python Tools/InflationReference/analyze_reference_corpus.py --generate-workbench-references --workbench-executable <chemin-vers-wb_command.exe>
```

Le corpus couvre deux hémisphères MNI et leurs inflated embarqués, trois
substituts patient non cliniques de densités différentes, ainsi que surfaces
ouverte, dégénérée, non-manifold et multicomposante. Il ne contient aucune donnée
clinique. Les classes de `.trm` proviennent des valeurs singulières et du
déterminant, pas du nom de fichier.

## Provenance et résultats historiques

Les chemins opérationnels du corpus, des métriques et des champs source/outputs
du manifeste suivent ce rangement. Les commandes enregistrées dans le manifeste
et les métadonnées internes des GIfTI conservent les chemins de l’exécution
d’origine : ce sont des traces de provenance, pas des commandes à rejouer telles
quelles. Les GIfTI et l’image de référence n’ont pas été réécrits lors du
déplacement. Les commandes ci-dessus utilisent les chemins actuels.

Workbench **2.2.1**, commit `01164ffa47f2778088bd6ff472ec9cd9a57f5b42`, facteur
d’itérations **1,0** : sept surfaces admissibles ont produit inflated et
very-inflated. Le manifeste garde version/hash de l’exécutable, commandes,
hashes d’entrée, stdout/stderr et sorties. La méthode d’origine des inflated
MNI embarqués n’est pas attestée : ils ne sont pas attribués à Workbench ou
FreeSurfer.

| Hémisphère | Sommets / triangles | Aire Workbench / anatomique | Aire historique / anatomique | Étendue XYZ Workbench / anatomique |
|---|---:|---:|---:|---|
| Gauche | 33 036 / 66 068 | 0,511136 | 0,511182 | 1 / 1 / 1 |
| Droit | 33 263 / 66 522 | 0,533895 | 0,512362 | 1 / 1 / 1 |

La topologie est identique; les positions ne sont pas interchangeables.
Workbench conserve l’étendue XYZ tandis que les références embarquées sont plus
contractées. FreeSurfer n’était pas disponible pour cette campagne. La baseline
qualifie les oracles MNI/synthétiques et leur reproductibilité historique, pas
la qualité clinique ni les performances du logiciel actuel. Aucune campagne n’a
été relancée à l’occasion de la consolidation documentaire.
