# Roadmap des jalons

Les jalons décrivent des démonstrations produit. Pour implémenter une unité
reviewable, utiliser les [29 tâches QUEST](tasks/README.md) et leur
[contrat d'exécution](TASK-WORKFLOW.md). Chaque jalon possède sa
[fiche de démonstration](milestones/README.md). Le registre de statut est
[TASK-STATUS.md](TASK-STATUS.md) ; cette roadmap n'est pas une liste de tâches à
exécuter en bloc.

L'ordre est fondé sur les réponses produit : Windows jusqu'à l'iEEG locale,
puis Mac, évaluation Linux, puis fonctionnalités supplémentaires.
Aucun jalon ne demande un portage global préalable.

## J0 — baseline et cadrage

- Objectif : base de départ et périmètre approuvés, ancien XR préservé.
- Décisions préalables : D01–D21 ; une demande d'implémenter une tâche autorise son périmètre.
- Tâches : vérifier la branche feature/xr-autonomous créée par le propriétaire,
  conserver les documents et préparer la fixture ; sélectionner les fichiers
  XR utiles depuis Git avec leur provenance lors des tâches concernées.
- Dépendances : réponses produit et audit.
- Risque : emporter la topologie/les politiques de l'ancienne feature.
- Acceptation binaire : branche dérivée de la baseline retenue, aucune reprise
  globale XR, état local préservé, périmètre approuvé.
- Mesures : SHA de baseline, inventaire des fichiers retenus.
- Artefacts : présent cadrage, journal et matrice de réutilisation.
- Révision : nouvelle divergence de baseline ou changement de périmètre.
- Charge : faible ; aucune migration de code pendant le cadrage.

## J1 — un projet, deux Players

- Objectif : HiBoP Windows intact et Quest passthrough construit depuis le même projet.
- Décisions préalables : composition des profils et packages, politique d'entrée.
- Tâches : manifeste unique, profils DesktopWindows/Quest, scène/prefabs Quest,
  OpenXR Android, isolation plugins et vérification du contenu.
- Dépendances : J0.
- Risques : changement d'Input System global, packages incompatibles, assets
  Resources et plugins embarqués malgré la scène minimale.
- Acceptation binaire : deux builds du même commit ; Desktop ouvre la fixture
  et garde ses contrôles ; Quest affiche passthrough et contrôleurs ; aucun
  loader XR Desktop actif et aucun plugin Desktop dans APK.
- Mesures : résolution packages, taille des Players, mémoire de démarrage.
- Artefacts : profils, scène/prefabs, rapports de build, smoke tests.
- Révision : incompatibilité démontrée du manifeste ou régression Desktop.
  Corriger la frontière avant de dupliquer le projet.
- Charge : moyenne ; principal inconnu, réglages d'entrée/cible.

## J2 — transfert réel et anatomie manipulable

- Objectif : depuis la visualisation Desktop ouverte, recevoir le cerveau MNI
  complet, le manipuler puis déconnecter sans perdre la session.
- Décisions préalables : transport embarqué préféré ; alternative seulement
  justifiée par obstacle mesuré ; contrat géométrie/identités.
- Tâches : essai transport embarqué borné ; extraction réelle ; appairage et
  bouton d'envoi ; snapshot/chunks/hash ; publication atomique ; contrôle spatial
  du prefab ; état de connexion ; nouvelle tentative.
- Dépendances : J1 ; aucun calcul natif Android requis pour afficher le mesh reçu.
- Risques : APIs réseau/TLS du Player, coût capture/hash/upload, conversions.
- Acceptation binaire : mêmes buffers et orientation, déplacement/rotation/échelle,
  aucune modification de caméra Desktop, coupure de 60 s sans perte de manipulation,
  transfert corrompu refusé, même livraison répétée sans double instance.
- Mesures : octets, temps capture/transfert/première frame, mémoire et frame.
- Artefacts : preuve Windows–Quest enregistrée, fixture/hash, rapport du choix
  transport, contrats/tests de livraison.
- Révision : si l'embarqué échoue ou nécessite une pile fragile, documenter le
  coût/obstacle et évaluer P06 auxiliaire ; ne pas affirmer a priori qu'il est requis.
- Charge : élevée/incertaine jusqu'à la preuve du transport embarqué.

## J3 — contacts associés

- Objectif : contacts synthétiques correctement placés, visibles et manipulés
  avec le cerveau.
- Décisions préalables : bouton afficher/masquer la surface accepté.
- Tâches : entrée de sites ordonnée, reconstruction/renderer, taille partagée,
  bouton XR, maintien de la prise sur le groupe lorsque la surface est masquée.
- Dépendances : J2.
- Risques : double conversion d'axes/unités et contacts occultés.
- Acceptation binaire : sites à positions attendues ; affichage/masquage fonctionne ;
  translation/rotation/échelle gardent leurs relations au cerveau.
- Mesures : cardinalité, taille transférée, frame et allocations.
- Artefacts : fixture sites déterministe, tests de repère, démonstration.
- Révision : écart de repère ou nécessité avérée de revoir le renderer.
- Charge : moyenne ; rendu P10 candidat, picking avancé différé.

## J4 — moteur Android et densité commune

- Objectif : projection de densité calculée sur Quest avec le pipeline scientifique
  également appelé par Desktop.
- Décisions préalables : source native épinglée, entrées complètes et tolérances
  de validation par grandeur.
- Tâches : build Android reproductible de hbp_core complet ; smoke runtime
  IL2CPP ; transférer MNI.nii et paramètres ; reconstruire volume/surface/sites ;
  extraire le pipeline densité et sa propriété de ressources ; basculer Desktop
  sur ce pipeline avant de brancher Quest.
- Dépendances : J3. L'extraction Desktop peut être préparée après J2, mais la
  preuve native précède l'acceptation de la densité.
- Risques : ABI, mémoire de grille, durée de calcul, masque et ordre des appels,
  libération pendant un calcul non terminé.
- Acceptation binaire : mêmes entrées et source native, calcul Quest hors ligne,
  parité expliquée/validée, aucune branche densité scientifique dupliquée,
  régression Desktop absente, ressources libérées correctement.
- Mesures : reconstruction, grille, calcul, copie/UV/upload, mémoire et thermique.
- Artefacts : .so/manifest, pipeline commun, résultats bruts de parité et tests.
- Révision : divergence scientifique ou dépassement mémoire. Revoir scheduling
  et capacités ; ne pas réduire silencieusement la résolution scientifique.
- Charge : élevée ; principal risque du produit autonome.

## J5 — iEEG locale pour un instant

- Objectif : montrer l'activité projetée d'un instant préparé Desktop, calculée Quest.
- Décisions préalables : pas de timeline interactive ; premier cas aligné sur un
  échantillon exact ; conserver normalisation et sampling.
- Tâches : étendre les entrées et le pipeline commun à IEEGGenerator ; récupérer
  unités/plages/masques ; partager le calcul d'apparence des sites ; comparer
  aux sorties Desktop ; retirer le chemin redondant pour cette tranche.
- Dépendances : J4.
- Risques : recalcul de plages sur un instant, interpolation incorrecte,
  confusion entre données préparées et UV déjà projetés.
- Acceptation binaire : un instant identifiable, valeurs et paramètres identiques,
  projection locale et apparence cohérentes, pas d'import EEG Quest, pas de
  dépendance réseau pendant calcul/manipulation.
- Mesures : mêmes étapes que J4, erreurs max/RMS par grandeur, timing choisi.
- Artefacts : fixture iEEG synthétique, contrat versionné, rapport de parité,
  démonstration Windows complète.
- Révision : incohérence de normalisation/temps ou dépendance cachée aux loaders Desktop.
- Charge : moyenne à élevée, dépend de l'extraction réussie à J4.

## J6 — qualification Mac Apple Silicon

- Objectif : refaire A–D depuis un Player Desktop Mac vers Quest 3.
- Décisions préalables : version macOS réellement disponible, artefacts ARM64.
- Tâches : profil DesktopMac et packaging ; lancement transport ; ouverture
  fixture ; transfert/calcul/rendu ; comparaison de provenance et résultats.
- Dépendances : J5 accepté.
- Risques : APIs réseau/certificats, permissions OS, bundle/chemins de Data,
  versions de binaires natives. CMake min12 et runner15 ne prouvent pas le runtime.
- Acceptation binaire : parcours sur Mac physique, résultats scientifiques
  conformes et Quest manipulable hors ligne ; aucune variante métier Mac.
- Mesures : transfert, packaging, calcul Desktop de référence et comparaison Quest.
- Artefacts : Player Mac, rapport matériel/OS, preuves du même pipeline.
- Révision : corriger uniquement l'adaptateur/packaging concerné ; pas de nouveau
  projet Unity ni calcul spécifique à l'OS.
- Charge : incertaine avant accès au Mac, faible à moyenne si la frontière tient.

## J7 — décision et qualification Linux éventuelle

- Objectif : décider de tester Linux sur une machine/distribution identifiées.
- Décisions préalables : disponibilité d'un environnement et intérêt du test.
- Tâches : réutiliser profil/build existant, vérifier plugins, chemins, réseau
  et refaire le parcours si le test est retenu.
- Dépendances : J6.
- Risques : compatibilité distribution et transport non testé nativement.
- Acceptation binaire : soit report explicitement décidé, soit parcours A–D
  validé sur la distribution consignée.
- Mesures : mêmes familles que J6.
- Artefacts : décision de périmètre Linux et/ou rapport.
- Révision : pas de support général annoncé depuis une compilation.
- Charge : à estimer avec la machine cible.

## Après qualification du socle

Les responsabilités d'extension, dépendances, décisions et preuves futures sont
détaillées dans le [catalogue des extensions](10-extension-points-and-future-features.md).

Prioriser avec le propriétaire : mains, transparence avancée, autres colonnes,
timeline, coupes locales, ROI, commandes scientifiques synchronisées et politique
de source de vérité, export portable et persistance. Réévaluer chaque tranche
avec la même règle de comportement commun. Ne pas détailler une fusion concurrente
avant l'entrée des modifications bidirectionnelles.

## Unités d'implémentation

| Jalon | Fiche | Tâches exécutables |
| --- | --- | --- |
| J0 | [Baseline/fixture](milestones/J0.md) | QUEST-001 |
| J1 | [Deux Players](milestones/J1.md) | QUEST-002 à QUEST-004 |
| J2 | [Transfert/anatomie](milestones/J2.md) | QUEST-005 à QUEST-012 |
| J3 | [Contacts](milestones/J3.md) | QUEST-013 à QUEST-014 |
| J4 | [Densité locale](milestones/J4.md) | QUEST-015 à QUEST-019 |
| J5 | [iEEG/qualification Windows](milestones/J5.md) | QUEST-020 à QUEST-024 |
| J6 | [Mac](milestones/J6.md) | QUEST-025 à QUEST-026 |
| J7 | [Linux éventuel](milestones/J7.md) | QUEST-027 à QUEST-029 |

Les anciens IDs B01–B19 sont retirés au profit des fiches QUEST ; ils ne doivent
plus être utilisés pour commander une implémentation. Leur périmètre est couvert
par les tâches ci-dessus, réparties par responsabilité et preuve.

Les estimations calendaires restent à préciser après QUEST-009 (transport) et
QUEST-016 (runtime natif). Les charges relatives servent à organiser le travail,
pas à promettre un délai. Estimer ensuite chaque tâche avec les hypothèses de
disponibilité casque/Mac, réutilisation effective et seuils convenus.
