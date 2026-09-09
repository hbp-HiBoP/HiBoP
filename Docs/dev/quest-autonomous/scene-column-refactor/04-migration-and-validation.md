# Migration, dépendances et validation

## Point de départ et limites de l'estimation

La baseline sera le code et les preuves réellement disponibles après QUEST-023.
Le travail en cours sur 018 n'est pas figé par cette spécification. L'audit doit
réutiliser les abstractions qui auront été livrées, particulièrement les pipelines
scientifiques et leurs règles de fin réelle des calculs.

Huit lots sont proposés pour le périmètre du prototype. L'hypothèse de coût est
une reprise ciblée, pas une réécriture de tout HiBoP. SCENE-001 doit relever les
dépendances qui invalideraient cette hypothèse : couplage à toutes les modalités,
sérialisation historique, duplication d'état ou contraintes de ressources. Si le
reste est trop large, proposer des sous-tâches et leur impact avant d'élargir le
chantier. Aucun engagement de durée ou de nombre maximal de tâches n'est donné.

## Séquence

| Tâche | Résultat | Dépendance |
| --- | --- | --- |
| [SCENE-001](tasks/SCENE-001.md) | Audit après 023, baseline et responsabilités attribuées | QUEST-023 et preuves utiles de 018–022 |
| [SCENE-002](tasks/SCENE-002.md) | Socle Scène/Colonne concret, accès commun et cycle de vie | SCENE-001 |
| [SCENE-003](tasks/SCENE-003.md) | Desktop utilise le socle pour le périmètre migré | SCENE-002 |
| [SCENE-004](tasks/SCENE-004.md) | Capture et restauration cohérentes du modèle commun | SCENE-003 |
| [SCENE-005](tasks/SCENE-005.md) | Réception et présentation Quest utilisent le modèle restauré | SCENE-004 |
| [SCENE-006](tasks/SCENE-006.md) | Opérations et invalidation communes démontrées et complétées | SCENE-005 |
| [SCENE-007](tasks/SCENE-007.md) | Chemins remplacés retirés, autres modalités préservées | SCENE-006 |
| [SCENE-008](tasks/SCENE-008.md) | Preuves d'intégration et dossier de reprise pour 024 | SCENE-007 |

SCENE-002 établit dès le départ les opérations, ressources et invalidations
nécessaires au premier chemin concret ; SCENE-006 n'autorise pas deux
orchestrations provisoires pendant 003–005. Elle couvre les variantes et cas
limites après intégration des deux consommateurs. Les tâches restent autonomes
à vérifier, même si leur résultat n'est pas encore tout le produit refactorisé.

## Stratégie de migration

1. Capturer des références fonctionnelles et numériques reproductibles avant
   modification. Identifier les tests existants réellement réutilisables.
2. Extraire à partir du code utilisé. Introduire une première scène et colonne
   fonctionnelles ; éviter les interfaces sans consommateur ou modèle vide.
3. Raccorder Desktop avant Quest pour empêcher la création d'un « socle commun »
   en réalité réservé au casque. Préserver les projets, scènes, prefabs et l'UI.
4. Restaurer ce même modèle depuis les données transportées et adapter le renderer
   Quest. Réutiliser les transports, codecs, conversions et calculateurs vérifiés.
5. Compléter les opérations communes, puis retirer les façades transitoires et
   branches remplacées, avec une comparaison avant/après.
6. Qualifier le parcours technique refactorisé et préparer 024. Ne pas annoncer
   la qualification complète de 024 ni celle des autres OS dans SCENE-008.

Une façade temporaire a un propriétaire, une liste de callers et une condition
de suppression. Elle délègue au modèle commun sans conserver une copie modifiable
de l'état scientifique. Les autres modalités Desktop encore historiques sont
distinguées des duplications à retirer.

## Matrice de preuves

| Domaine | Référence et scénario | Tâches principales |
| --- | --- | --- |
| Accès et dépendances | Même scène/colonne utilisable sans UI ; imports métier et graphe d'appels inspectés | 001, 002, 003, 006, 007 |
| Données et calcul | Anatomie, sites, densité et iEEG comparés à la baseline, valeurs masquées et limites | 001, 003, 004, 006, 008 |
| Opérations | Même modification de rayon de colonne et même opération existante de scène, APIs communes, invalidation et résultat sans appeler directement le calculateur | 001, 002, 003, 005, 006, 008 |
| Temporalité | Desktop conserve plusieurs instants ; Quest conserve les sémantiques de l'instant reçu, dont surface/sites | 003, 004, 006 |
| Partage de ressources | Deux colonnes, asset partagé, paramètres/masques distincts, suppression isolée | 002, 004, 006 |
| Asynchronisme | Mutation pendant calcul, résultat ancien, annulation, fermeture, erreur et fin native réelle | 002, 004, 005, 006 |
| Réseau et publication | Remplacement valide/invalide, perte d'acquittement, nouvelle tentative, déconnexion | 004, 005, 008 |
| Présentation | Desktop inchangé ; pose Quest indépendante, vue retirée/recréée et modèle conservé | 003, 005, 008 |
| Compatibilité | Projets/prefabs existants, formats supportés et modalités Desktop hors migration | 001, 003, 004, 007, 008 |

Les tolérances numériques restent celles justifiées par la baseline ; une
différence inexpliquée est un écart, pas un motif d'élargir la tolérance. Les hashes
binaires de payload peuvent changer avec le format ; comparer alors la sémantique
et identifier précisément la version de chaque fixture.

Pour les changements de volume, surface, implantation, masque, paramètres et
instant, inventorier les déclencheurs existants et tester les chemins affectés.
Un déclencheur non disponible sur Quest faute de données est explicitement limité,
pas remplacé par une implémentation différente.

## Niveaux de vérification

- Tests de logique/contrats : IDs, relations, données, validation et invalidation.
- Tests Unity intégrés : adaptation Desktop, sérialisation, scènes/prefabs,
  abonnements, rendu utile et contraintes du thread Unity.
- Players Windows/Quest : mêmes sources, provenance native, scénarios pertinents
  de calcul, publication, ressources et fonctionnement hors ligne.
- Retour du propriétaire : seulement les aspects visuels ou interactifs utiles,
  après fourniture d'une recette exacte et vérifications techniques par l'agent.

Chaque fiche exige des tests proportionnés à son changement. Ne pas relancer
systématiquement toute la qualification sur chaque lot ; compléter lors d'une
nouvelle modification, erreur ou incertitude. Les tests sans UI n'impliquent pas
nécessairement l'absence du PlayerLoop Unity.

## Sortie vers QUEST-024

SCENE-008 fournit la correspondance entre baseline et code refactorisé, les
binaires/fixtures identifiés, les résultats des invariants I01–I10, les limites
et le scénario concret de reprise de 024. Une réussite de calcul seule n'est
pas suffisante : la preuve du modèle et des opérations communs est obligatoire.

024 reste responsable de sa campagne globale et de son acceptation. Les tests
antérieurs dont les chemins ont changé ne sont pas annoncés comme valides pour
les nouveaux binaires sans vérification. Les preuves non affectées peuvent être
référencées avec justification, sans dupliquer leurs artefacts.

Les fiches historiques ne sont pas modifiées par cette livraison. La nouvelle
dépendance est portée ici et dans SCENE-008 ; la tâche 024 devra être lancée avec
ce contexte tant que son index historique n'aura pas été raccordé séparément.
