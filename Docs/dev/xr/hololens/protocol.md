# REVIEW BEFORE COMMIT — derived from closed-source prototype

# Audit HoloLens — protocole historique

## Résultat

Il n'existe aucun protocole applicatif HiBoP entre le desktop et le HoloLens.

| Élément recherché | Observation |
| --- | --- |
| transport | Microsoft OpenXR App Remoting |
| contenu | vidéo stéréo, audio optionnel, tracking et entrées |
| adresse | IP/hostname et port saisis dans l'UI |
| débit/codec | paramètres de `RemotingConfiguration` |
| découverte | aucune découverte HiBoP |
| appairage | aucun appairage applicatif |
| framing/sérialisation | gérés par le runtime Microsoft, non définis par HiBoP |
| messages métier | aucun |
| versioning de schéma | aucun |
| snapshot/deltas | aucun |
| révisions/acks | aucun |
| reprise | aucune reprise sémantique |
| sécurité applicative | aucune couche observée |
| transfert de données patient | aucun transfert applicatif ; les données restent sur le PC |

## Réponses aux parcours obligatoires

| Parcours | Message historique |
| --- | --- |
| ouverture d'une visualisation | aucun ; appel local dans le processus PC |
| transfert de mesh | aucun ; mesh construit/chargé localement |
| sélection de site | interaction MRTK locale au processus PC |
| coupe | calcul local, aucune requête réseau métier |
| timeline | événement C# local, aucun message |
| changement de colonne | état local |
| paramètres | état local |
| fermeture | état local ; seule la session remoting peut se déconnecter |

## Pourquoi il n'est pas réutilisable

La V1 Quest exige un rendu local Android, une autorité Desktop explicite, plusieurs assets et résultats révisionnés, une reprise après coupure et une protection contre les réponses obsolètes. App Remoting transmet des pixels et des entrées sans exposer ces contrats. Il contredit donc le besoin produit plutôt qu'il ne l'implémente.

## Leçon exploitable

L'absence de transfert patient dans le prototype résultait du remoting. La nouvelle architecture doit préserver cette propriété volontairement : données sources et identifiants humains restent Desktop ; seuls des IDs opaques, assets de rendu et résultats post-projection transitent ; rien n'est persisté sur Quest.

Le futur protocole est spécifié dans `../03-protocol-and-render-contract.md`. Aucune compatibilité wire avec le prototype n'est requise.
