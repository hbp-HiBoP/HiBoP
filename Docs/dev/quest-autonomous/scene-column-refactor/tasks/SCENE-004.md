# SCENE-004 — Exporter et restaurer la scène scientifique

Type : contrat de données et reconstruction fonctionnelle.
Dépendance : [SCENE-003](SCENE-003.md). Suivi : [registre](../TASK-STATUS.md#scene-004).

## Reprise

Lire le [contrat local](../TASK-WORKFLOW.md), [le payload](../03-payload-and-restoration.md)
et les rapports 001–003. `ToPayload`/`FromPayload` désignent le contrat approuvé,
pas l'obligation de placer l'encodage et les attentes dans `Base3DScene`.

## Points d'entrée à inspecter

Socle commun et préparation Desktop migrée ; formats et captures effectifs après
023 ; codecs/versionnement, reconstructeurs natifs et fixtures de transfert.
Réutiliser les protections existantes de dimensions, hashes, coordonnées et fichiers.

## À implémenter

- Capturer une version cohérente de la scène et des colonnes sélectionnées avec
  identités, références d'assets, paramètres, état, disponibilités et provenance.
- Adapter le format au minimum nécessaire ; documenter versions et lecteurs
  supportés. Une version ancienne admise alimente le même modèle commun.
- Restaurer un modèle temporaire via les mêmes constructeurs/validations métier
  que Desktop ; séparer vérifications structurelles du codec et invariants communs.
- Préserver partages immuables, indépendance des données mutables par colonne,
  conventions, masques et sémantiques de l'instant iEEG/plages préparées.
- Offrir une restauration transactionnelle avec nettoyage sur échec/annulation,
  sans publication prématurée ni besoin de chemins ou d'objets Desktop.
- Raccorder l'export de production Desktop à cette capture en conservant les
  protections d'offre cohérente ; maintenir l'intégration réseau existante jusqu'à 005.
  Si une transition de format est nécessaire, la borner et la tester explicitement.

## Hors périmètre

Pas de projet portable complet, timeline nouvelle, UI multicolonne, nouvelle
authentification ou refonte du transport. Pas de rupture de compatibilité implicite.
Pas de publication d'objets Unity/handles natifs via sérialisation du graphe historique.

## Vérifications par l'agent

- Round-trip réel du contenu préparé Desktop : valeurs, IDs, relations, références,
  repères et capacités. Deux colonnes de test partagent un asset sans aliasser leurs masques.
- Même opération scientifique sur scène source puis restaurée sans UI et sans
  lecture métier du payload : invalidation, calcul et résultat équivalents.
- Capture pendant mutation : aucune combinaison de révisions ; limites de copies
  et de mémoire mesurées sur la fixture complète, sans troncature scientifique.
- Versions supportées, inconnues, données absentes, IDs dupliqués, liens pendants,
  hashes/longueurs invalides et échec partiel de restauration : refus et nettoyage.
- Aucune donnée restaurée ne dépend d'un chemin Desktop ou d'une adresse native
  exportée. Fermeture/annulation en vol respecte la fin réelle des utilisateurs.
- Revue indépendante bornée de cohérence, compatibilité et propriété.

## Validation manuelle

NON_REQUIS pour codec et restauration. L'agent vérifie les valeurs et ressources ;
le parcours visuel Quest sera contrôlé en 005.

## Rapport et critère de fin

Produire `reports/SCENE-004.md`, `evidence/SCENE-004/manifest.json` et sa ligne
locale. Expliquer exemple source → payload → même modèle restauré, versionnement,
sélection exportée, copies et publication/nettoyage.

Terminé lorsque le contrat de scène restaurable est exercé depuis la capture
Desktop réelle et permet la même opération scientifique, avec formats et échecs
vérifiés. Un round-trip de DTO seul ne suffit pas. Prochaine tâche : [SCENE-005](SCENE-005.md).
