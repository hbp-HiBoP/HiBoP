# Vérifier le rendu et interpréter les mesures

Cette référence décrit les contrôles réutilisables et une comparaison historique
qui explique les choix du renderer. Elle ne certifie pas le build courant.
Pour lancer Unity, suivre le
[workflow de validation](../../../.agents/skills/hibop-unity-validation/SKILL.md).

## Campagne reproductible

Le collecteur permanent est
[RenderingBaselineCapture](../../../Assets/Scripts/HBP/Dev/Rendering/RenderingBaselineCapture.cs).
Il charge `visu_full_test.hibop` / `Small`; un autre chemin peut être fourni par
`HIBOP_RENDERING_BASELINE_PROJECT`. Il écrit captures, manifeste et
`latest-run.txt` sous `.test-results/rendering/`, hors documentation versionnée.
Ces résultats locaux peuvent être absents d’un autre checkout.

Fixer source/build, dataset, caméra, timeline, paramètres scientifiques, taille
physique des vues, résolution et thème. Attendre chargements/animations puis
vérifier deux captures stables. Le manifeste doit identifier Unity, OS, GPU,
driver/API, qualité, colonnes/vues/sites, VSync/focus et verdict humain.
Mesurer sur PNG brut SDR, sans gestion de couleur externe. Le collecteur suspend
le mode idle et restaure l’état; une série `IdleThrottled` ou non `Normative`
ne sert pas à conclure sur les performances.

| Contrôle | Cas nécessaires |
|---|---|
| Données/couleur | Patchs uniforme/vertex/texture sRGB/Linear, LUT, seuils positifs/négatifs, alpha, interpolation temporelle |
| Géométrie | Anatomie opaque/transparente, extrusion, clipping 0/1/20, inflation et coupes |
| Interaction | Atlas hover/sélection, sites filtrés/blacklistés, picking, ROI normale/sélectionnée |
| Contours | Off/on, relief opaque/silhouette transparente, exclusion sites/ROI/repères |
| Export | PNG individuel/composite/vidéo, alpha de fond, recomposition sur blanc et `#282828`, restauration d’état |
| Ressources | 100 cycles resize/export; 1×1, 8×3, 9×3 vues; retour au plateau des ressources possédées |

Tolérances de référence : patchs scientifiques plats à une unité par canal 8 bits
au plus dans le même environnement; export/vue offscreen équivalente identiques
hors bords anti-aliasés; cible cross-platform `DeltaE00 <= 2`. Le relief contrôlé
du mesh n’impose pas une égalité RGB avec une coupe non éclairée sous toutes
les normales. Ne pas comparer toute l’anatomie pixel par pixel comme seul verdict.
Les masques fond/anatomie/science/bords évitent qu’une bonne moyenne cache une
petite région scientifique erronée.

La validation humaine porte sur lisibilité anatomique, ambiguïté des couleurs,
profondeur/transparence, contours et écarts intentionnels. Consigner capture,
configuration, validateur/date et verdict. Présence de tests, compilation shader
et qualification matérielle sont trois preuves différentes.

Tests : `HBP.Rendering.Tests` (couleur, shaders, inventaire matériaux, ROI/sites,
textures, Edges/exports), `HBP.Module3D.PlayModeTests`, tests natifs de générateurs.
Les noms historiques `Phase*` des tests restent des points d’entrée actuels.

## Protocole performance

Mesurer CPU main/render/GPU, médiane/P95/P99, SetPass/batches, géométrie, culling,
allocations GC et ressources vivantes. Même machine, état, résolution et
warm-up; les FPS sous VSync ne suffisent pas. Le comparatif historique retenait
investigation au-delà de 5 % et refus au-delà de 10 % de régression P95 soutenue,
sauf acceptation explicite. Ces critères relatifs ne sont pas des limites produit.

Le cas isolé 30 000 sites × 1 vue mesure le renderer : les sites temporaires
n’ont pas les colliders du modèle réel. Le cas 9 colonnes × 3 vues, éventuellement
30 000 sites par colonne, est un stress de stabilité, sans promesse de fluidité.

## Comparaison historique finale du 7 août 2026

Windows 11, RTX 2070 SUPER, D3D11, Unity `6000.5.2f1`, qualité Fantastic, Linear;
120 frames de chauffe puis 900 mesurées, focus conservé, idle désactivé.
`Small` : 3 × 348×516 pixels; 30 000 sites : 1 × 348×516;
9×3 : 27 × 112×200. Runs locaux Built-in `20260807-191903` et URP
`20260807-211822`, sous `baseline-birp-phase5` / `urp-phase5`.

| Scénario | Médiane BIRP / URP (ms) | P95 BIRP / URP (ms) |
|---|---:|---:|
| Témoin caméras 3D coupées | 3,249 / 2,850 | 25,757 / 3,455 |
| Opaque, Edges off | 4,546 / 3,872 | 60,256 / 4,577 |
| Opaque, Edges on | 4,493 / 4,040 | 15,704 / 4,910 |
| Transparent, Edges off | 3,975 / 3,859 | 16,806 / 4,472 |
| Transparent, Edges on | 4,473 / 4,104 | 16,635 / 4,958 |
| Rotation | 4,521 / 4,223 | 17,374 / 5,183 |
| Activité temporelle | 16,581 / 19,512 | 20,053 / 20,785 |
| Hover atlas | 4,717 / 4,605 | 28,125 / 5,630 |
| Déplacement coupe | 13,308 / 14,355 | 16,260 / 16,584 |
| 30 000 sites, 1 vue | 38,075 / 17,573 | 58,872 / 19,501 |
| 9×3 vues | 10,129 / 10,011 | 23,104 / 11,068 |

Ces mesures justifiaient le maintien des sites simples et l’acceptation Windows.
Elles ne montrent pas un gain uniforme : médiane activité +17,7 %, coupe +7,9 %,
mais P95 respectifs +3,7 % et +2,0 %. Elles ne prédisent pas les temps actuels,
ni ceux d’un autre GPU/API ou de Quest.

La réutilisation de trois buffers natifs et la fusion des parcours de lissage
avaient réduit le P95 activité d’une candidate intermédiaire de 38,816 à
20,785 ms sans changer les noyaux. Un cache global du support serait incorrect
car il dépend des instants et seuils. Les vues statiques n’ajoutaient pas de GC
au témoin (10 513 octets/frame communs à l’application/éditeur/collecteur);
ce résultat ne signifie pas zéro GC total.

Après le stress 9×3 puis retour à Small, textures possédées 27→3 et mémoire
graphique 354,6→318,7 Mo (322,3 avant stress); la mémoire réservée Unity reste
un high-water mark. Le rapport historique indique acceptation visuelle Windows
et suites 79 tests rendering, 43 PlayMode, 13 natives réussies. Aucune de ces
campagnes n’a été réexécutée pour la refonte documentaire. Les validations
macOS Apple Silicon/Metal et Linux/Vulkan étaient encore reportées dans ce
rapport; leur état matériel actuel n’est pas établi ici. OpenGL Core Linux
était seulement un fallback à qualifier, pas une plateforme implicitement validée.
