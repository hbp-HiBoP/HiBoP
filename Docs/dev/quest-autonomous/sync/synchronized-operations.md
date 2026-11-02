# Opérations partagées et calculs

La [frontière de mutation](../../../../Assets/Scripts/HBP/Sync/Scene/SceneMutationBoundary.cs) possède validation, application, invalidation et checkpoints des familles ci-dessous. Les identifiants D1–D39 servent uniquement à retrouver les anciennes preuves ; les noms numériques du protocole sont définis dans [MutationContract](../../../../Assets/Scripts/HBP/Sync/MutationContract.cs) et les contrats `T09` à `T13` du même dossier. L'existence d'un handler ne prouve pas sa qualification matérielle : voir [validation](synchronization-validation.md).

## Matrice actuelle

« Dernière valeur » autorise le remplacement d'une valeur **non envoyée** sur la même clé ; la dernière valeur restante reste fiable. Structure, résultats et transactions sont fiables et ordonnés. « Sûr » signifie autorisé pendant une projection d'activité ; le rendu ciblé peut néanmoins travailler. Un batch mixte prend la contrainte scientifique la plus forte.

| Famille | Opération / état transmis | Effet et contrainte |
| --- | --- | --- |
| D1–D2 | `SetSelectedColumn`, `SetSelectedSite` | Sélection scientifique partagée ; nouveau site soumis à sa visibilité effective. Coupes automatiques dérivées localement. Sûr. |
| D3–D6 | `CreateCut`, `DeleteCut`, `SetCutOrder`, `SetCutDefinition`, Strong/Soft, AutomaticCut policy | Identités stables, trois axes automatiques sémantiques. Définition complète atomique, dernière valeur par coupe ; structure fiable. Sûr, géométrie/textures/colliders ciblés. |
| D7–D9 | Création/nom/suppression ROI, ROI active, sphères, sélection de sphère | Import = contenu résultant, sans chemin. Géométrie/rayon scientifique sensibles ; sélection de sphère sûre ; rayon animé local. Masque ROI calculé localement. |
| D10 | Commande de filtre puis `SetSiteFilterResult` | `IsFiltered` et identité du roster + bitset. Job canonique Desktop en ligne ; détails ci-dessous. |
| D11–D13 | Blacklist, highlight, couleur demandée, labels ordonnés, HideBlacklisted, ShowAllSites, gain | Blacklist/ShowAllSites affectent le masque scientifique ; highlight/couleur/labels/gain et politique de masquage visuel n'invalident pas le champ d'activité. |
| D14 | `MoveSites(Left|Right|Reset)` | Commande scientifique fiable, pas trajectoire ni tableau de positions substitué. Placement absolu conservé dans configurations/checkpoints. Parité numérique Windows/ARM à distinguer de l'acceptation visuelle. |
| D15 | Calcul/chargement de corrélations, reset/affichage | Matrices/provenance canoniques, jamais chemin Desktop ; calcul/chargement par job, affichage sûr. |
| D16 | `SetMeshDisplay` : ressource préparée, hémisphère, représentation | Barrière de ressource/topologie sensible. Inflation calculée localement par job coordonné ; aucune nouvelle livraison pour la dérivée. |
| D17–D18 | IRM, calibration min/max, implantation | Choix de ressource sensible, identité préparée et nettoyage des références dépendantes. Contraste IRM atomique sûr pour le champ d'activité, actualise les coupes. |
| D19–D21 | Couleurs cerveau/coupes, colormap, arêtes, alpha/transparence, activité ; masque triangles | Apparence sûre. `ApplyTriangleMask` sensible pendant calcul : IDs de topologie originale, liste éparse ou bitset atomique, pas gestes d'effacement. Undo éventuel = nouveau masque résultant. |
| D22–D26 | Influence ; label statique ; span iEEG/CCEP ; source CCEP ; ressource fMRI/MEG, seuils et plages cachées | Ressources/influence/source sont sensibles ; spans et paramètres d'une sortie déjà préparée peuvent conserver la dernière valeur sûre pendant calcul. Source CCEP explicite, distincte du simple site sélectionné. |
| D27 | `SetTimelineAnchor` : index/play/loop/step, temps monotone et intention | Sûr, lecture locale ; aucun message par tick. Règles d'horloge ci-dessous. |
| D28–D31 | Mars/JuBrain, IBC/DiFuMo, localizer, calibration atlas fMRI | Choix logiques de source/aire/temps/paramètres. Dépendances installées prêtes sur les deux appareils ; overlays dérivés localement. Aucun transfert atlas. |
| D32 | Demande/retrait/automatique d'activité | Calcul local sur chaque appareil ; coordination de génération et disponibilité, pas transfert du résultat d'activité. |
| D33 | `SetConfigurationTransaction` | Une transaction typée prévalidée sur ressources et identités mises en scène ; barrière la plus forte, application atomique, restauration en cas d'échec. |
| D34 | `SetSiteConfigurationBatch` | Un geste/import = affectations finales blacklist/highlight/couleur/labels ordonnés. Filtre et positions exclus. Champs désactivés et fusion ordonnée des labels préservés. |
| D35–D38 | Caméras/vues, poses Quest, hover, panneaux, outils/brush, exports et édition de projet | Locaux. Un changement de ressource source nécessite une nouvelle livraison ; un export ne devient pas une mutation live. |
| D39 | Livraison, close/replace, perte réseau | Voir [cycle de vie](synchronization-architecture.md#inventaire-visibilité-et-scène-patient). |

Le mode global d'une toolbar n'est pas l'état partagé ; les affectations résultantes le sont. Une sélection déjà retenue peut survivre à une visibilité changée via checkpoint/correction, sans autoriser une nouvelle sélection d'un site invisible. Les effets dérivés d'un événement distant ne réémettent pas une seconde opération.

Les masques correspondent aux deux topologies originales complète/simplifiée. La validation contrôle tailles et identité avant mutation ; un masque canonique peut viser une topologie préparée momentanément inactive à cause d'une sélection optimiste Quest. La géométrie sélectionnée est reconstruite avant application ; le driver conserve le dernier masque autoritaire par topologie et rebase les propositions restantes. Ne pas contourner une erreur en sautant le masque ou en avançant seulement son watermark.

## Timeline et horloge

[TimelineClockEstimator](../../../../Assets/Scripts/HBP/Sync/Scene/TimelineClockEstimator.cs) n'est consulté que pour l'intention `Play`. `Pause`, `Seek`, `Step`, `Loop` appliquent exactement l'index transmis, même si la lecture reste active. Sans estimation utilisable, Play démarre à l'index reçu, y compris au dernier index d'une timeline non bouclée.

Le transport sonde toutes les 250 ms pendant lecture. L'estimateur conserve au plus 32 observations sur deux secondes, exige trois observations et la plus récente âgée d'au plus une seconde, choisit le RTT minimal parmi les observations fraîches et refuse une incertitude supérieure à un demi-échantillon. Une lecture n'est corrigée qu'au-delà d'un échantillon plus cette incertitude. Les horloges sont monotones 64 bits avec fréquence déclarée, jamais l'heure civile ni un `float` Unity durable. Play/Pause/Step/Loop constituent des frontières qui arrêtent la fusion des seeks.

## Jobs, invalidation et annulation

Chaque calcul est identifié par incarnation, type, ID et génération monotone. Seule la génération courante publie/déverrouille. Annuler A, terminer B, puis recevoir A tardivement doit laisser B intact. La préparation retient les buffers/handles réellement utilisés jusqu'au retour natif ; annuler une attente ne libère pas un buffer encore emprunté.

| Job | Exécution et résultat |
| --- | --- |
| Filtre | Desktop seul en ligne, masque complet lié au roster, Quest valide/applique atomiquement puis Ready exact. |
| Corrélation | Desktop calcule ou charge, résultat borné/versionné avec provenance, Quest publie toutes les matrices ensemble puis Ready. |
| Projection d'activité | Les deux calculent sur leurs entrées communes ; les commandes sensibles attendent les deux états terminaux, les commandes sûres et le réseau continuent. |
| Inflation | Les deux préparent le cache exact, puis transitions locales et commit canonique coordonné. |

Filtres/corrélations s'excluent mutuellement. Une perte confirmée après 500 ms annule les jobs en ligne sans les redémarrer automatiquement hors ligne. Une demande locale explicite Quest peut ensuite utiliser les capacités livrées. Une erreur Desktop abandonne le partiel ; une erreur d'application Quest ne vaut pas synchronisation réussie. Ressource/roster manquant impose un nouvel envoi. Un résultat périmé est abandonné sans être présenté comme échec de la nouvelle génération.

Projection demandée, politique automatique, état Absent/Stale/Computing/Ready/Failed et génération sont distincts. En manuel, invalidation marque Stale et attend une demande ; en automatique elle programme la génération la plus récente. Retirer l'activité annule et nettoie sa présentation. Sont sensibles : filtre/blacklist, ROI active/géométrie, positions scientifiques, influence, source CCEP, ressource/topologie, triangles et réglages natifs. Sélections, coupes, couleurs/labels/highlight, timeline et paramètres de sortie prouvés sûrs restent applicables. Les batches mixtes sont sensibles.

## Contrats de filtre et corrélation

[SiteFilterJob](../../../../Assets/Scripts/HBP/Sync/Scene/SiteFilterJob.cs) garde les sémantiques distinctes : fenêtre Site Filters = sites non masqués uniquement ; reset toolbar = tous inclus, même masqués ; `FilterChannels` = remplacement sur tous les sites par couples patient/nom. Le vieux parseur Basic/Advanced désactivé n'est pas le contrat de l'interface active.

Hors ligne, reset et sélection de canaux sont locaux. Les conditions autorisées sont nom, patient, attributs, position brute, tags résolus et présents, activité si statistiques disponibles, localisation ROI, avec groupes All/Any valides. Masques IRM, atlas, géométrie mesh/coupe et autres conditions non prises en charge sont refusés avant modification de `IsFiltered`.

[CorrelationResultResource](../../../../Assets/Scripts/HBP/Sync/Scene/CorrelationResultResource.cs) porte le roster complet et les matrices corrélation/moyenne, plus provenance Computed/Imported/Unknown, patient/colonne/dataset/protocole/bloc/data-info, normalisation, seuil et Bonferroni. Limite : 8 Mio. Les valeurs et toutes les dimensions sont validées avant remplacement du premier tableau. Le checkpoint garde indépendamment résultat (ou absence explicite) et `DisplayCorrelations` ; recharger un résultat live active l'affichage, restaurer un checkpoint rétablit son flag capturé.

## Inflation coordonnée

[SurfaceInflationCoordinator](../../../../Assets/Scripts/HBP/Sync/Scene/SurfaceInflationCoordinator.cs) capture ressource, hémisphère, options exactes, version d'algorithme et références/GeometryVersion des surfaces anatomiques. Quest draine d'abord le watermark canonique précédent. L'ancien affichage reste visible pendant la préparation.

L'algorithme courant **2** travaille dans les coordonnées anatomiques déjà transformées en mémoire. Aucun GIFTI/transform supplémentaire n'est envoyé pour inflation. Seul `CurrentSurfaceCoordinates` est accepté pour un résultat préparé ; les anciens marqueurs `NativeGifti`/`NativeGiftiThenTransformed` sont refusés sans recalcul silencieux ni changement de leurs numéros historiques.

Quand les deux caches sont prêts, une demande animée effectue la transition locale existante de 0,6 s sur chaque pair. Les deux fins précèdent le `SetMeshDisplay` canonique ; Quest confirme après application du watermark et vérification de la représentation. Sans animation, cette barrière est sautée. Les attentes de pair/barrière sont bornées à dix minutes. LoadingManager couvre uniquement préparation/attente du pair et se ferme **avant** animation/commit. Une annulation prépublication conserve l'ancien affichage ; après commit elle ne l'annule pas. Masques, UV/couleurs et poses locales sont conservés. Le [contrat scientifique](../../surface-inflation.md) décrit topologie, cache et précision.
