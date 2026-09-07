# Validation et mesures

## Matrice de preuves

| Niveau | Vérification nécessaire | Ne prouve pas |
| --- | --- | --- |
| Analyse statique | Graphe, APIs, provenance, import settings | Compilation/rendu réel |
| Tests .NET/EditMode | Codecs, règles communes, conversions et cas invalides | APIs mobiles effectives |
| Build Player | Compilation cible et contenu inspecté | Runtime scientifique |
| Quest physique | Calcul, rendu, interaction, réseau et mémoire | Parité générale hors fixture |
| Mac/Linux physique | Parcours Desktop complet avec Quest | Support de tous les matériels/OS |

Chaque rapport mentionne commit HiBoP, commit/hash natif, Unity/packages,
OS/appareil, fixture/hash, paramètres, exécutable/APK et limites de la mesure.

## Tests par tranche

A — anatomie :
- mêmes sommets/normales/indices après transport, hashes exacts ;
- orientation asymétrique et dimensions MNI correctes ;
- véritable transfert depuis Player Windows, aucune substitution par un asset APK ;
- gestes locaux sans modification caméra Desktop ;
- coupure une minute après réception, manipulation conservée ;
- réception interrompue, corruption et accusé de livraison perdu : pas de contenu
  partiel ni double instance.

B — sites :
- IDs/ordre et repère conservés ; distance relative et taille des sites cohérentes ;
- grossissement global, affichage/masquage surface sans perdre les sites ;
- points connus de part et d'autre du cerveau pour détecter réflexion d'axe ;
- visibilité interne validée via masquage, sans exiger transparence.

C — densité :
- même entrée préparée sur Desktop et Quest, moteur épinglé ;
- comparaison grille, couverture, densité maximale, UV, sentinelles et masques ;
- cas aucun site, un site, tous masqués et positions aux limites ;
- calcul encore possible après perte du réseau, à l'aide des entrées reçues ;
- charge/libération répétée des handles sans crash ni croissance non expliquée.

D — iEEG :
- fixture alignée Alpha=0, instant/provenance et unités conservés ;
- amplitudes préparées identiques, normalisation Desktop conservée ;
- UV, opacités, couleurs et tailles scientifiques comparées ;
- valeurs négatives, nulles, site absent/masqué ;
- si support d'un instant interpolé ajouté, test distinct du comportement
  surface sample-and-hold et sites interpolés, sans arrondi implicite.

La comparaison doit porter sur les buffers scientifiques avant les shaders.
Des captures rendues à caméra contrôlée complètent la comparaison ; le passthrough
et la pose réelle empêchent de demander des images Desktop/Quest identiques.

## Protection de la source unique

- Test du graphe des assemblies : contrats et pipeline commun sans référence
  aux assemblages Desktop/UI/Quest.
- Vérification ciblée des types du seam extrait : aucune entrée utilisateur,
  Camera.main, singleton de scène ou transport.
- Test d'intégration Desktop montrant l'appel au chemin commun, puis avant/après
  avec les mêmes fixtures. Ne pas se limiter à tester le service isolé.
- Test Quest utilisant ce même service. Une modification contrôlée d'un paramètre
  commun dans la fixture doit produire le même effet dans les deux cibles.
- Ne pas présenter les comparaisons anciennes hbp_export/hbp_core comme oracle
  Windows/Android : elles comprennent des écarts intentionnels historiques.

## Seuils proposés et décisions

| Mesure | Proposition / statut |
| --- | --- |
| Coupure démontrée | 60 secondes, validé par le propriétaire. |
| Intégrité transport | Hashes/IDs/comptages exacts ; aucune quantification. |
| Masques, sentinelles, couverture catégorielle | Égalité exacte, séparée des comparaisons de floats. |
| Calcul analytique des fixtures existantes | Conserver les tolérances des tests source ; ActivityGeneratorFunctionalTests utilise notamment 0,0005 sur certains UV. |
| Parité Windows/Android sur MNI | Relever max/RMS et localisation des écarts ; fixer une tolérance par grandeur avant le verdict scientifique. 0,0005 n'est pas automatiquement une tolérance générale validée. |
| Fluidité | Cible provisoire 72 Hz, cohérente avec le probe P05 ; budget de frame 13,89 ms. Mesurer frames manquées et durées CPU/GPU séparément. À valider sur prototype manipulé. |
| Durée d'observation | Proposition : 10 minutes d'exploration après warmup, incluant la coupure ; endurance 30 minutes seulement si nécessaire à la qualification suivante. |
| Temps initial/calcul/transfert | Mesurer d'abord ; pas de seuil arbitraire bloquant l'anatomie. L'utilisateur doit pouvoir constater progression et fin. |
| Mémoire | Mesurer pic et libération ; refuser OOM/crash, pas un nombre importé des anciennes ADR. |

Les seuils provisoires sont des cibles de travail à confirmer, pas des gates
anciens réappliqués. Une divergence scientifique inexpliquée bloque C/D.
Une durée de calcul longue mais correcte alimente le compromis produit et
l'optimisation ; elle ne justifie pas un algorithme différent sans décision.

## Instrumentation minimale

Temps de capture, taille transférée, débit utile réel, hash/décodage,
reconstruction native, calcul, copies managed/native, préparation et upload
GPU, première frame visible. Distinguer soumission GPU, fin de frame et
command-to-photon : ne pas substituer une mesure à l'autre.

Relever mémoire Unity, PSS/RSS Android, pic de staging et calcul, allocations
pendant manipulation, fréquence/frames manquées, thermique et évolution batterie
sur fenêtre consignée. Si la mesure GPU n'est pas disponible, la déclarer absente.

Rapporter au moins une exécution froide et une chaude sur chaque fixture.
Les tailles et cardinalités accompagnent toujours les résultats.

## Builds et CI

Construire Desktop et Quest depuis le même commit avec manifest/lock identiques.
Utiliser des workspaces et Library/caches séparés par cible/version, pas des
projets produits divergents. Préserver les jobs Desktop existants ; ajouter le
profil Quest puis Mac au jalon correspondant.

Archiver Build Reports : scènes, assemblies, plugins natifs, shaders/variants,
assets volumineux et taille Player. Vérifier loader XR absent/inactif Desktop,
librairies Desktop absentes APK, dépendances nécessaires Android présentes.
Inclure un smoke test Desktop après changement de configuration d'entrée.

Aucune exécution n'est lancée durant le cadrage. Pendant l'implémentation,
respecter MCP si Unity est ouvert ; sinon CLI Unity hors sandbox. Les tests
Unity async attendent directement le travail, sans Assert.ThrowsAsync bloquant,
Wait/Result ou boucle d'attente principale. Formater les C# modifiés avec
Tools/format-code.cmd avant handoff.
