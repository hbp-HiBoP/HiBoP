# État, transfert et synchronisation

## Séparer trois états

| État | Propriétaire | Durée | Partage |
| --- | --- | --- | --- |
| Projet et préparation | Desktop | Selon fonctionnement HiBoP existant | Snapshot préparé sélectionné |
| Session scientifique reçue | Chaque Player | Session en mémoire ; fichiers techniques si nécessaires | Identité/provenance et contenu transféré |
| Présentation spatiale | Chaque application | Session locale | Aucun partage de caméra, placement ou échelle |

Le Quest n'a pas besoin d'ouvrir un projet HiBoP complet pour calculer une
projection. Une session préparée ne définit pas un nouveau modèle scientifique ;
elle donne accès aux entrées du modèle commun pour le périmètre admis.

## Contrat de snapshot proposé

Enveloppe versionnée : transferId, sessionId, visualizationId, columnId,
instanceId local lié à ces IDs, contentRevision, version de schéma, révision du
code scientifique/ABI, capabilitySet et manifeste d'assets.

Les IDs existants de Visualization/Column sont conservés. Les révisions de
contenu et de transfert sont explicites ; ne pas prendre l'index de colonne, un
Unity instanceID ou un numéro de frame comme identité métier. Un identifiant de
transfert n'est pas un identifiant de commande scientifique.

Le manifeste référence par hash les buffers ou fichiers nécessaires : surfaces,
sites, volume aux niveaux C/D, paramètres et valeurs préparées. Les longueurs,
comptages, formats et conventions de coordonnées sont déclarés et vérifiés avant
allocation ou appel natif. Les buffers numériques conservent float32 sans
quantification introduite par le transport.

Le snapshot décrit une seule version cohérente. Pendant sa capture, copier ou
figer les entrées nécessaires ; une édition Desktop concurrente ne doit pas
produire un mélange ancien/nouveau. Ne pas maintenir un verrou bloquant sur
le thread Unity pendant l'encodage ou l'envoi.

## Machine de transfert minimale

```text
Non appairé -> Connecté
Connecté -> Réception -> Vérification -> Préparation locale -> Prêt
Réception interrompue -> En attente / nouvelle tentative
Prêt + réseau perdu -> Prêt hors ligne
Prêt hors ligne + réseau retrouvé -> Prêt connecté
```

Les états de connexion et de disponibilité du contenu sont orthogonaux.
La déconnexion n'est pas une instruction de destruction de session.

Les chunks vérifiés peuvent être conservés en mémoire pour une nouvelle
tentative du même contenu. Au minimum, recommencer le transfert interrompu est
acceptable ; la reprise par plages est une optimisation si P08 la fournit
sans élargir le jalon. Reconnexion après transfert terminé : zéro recharge
nécessaire pour continuer la manipulation.

Le Quest acquitte le transfert une fois le snapshot préparé et publié. Une
répétition du même transferId/contentHash retourne le statut existant ; elle
ne recrée ni instance ni calcul déjà terminé. Si l'accusé est perdu, l'émetteur
interroge ou retransmet cette identité. Ce mécanisme concerne la livraison d'un
snapshot, sans inventer un journal de gestes.

En cas de corruption, version inconnue ou entrée scientifique invalide, conserver
la session précédente et présenter l'erreur. Libérer les ressources de staging.
L'application peut refuser une taille incompatible avec les ressources disponibles,
sans tronquer les données pour faire passer le transfert.

## Cycle de vie

Les ressources validées restent détenues par la session tant qu'elle est ouverte.
La perte d'une connexion ou l'expiration d'un token réseau ne révoque pas le
contenu déjà reçu. Adapter les événements de cache P08 : leur politique historique
n'est pas celle du nouveau produit.

La fermeture explicite de session libère meshes, buffers et handles natifs.
Un calcul en cours garde ses ressources jusqu'à sa terminaison effective ; annuler
l'attente ne permet pas de libérer un pointeur encore utilisé. La suspension
Android interrompt éventuellement réseau/tracking ; si le processus survit,
restaurer la présentation et les ressources encore valides. Aucune promesse après
kill/reboot n'est faite.

## Commandes futures

Les commandes scientifiques comme SetCutPlane ou SetTimelineIndex appartiennent
à un jalon ultérieur. Elles appelleront les mêmes services localement, avec une
présentation différente sur Desktop et Quest.

Avant leur introduction, définir scope, source autoritaire choisie, révision de
départ, journal avant envoi, identifiant/ordre, acquittement et rejeu idempotent.
La politique envisagée par le propriétaire consiste à choisir Desktop ou Quest
comme source de vérité ; elle n'est pas encore spécifiée. Ne pas confondre une
commande déjà reçue dont l'accusé est perdu avec une édition concurrente.

Latest-wins peut coalescer un geste scientifique ; ce n'est pas une fusion
silencieuse de deux modèles divergents. Aucune de ces infrastructures n'est un
prérequis du premier cerveau manipulable.
