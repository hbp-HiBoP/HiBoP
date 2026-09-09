# SCENE-005 — Raccorder Quest au modèle commun

Type : migration du consommateur Quest.
Dépendance : [SCENE-004](SCENE-004.md). Suivi : [registre](../TASK-STATUS.md#scene-005).

## Reprise

Lire le [contrat local](../TASK-WORKFLOW.md), [l'architecture](../02-target-architecture.md),
[la restauration](../03-payload-and-restoration.md) et les rapports 002–004.
Réutiliser le transport et les présentations existants, sans reconstruire le prototype.

## Points d'entrée à inspecter

`QuestAnatomySession`, `QuestAnatomyView`, reconstructeurs natifs, intégrations
densité/iEEG issues de 019/023, renderer des sites, manipulateur et prefabs Quest.
Ces noms sont à résoudre sur le code courant. Inspecter aussi ACK, déduplication,
annulation et fermeture dans le chemin de réception.

## À implémenter

- Faire restaurer par la réception le même modèle Scène/Colonne que Desktop.
  L'intégration réseau conserve son rôle de livraison ; les fonctionnalités
  scientifiques ne consultent ni le récepteur ni le payload.
- Faire consommer par la présentation Quest les données/résultats communs.
  Déplacer vers le socle les propriétaires scientifiques encore attachés aux vues.
- Conserver les ressources GPU et la pose dans la présentation. L'absence ou
  reconstruction d'une vue ne ferme pas implicitement la scène scientifique.
- Raccorder les actions scientifiques déjà disponibles aux opérations communes,
  sans créer de contrôles supplémentaires. La fermeture applicative ferme
  explicitement la scène, tandis que la déconnexion la conserve.
- Préserver publication atomique modèle/vue, annulation, conservation de la scène
  précédente en cas d'échec et idempotence après perte d'acquittement.
- Identifier et retirer les chemins remplacés de cette intégration ; les derniers
  résidus transitoires clairement listés sont traités en 007.

## Hors périmètre

Pas de nouvel appairage, nouvelle UI, coupe, timeline, édition ROI, disposition
multicolonne ou import de projet sur Quest. Aucun port de `View3D`/`Camera3D`
ou création d'objets factices pour satisfaire les dépendances historiques.

## Vérifications par l'agent

- Trajet Desktop réel → capture du socle → transport existant → restauration →
  même scène/colonne → calcul local → présentation Quest. Identifier les appels.
- Comparer anatomie, sites, densité et iEEG avec la baseline et Desktop migré.
  Prouver le calcul avec les entrées reçues après déconnexion.
- Transfert identique répété, remplacement valide/invalide, interruption et fermeture
  pendant préparation/calcul : ancien contenu préservé, aucune publication tardive.
- Détruire/recréer un adaptateur de présentation conserve le modèle ; fermer la
  session libère ressources natives, GPU détenues et fichiers techniques au bon moment.
- Manipulation et visibilité locale gardent repères, résultats et indépendance
  de présentation. Vérifier abonnements et ressources sur plusieurs cycles.
- Build et essai Windows/Quest pertinents, avec provenance du contenu réellement
  installé. Distinguer tests Editor/loopback et preuves physiques.

## Validation manuelle

M1 — Suivre la recette fournie pour envoyer anatomie/densité/iEEG, manipuler et
masquer le cerveau, puis vérifier que le contenu reste explorable hors connexion.
Attendu : expérience connue conservée, présentation indépendante, aucun contenu
partiellement remplacé visible dans les scénarios fournis.

Fournir binaires, fixtures, étapes et résultat attendu par étape avant la demande.
Ne pas faire vérifier les invariants de mémoire par le propriétaire. Recueillir
OK/KO daté ; récupérer les preuves puis arrêter HiBoP selon le contrat appareil.

## Rapport et critère de fin

Produire `reports/SCENE-005.md`, `evidence/SCENE-005/manifest.json` et sa ligne
locale. Montrer les propriétaires avant/après et le chemin commun réellement utilisé.

Terminé techniquement lorsque le parcours reçu fonctionne sur le même modèle,
avec conservation de l'expérience et des garanties de livraison/ressources.
L'acceptation manuelle reste un champ distinct. Prochaine tâche : [SCENE-006](SCENE-006.md).
