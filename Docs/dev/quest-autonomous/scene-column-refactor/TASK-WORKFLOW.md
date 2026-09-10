# Workflow de ce chantier

## Autorisation

La réécriture du dossier a été autorisée le 10 septembre 2026 ; elle ne lance pas
l’implémentation.

Une demande future « implémente le chantier scene-column-refactor » autorise
l’ensemble des lots et les corrections nécessaires jusqu’à la stabilisation.
Une demande portant sur un lot autorise ses fiches et les adaptations nécessaires.
Une demande explicitement limitée à une fiche ou à une analyse respecte cette limite.

Ne pas recréer une demande d’autorisation à chaque fiche dans un périmètre déjà
autorisé. Les fiches sont des repères, pas des étapes à lancer dans huit
conversations distinctes. Ne pas déduire une extension de scope du silence du
propriétaire ; demander seulement les décisions produit réellement nouvelles.

## À la reprise

Lire les AGENTS.md, le README, les quatre documents thématiques et le registre.
Relever branche, commit et modifications locales. Préserver les travaux concurrents.
Reprendre l’inventaire/journal déjà disponible sans refaire tout l’audit ni les
anciennes campagnes Quest.

Les nouvelles décisions de ce dossier priment sur les anciennes contraintes
documentaires du prototype : périmètre complet, anciennes versions de transfert
abandonnées, grandes passes de code et validation finale regroupée.
Le contrat Quest historique ne doit pas réintroduire une recette par fiche.

## Exécution autonome

- Appliquer [la cadence de développement](04-migration-and-validation.md).
- Utiliser les classes existantes ; conserver Unity et les composants communs
  lorsqu’ils conviennent. Pas de deuxième branche métier Quest.
- Respecter prefab-first et sérialiser les références nécessaires.
- Maintenir les contrats des projets Desktop ; ne pas utiliser une copie de
  projet utilisateur pour des tests destructifs de sauvegarde.
- Respecter les règles async/non-blocage et la terminaison native réelle.
- Suivre les AGENTS.md pour MCP si Unity est ouvert et CLI si fermé. Ne pas
  démarrer, arrêter ou réinitialiser une application pour résoudre une incertitude
  d’état que l’utilisateur peut clarifier.
- Aucun test/build obligatoire durant A/B. Un obstacle concret peut justifier
  un contrôle court ; reprendre ensuite le travail.
- Formater les C# selon AGENTS.md avant review/handoff, en respectant les travaux
  concurrents. Ce formatage n’impose pas un build.
- Effectuer une revue indépendante bornée pour la conception et les risques de
  concurrence/intégrité selon AGENTS.md ; pas une revue imposée à chaque fiche.

Ne pas committer, pousser ou changer de branche implicitement.
L’implémentation peut toucher les dépôts associés si nécessaire à cette portée,
en suivant leurs instructions. La présente rédaction ne modifie que ce dossier.

## Suivi léger

Le registre distingue avancement du code et validation. Les fiches 001–007
peuvent être IMPLEMENTEE avec validation DIFFEREE jusqu’à 008.
Cela signifie que le code est écrit/intégré, pas que son fonctionnement est prouvé.

Créer au besoin un unique `reports/JOURNAL.md` : décisions d’implémentation,
carte succincte des responsabilités, références de fixtures et liste des défauts
ou contrôles différés. Une ligne utile vaut mieux qu’un inventaire de fichiers.

En fin de chantier, produire `reports/FINAL.md` selon [le modèle](reports/TEMPLATE.md)
et `evidence/final/manifest.json` selon [les consignes](evidence/README.md).
Aucun manifeste intermédiaire obligatoire.

## Appareils et validation manuelle finale

Réutiliser les autorisations pertinentes de la session, sans supposer qu’un
ancien état d’appareil est encore actuel. Fournir les binaires/fixtures et une
recette concrète avant de demander les gestes au propriétaire.
Ne pas demander de vérifier des hashes, valeurs scientifiques ou invariants
mémoire que l’agent peut contrôler.

Ne pas arrêter HiBoP pendant une recette manuelle en attente. Après confirmation
de l’essai terminé/validé, récupérer les preuves puis arrêter HiBoP sur le Quest
identifié, conformément à la consigne appareil existante, sans couper ADB.
Ne pas confondre retour utilisateur, preuve automatisée et simple compilation.

## Fin

Après A/B : continuer si le périmètre autorisé le permet ; ne pas attendre une
acceptation intermédiaire. Si la demande était limitée, transmettre un état bref
et les vérifications différées, sans lancer une qualification pour rendre le
handoff artificiellement « vert ».

Après C : rapport final, résultats, bugs connus et limites réelles.
Une preuve absente reste absente. Ne pas déclarer toute la migration validée si
une modalité obligatoire ou le parcours physique n’a pas été vérifié.
Fournir les éléments réutilisables pour 024 sans modifier son registre hors scope.
