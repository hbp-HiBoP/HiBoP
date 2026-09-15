# QUEST-026 — Qualifier Mac vers Quest

Jalon : [J6](../milestones/J6.md). Type : intégration / qualification.
Dépendances : [QUEST-025](QUEST-025.md)
Statut et preuves : [registre](../TASK-STATUS.md#quest-026).

## Instructions de reprise

Lire le [contrat d'exécution](../TASK-WORKFLOW.md), les [décisions](../01-decisions-and-open-questions.md)
et [la référence thématique](../07-validation-and-measurement-plan.md) avant d'agir.
Relever l'état réel ; les noms de nouveaux types/menus restent à confirmer dans le code.
Une demande « implémente QUEST-026 » autorise cette tâche, pas le jalon entier.

## Points d'entrée à inspecter

Player Mac et recette Windows QUEST-024

Pour la mémorisation : `Assets/Scripts/HBP/UI/Quest/DesktopQuestPanel.cs`,
`Assets/Scripts/HBP/Transfer/Transport/PairingStorage.cs` et le
[rapport QUEST-030](../reports/QUEST-030.md).

Les chemins historiques absents se consultent depuis feature/xr / eb26c323e
selon le contrat commun ; ne pas considérer les restes non suivis comme référence.

## À implémenter

- Rejouer les quatre niveaux depuis le Mac réel vers Quest 3.
- Comparer transfert, provenance et résultats scientifiques au Windows de référence.
- Corriger uniquement les défauts de frontière Mac nécessaires et consigner limites/acceptation.

## Hors périmètre

Pas de nouveau protocole spécifique Mac ou affirmation de support de toutes les versions macOS.

## Décisions et questions à traiter

Si l'environnement empêche le test, préciser exactement ce qui manque sans marquer Mac qualifié.

## Vérifications à réaliser par l'agent

- Mesurer transfert/calcul, vérifier parité et reconnexion ; même APK si protocole inchangé.
- Vérifier les conséquences des permissions réseau OS et l'arrêt du transport.
- Identifier chaque preuve comme réseau local sans ADB ou USB via ADB. Pour
  l'USB, appliquer les [prérequis Mac de QUEST-025](QUEST-025.md), relever la
  version d'ADB et vérifier découverte, autorisation et reconnexion sur le Mac
  réel. Ne pas déduire la qualification Wi-Fi d'un succès USB, ni l'inverse.
  La procédure définitive est suivie dans [QUEST-031](QUEST-031.md) (D34).

### Mémoire d'appairage et protection du secret

Précision demandée par le propriétaire le 2026-09-15 : distinguer la
persistance fonctionnelle de la protection du secret sur disque.

- Le code commun sauvegarde un secret de reconnexion de 32 octets dans
  `Application.persistentDataPath/QuestPairings`, avec un fichier `.pair`
  identifié par l'empreinte du casque. Il le relit aussi pour l'appairage Wi-Fi.
  L'absence de DPAPI sur Mac ne désactive donc pas la mémorisation.
- À cette date, `PairingStorage.Protect` chiffre avec DPAPI sous Windows et
  retourne les octets inchangés ailleurs : sur Mac, le secret est enregistré
  sans chiffrement applicatif. Le fonctionnement réel reste à qualifier.
- Ce secret permet à un détenteur pouvant joindre le casque de se présenter
  comme le Desktop autorisé et d'envoyer du contenu sans ressaisir le code.
  La protection au repos limite l'exploitation d'une copie du fichier, par
  exemple issue d'une sauvegarde divulguée. DPAPI le lie normalement au compte
  Windows et à la machine ; il protège peu contre un logiciel exécuté sous ce
  même compte. Voir [Microsoft — CryptProtectData](https://learn.microsoft.com/en-us/windows/win32/api/dpapi/nf-dpapi-cryptprotectdata).
- Cette protection ne remplace pas TLS, qui protège les échanges. Le secret
  seul ne permet ni de déchiffrer les anciens transferts TLS, ni de télécharger
  les projets du Desktop via le protocole actuel.
- Sur le Player Mac réel, vérifier premier appairage Wi-Fi, sauvegarde, fermeture
  et relance du Desktop, sélection du même casque puis Pair sans nouveau code.
  Vérifier aussi la persistance après redémarrage normal de HiBoP Quest, sans
  renouveler l'appairage avec Y. Tester la reconnexion après coupure séparément.
- Relever chemin effectif, permissions et comportement si le fichier est absent,
  invalide ou inaccessible ; vérifier l'erreur et la récupération. Ne jamais
  inclure le secret ou le fichier `.pair` dans les journaux ou preuves publiées.
- L'absence de chiffrement applicatif ne bloque pas les essais fonctionnels du
  prototype. Rapporter séparément « mémorisation vérifiée » et « protection au
  repos présente/absente ». Une protection adaptée à macOS reste recommandée
  avant distribution finale ; son mécanisme n'est ni choisi ni implémenté par
  cette note. Ne pas déduire une qualification sécurité de la seule reconnexion.

Cette précision sur le stockage est indépendante du choix de transport ;
D34 fixe désormais le réseau principal et l'USB optionnel avec ADB externe.

## Validation manuelle du propriétaire

1. Appairer/envoyer depuis Mac, tester gestes et bouton masquer, puis coupure 60 s.
2. Comparer l'expérience à Windows et consigner tout écart visible.
3. Après relance du Desktop puis, séparément, de HiBoP Quest, sélectionner le
   casque déjà appairé et confirmer que Pair puis l'envoi fonctionnent sans
   nouvelle saisie du code. Consigner chaque résultat séparément.

Avant de remettre cette checklist, remplacer toute indication générale par les
vrais chemins de binaires, fixture, boutons et commandes de la version livrée.
Ne pas prétendre que ces gestes ont été validés sans résultat observé ou retour utilisateur.

## Explication et points de review attendus

Rapporter les différences Windows/Mac utiles et leur cause ; aucune répétition de toute l'architecture.
Compléter le [modèle de rapport](../reports/TEMPLATE.md), avec les liens exacts
vers les fichiers/symboles modifiés et le résultat de chaque vérification ci-dessus.
Pour cette tâche, produire reports/QUEST-026.md, un manifeste des preuves et mettre
à jour uniquement sa ligne de TASK-STATUS.md.

## Critère de fin

J6 validé sur Mac identifié, avec résultats réels et accord manuel enregistré.
Si une preuve requise manque, conserver son statut non vérifié. Les retours
manuels peuvent rester en attente sans masquer la fin de l'implémentation.
