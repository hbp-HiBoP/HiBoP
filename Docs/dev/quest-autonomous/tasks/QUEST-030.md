# QUEST-030 — Simplifier la découverte et l'appairage sécurisé

Position : après les jalons J0–J7. Type : implémentation ciblée / qualification sécurité et UX.
Dépendances : [QUEST-011](QUEST-011.md), [QUEST-026](QUEST-026.md),
[QUEST-027](QUEST-027.md) ; [QUEST-029](QUEST-029.md) si Linux est retenu.
Statut et preuves : [registre](../TASK-STATUS.md#quest-030).

## Instructions de reprise

Lire le [contrat d'exécution](../TASK-WORKFLOW.md), les [décisions](../01-decisions-and-open-questions.md)
et la [spécification](../04-prototype-specification.md), puis les rapports des dépendances.
Une demande « implémente QUEST-030 » autorise cette tâche, pas les fonctionnalités ultérieures.
La rédaction de cette fiche n'autorise pas son exécution immédiate.

Attendre la clôture des jalons précédents. Si QUEST-027 diffère Linux, ce report
explicite clôt J7 sans imposer QUEST-028/029 comme prérequis artificiel.
Ne pas déduire la qualification d'une plateforme de celle d'une autre.

## Résultat utilisateur attendu

1. Ouvrir HiBoP sur le Quest ; l'application affiche un code d'appairage à six chiffres.
2. Sur Desktop, sélectionner le casque dans une liste déroulante et valider.
3. Saisir le code affiché dans ce casque et valider l'appairage.
4. Utiliser **Envoyer au Quest** comme auparavant.

Le parcours normal ne demande ni adresse IP, ni inspection ou comparaison
manuelle d'empreinte, ni confirmation supplémentaire dans le casque.
Les appareils proposés sont les récepteurs HiBoP ouverts et joignables sur le
réseau local, pas tous les casques présents ou éteints.

## Points d'entrée à inspecter

- `Assets/Scripts/HBP/UI/Quest/DesktopQuestPanel.cs` et les prefabs de connexion.
- `Assets/Scripts/HBP/Transfer/Transport/QuestPairing.cs` et `TransportIdentity`.
- `Assets/Scripts/HBP/Quest/Runtime/QuestConnectionPanel.cs` et `QuestAnatomySession.cs`.
- Tests de transport, session et UI de QUEST-011 ; dépendances cryptographiques
  et profils/builds IL2CPP réellement utilisés à cette date.

## À implémenter

- Découverte locale et liste actualisée, avec noms permettant de distinguer les
  candidats, retrait des annonces périmées et états recherche/aucun appareil/erreur.
  mDNS/DNS-SD est une piste à qualifier, pas une bibliothèque déjà choisie.
- Conserver une saisie d'adresse dans un accès de secours lorsque la découverte
  est filtrée ou indisponible ; appliquer le même appairage sécurisé à cet accès.
- Remplacer le rôle d'authentification de la comparaison d'empreinte par un
  protocole adapté aux codes courts, de type PAKE, avec confirmation mutuelle
  des clés. Ne pas simplement accepter un certificat inconnu puis lui envoyer
  le code actuel.
- Qualifier d'abord une implémentation maintenue et éprouvée : licence,
  compatibilité Unity/IL2CPP, Windows, Android ARM64, Mac Apple Silicon et Linux
  uniquement si retenu. Réutiliser une implémentation existante ; ne pas inventer
  de primitives cryptographiques. Le choix du protocole et de la bibliothèque
  reste à justifier à l'exécution.
- Lier l'authentification à la session et au canal de transfert effectivement
  utilisés avant d'échanger des secrets ou données. Une annonce de découverte,
  un nom de casque ou une adresse ne constitue jamais une preuve d'identité.
- Conserver un code aléatoire affiché localement, à durée limitée, consommé après
  succès, et des essais bornés pour toute la session d'appairage, y compris entre
  connexions successives ou simultanées. Ne publier ni code ni secret dans la
  découverte ou les journaux. Préserver la résistance au rejeu et au devinage
  hors ligne ; ne pas prévoir de repli silencieux vers un mode non authentifié.
- Rendre compréhensibles expiration, mauvais code, disparition du casque et
  renouvellement après pause/reprise. Préserver le transfert, la nouvelle tentative,
  le contenu déjà publié et l'indépendance des vues.

## Hors périmètre

Pas d'harmonisation graphique générale ni de migration du panneau vers le système
de fenêtres HiBoP : cette intégration reste différée séparément. Pas de compte,
service cloud, appairage distant, mémorisation durable d'identité ou envoi simultané
à plusieurs casques. La liste permet de choisir un destinataire unique.
Pas de modification des calculs scientifiques ou contrats de contenu pour simplifier l'appairage.

## Décisions et questions à traiter

D28 fixe le parcours et son placement après les jalons. La facilité d'intégration
et l'équivalence des garanties de sécurité ne sont pas encore démontrées.
Si aucune dépendance appropriée ne fonctionne sur les cibles retenues, documenter
le blocage et présenter les alternatives au propriétaire, sans affaiblir le
protocole existant ni élargir silencieusement le périmètre.

## Vérifications à réaliser par l'agent

- Revue indépendante bornée du protocole complet et de son intégration : confiance
  dans la découverte, confirmation des clés, liaison au canal, limites d'essais,
  annulation et compatibilité avec les anciennes versions.
- Tests du mauvais code, expiration, rejeu, faux récepteur/certificat, annonce
  usurpée, doublons de noms, connexions concurrentes, interruptions et renouvellement.
  Aucun de ces cas ne doit publier de données à un pair non authentifié.
- Vecteurs de test officiels du protocole choisi, tests d'intégration et essais
  dans les vrais Players IL2CPP ; un succès dans l'éditeur ne suffit pas.
- Découverte, disparition/réapparition, sélection entre plusieurs annonces,
  découverte indisponible et accès par adresse ; UI réactive et double clic borné.
- Parcours de transfert réel sur les plateformes qualifiées, conservation des
  données et manipulations après déconnexion ; vérifier que la simplification
  d'appairage ne régresse pas les fonctionnalités des jalons précédents.

## Validation manuelle du propriétaire

1. Avec HiBoP ouvert sur Quest, le sélectionner dans la liste Desktop, saisir
   uniquement son code, puis envoyer la visualisation et la manipuler sur Quest.
   Confirmer l'absence de saisie IP et de comparaison d'empreinte dans ce parcours.
2. Saisir un code erroné, puis corriger ; vérifier l'erreur et la reprise.
3. Suivre la recette préparée pour un casque temporairement indisponible et
   pour l'accès de secours par adresse ; vérifier que l'état et l'action attendue
   restent compréhensibles.

Avant de demander ces gestes, fournir les vrais binaires, appareils, fixture,
boutons et résultats attendus. Recueillir OK/KO et observations par scénario.
Les tests adversariaux sont à exécuter par l'agent, pas à déléguer au propriétaire.

## Explication et points de review attendus

Produire `reports/QUEST-030.md`, `evidence/QUEST-030/manifest.json` et mettre à jour
la ligne QUEST-030 du registre selon le contrat. Expliquer le parcours avant/après,
le mécanisme qui remplace la comparaison d'empreinte, les dépendances retenues,
les plateformes réellement testées et les limites réseau. Joindre les preuves de
revue, tests et recette ; ne pas présenter le seul usage d'un PAKE comme une
preuve de sécurité de l'ensemble.

Références de départ, à revérifier lors du choix technique :
[DNS-SD, RFC 6763](https://www.rfc-editor.org/rfc/rfc6763.html),
[mDNS, RFC 6762](https://www.rfc-editor.org/rfc/rfc6762.html),
[SPAKE2, RFC 9382](https://www.rfc-editor.org/rfc/rfc9382.html) comme exemple de PAKE,
sans prescription de cet algorithme.

## Critère de fin

Le parcours liste → code → appairage → envoi fonctionne dans les deux applications,
avec authentification vérifiée et garanties documentées, sans comparaison manuelle
d'empreinte ni IP dans le cas normal. Les preuves absentes restent non vérifiées ;
aucune sécurité équivalente ou prise en charge de plateforme n'est revendiquée par inférence.
