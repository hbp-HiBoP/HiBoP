# QUEST-029 — Qualifier Linux vers Quest

Jalon : [J7](../milestones/J7.md). Type : intégration / qualification.
Dépendances : [QUEST-028](QUEST-028.md)
Statut et preuves : [registre](../TASK-STATUS.md#quest-029).

## Instructions de reprise

Lire le [contrat d'exécution](../TASK-WORKFLOW.md), les [décisions](../01-decisions-and-open-questions.md)
et [la référence thématique](../07-validation-and-measurement-plan.md) avant d'agir.
Relever l'état réel ; les noms de nouveaux types/menus restent à confirmer dans le code.
Une demande « implémente QUEST-029 » autorise cette tâche, pas le jalon entier.

## Points d'entrée à inspecter

Player Linux et scénario QUEST-024

Pour la mémorisation : `Assets/Scripts/HBP/UI/Quest/DesktopQuestPanel.cs`,
`Assets/Scripts/HBP/Transfer/Transport/PairingStorage.cs` et le
[rapport QUEST-030](../reports/QUEST-030.md).

Les chemins historiques absents se consultent depuis feature/xr / eb26c323e
selon le contrat commun ; ne pas considérer les restes non suivis comme référence.

## À implémenter

- Rejouer les quatre niveaux et la coupure réseau sur Linux réel avec Quest.
- Comparer résultats au socle qualifié et consigner le périmètre exact supporté.
- Fermer le jalon ou enregistrer un report/écart explicite.

## Hors périmètre

Pas d'élargissement fonctionnel ni de support d'autres distributions par inférence.

## Décisions et questions à traiter

Une impossibilité runtime doit être rapportée, pas masquée par la réussite du build.

## Vérifications à réaliser par l'agent

- Vérifier transferts, intégrité, calcul local, indépendance des vues et parité.
- Mesurer les différences de plateforme et archiver les preuves.
- Identifier chaque preuve comme réseau local sans ADB ou USB via ADB. Pour
  l'USB, appliquer les [prérequis Linux de QUEST-028](QUEST-028.md), relever la
  version d'ADB et vérifier découverte, permissions, autorisation et reconnexion
  sous le compte utilisateur normal. Ne pas déduire la qualification Wi-Fi
  d'un succès USB, ni l'inverse. La procédure définitive relève de
  [QUEST-031](QUEST-031.md) (D34).

### Mémoire d'appairage et protection du secret

Précision demandée par le propriétaire le 2026-09-15 : la mémorisation est
prévue dans le code commun, y compris en Wi-Fi, mais reste à qualifier sur Linux.

- `DesktopQuestPanel` écrit puis relit un secret de reconnexion de 32 octets dans
  `Application.persistentDataPath/QuestPairings`, avec un fichier `.pair`
  identifié par l'empreinte du casque. DPAPI n'est pas nécessaire à cette persistance.
- À cette date, `PairingStorage.Protect` retourne les octets inchangés hors
  Windows : sur Linux, le secret est enregistré sans chiffrement applicatif.
  Les permissions du fichier et de son répertoire sont donc à relever sur la
  distribution cible, sans supposer une protection équivalente à DPAPI.
- L'utilité et les limites du chiffrement au repos sont décrites dans
  [QUEST-026](QUEST-026.md#mémoire-dappairage-et-protection-du-secret) : limiter
  l'exploitation d'un fichier copié, dont le secret permet d'usurper le Desktop
  auprès d'un casque joignable. Cette protection est distincte de TLS et ne
  garantit pas la protection contre un logiciel exécuté sous le même compte.
- Sur le Player Linux réel, vérifier premier appairage Wi-Fi, sauvegarde,
  fermeture et relance du Desktop, sélection du même casque puis Pair sans
  nouveau code. Vérifier aussi la persistance après redémarrage normal de
  HiBoP Quest, sans renouvellement avec Y ; tester la coupure/reconnexion séparément.
- Relever le comportement si le fichier est absent, invalide ou inaccessible,
  puis vérifier l'erreur et la récupération. Ne jamais inclure le secret ou le
  fichier `.pair` dans les journaux ou preuves publiées.
- L'absence de chiffrement applicatif ne bloque pas les essais fonctionnels du
  prototype. Rapporter séparément « mémorisation vérifiée » et « protection au
  repos présente/absente ». Une protection adaptée à Linux reste recommandée
  avant distribution finale ; son mécanisme n'est ni choisi ni implémenté par
  cette note. Ne pas déduire une qualification sécurité de la seule reconnexion.

Cette précision sur le stockage est indépendante du choix de transport ;
D34 fixe désormais le réseau principal et l'USB optionnel avec ADB externe.
Elle ne remplace pas la décision de tester Linux requise par QUEST-027.

## Validation manuelle du propriétaire

1. Effectuer le parcours des quatre niveaux et déconnexion/reconnexion ; donner le verdict utilisateur.
2. Après relance du Desktop puis, séparément, de HiBoP Quest, sélectionner le
   casque déjà appairé et confirmer que Pair puis l'envoi fonctionnent sans
   nouvelle saisie du code. Consigner chaque résultat séparément.

Avant de remettre cette checklist, remplacer toute indication générale par les
vrais chemins de binaires, fixture, boutons et commandes de la version livrée.
Ne pas prétendre que ces gestes ont été validés sans résultat observé ou retour utilisateur.

## Explication et points de review attendus

Comparer aux rapports Windows/Mac et limiter les conclusions à l'environnement testé.
Compléter le [modèle de rapport](../reports/TEMPLATE.md), avec les liens exacts
vers les fichiers/symboles modifiés et le résultat de chaque vérification ci-dessus.
Pour cette tâche, produire reports/QUEST-029.md, un manifeste des preuves et mettre
à jour uniquement sa ligne de TASK-STATUS.md.

## Critère de fin

Qualification Linux explicite ou défauts/report documentés, jamais support implicite.
Si une preuve requise manque, conserver son statut non vérifié. Les retours
manuels peuvent rester en attente sans masquer la fin de l'implémentation.
