# SCENE-002 — Extraire le socle Scène/Colonne et son cycle de vie

Type : extraction fonctionnelle ciblée.
Dépendance : [SCENE-001](SCENE-001.md). Suivi : [registre](../TASK-STATUS.md#scene-002).

## Reprise

Lire le [contrat local](../TASK-WORKFLOW.md), [l'architecture](../02-target-architecture.md)
et le rapport de SCENE-001. Les noms et responsabilités précis viennent de cet
audit et du code courant ; ne pas créer un modèle parallèle de convenance.

## Points d'entrée à inspecter

Types et propriétaires identifiés en SCENE-001 : scène/colonnes, pipelines de
018–023, wrappers natifs, données préparées et règles de masques/normalisation.
Inspecter leur accès aux singletons, vues, préférences et ressources partagées.

## À implémenter

- Extraire un socle concret représentant scène, colonnes, identités, relations,
  données disponibles et paramètres. Conserver/réutiliser les types existants
  quand possible ; justifier les façades et déplacements nécessaires.
- Fournir un accès scientifique commun aux volumes, surfaces, sites, amplitudes,
  masques et résultats. Le socle ne consulte ni payload, ni vue Quest, ni UI Desktop.
- Brancher les calculateurs existants du périmètre migré à ce socle ; aucune
  seconde formule. Démontrer au moins la mutation d'influence et son recalcul
  complet par une colonne commune.
- Établir les propriétaires, prêts/copies, partage immuable et état mutable par
  colonne. Fermer/remplacer sans double destruction ou libération avant fin réelle.
- Définir les invalidations, révisions et publications nécessaires au chemin
  concret ; empêcher un résultat ancien ou une fermeture de ressusciter l'état.
- Permettre l'utilisation sans présentation et des abonnements de présentation
  bornés. Ne pas installer un service commun vide en attendant les raccordements.

## Hors périmètre

Pas de migration complète de l'UI Desktop, réception Quest, nouveau format ou
nouvelle fonctionnalité. Pas de framework générique ni extraction globale des
autres modalités. Les raccordements de production suivent en 003 et 005.

## Vérifications par l'agent

- Scène et colonnes utilisées sans caméra, `View3D`, toolbar, renderer ou rig XR ;
  les tests conservent le PlayerLoop si les opérations le nécessitent.
- Même opération publique de colonne → mutation → invalidation → calcul → résultat,
  sans raccourci du test vers le générateur. Comparer à la référence densité.
- Deux colonnes partagent une ressource immuable, gardent leurs paramètres/masques
  distincts ; en libérer une conserve l'autre, puis aucune ressource détenue ne reste.
- Calcul en vol puis mutation, échec, annulation ou fermeture : aucune publication
  obsolète ni destruction avant terminaison native réelle ; attentes non bloquantes.
- Vérifier que l'extraction laisse le chemin de production Desktop utilisable
  en attendant 003 ; signaler toute façade temporaire et son propriétaire.
- Revue indépendante bornée de propriété/concurrence, avec constats traités.

## Validation manuelle

NON_REQUIS : la preuve porte sur des opérations et ressources automatisables.
Ne pas faire valider au propriétaire une architecture interne par une capture.

## Rapport et critère de fin

Produire `reports/SCENE-002.md`, `evidence/SCENE-002/manifest.json` et sa ligne
locale. Montrer l'API réelle, les dépendances, le graphe de propriété et les preuves.

Terminé lorsque le socle exécute le chemin scientifique concret sans présentation,
avec les invariants de ressources/invalidation vérifiés. Un modèle vide ou deux
implémentations métier derrière une interface commune sont insuffisants.
Prochaine tâche : [SCENE-003](SCENE-003.md).
