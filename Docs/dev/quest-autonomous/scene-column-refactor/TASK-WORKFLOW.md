# Exécuter une tâche SCENE

## Autorisation et reprise

Une demande « Implémente SCENE-003 » vise la fiche de ce dossier et autorise son
périmètre uniquement. Chercher l'ID dans [l'index](tasks/README.md). Ne pas
enchaîner la suivante. La création de ces documents n'autorise pas leur exécution.

Lire les AGENTS.md applicables, cette fiche, la tâche, les références thématiques,
le [registre local](TASK-STATUS.md) et les rapports des dépendances. Le
[contrat Quest](../TASK-WORKFLOW.md) reste applicable pour les outils, Unity,
les appareils, l'asynchronisme, la provenance et les tests, avec les adaptations
de chemins et d'ordre d'exécution précisées ici.

Les décisions SC-D01–SC-D06 de [la spécification](01-objective-and-scope.md)
complètent le cadrage historique : 018–023 se terminent avant ce chantier ;
024 est repris après SCENE-008. Cette insertion approuvée ne demande pas une
nouvelle autorisation générale à chaque fiche. Une incertitude produit nouvelle
ou un élargissement matériel demande une décision précise, après travail indépendant.

## Concurrence et périmètre d'écriture

Au début, relever branche, commit, changements locaux et activité concurrente.
Ne pas attribuer les modifications d'un autre agent à cette tâche, les formater,
les corriger, les restaurer ou les supprimer pour nettoyer le checkout.
Ne pas changer de branche, stager globalement, committer ou pousser implicitement.

La rédaction initiale est limitée au nouveau dossier. Les futures demandes
d'implémentation autorisent les changements de code/prefabs/tests nécessaires
à leur fiche. Elles ne demandent pas de modifier les documents historiques
QUEST-018–030, leurs rapports ou leur registre. Le suivi SCENE reste ici.
Tout raccordement des index historiques est une action distincte à coordonner.

La baseline après 023 doit être vérifiée dans le code et les preuves, pas déduite
du seul numéro de tâche. Une validation manuelle en attente n'est pas un blocage
automatique : expliquer si elle conditionne réellement le travail. Un résultat
scientifique incertain ne peut pas servir de référence approuvée.

## Exécution

- Donner le résultat visé et la preuve attendue ; implémenter le minimum concret.
- Préserver Desktop, les modalités hors migration et les conventions scientifiques.
- Respecter prefab-first ; aucun faux renderer, caméra ou toolbar pour activer le socle.
- Utiliser MCP si Unity est ouvert et la CLI appropriée sinon, selon AGENTS.md.
  Ne pas lancer ou interrompre des applications pour résoudre une incertitude d'état.
- Attendre directement les opérations async ; aucune attente bloquante sur le
  thread Unity. Une annulation d'attente n'autorise pas la libération anticipée.
- Exécuter `Tools/format-code.cmd` pour les C# modifiés avant handoff, en
  coordonnant son périmètre si d'autres C# non commités appartiennent à un agent.
- Exécuter les vérifications nécessaires puis le contrôle du diff. Une preuve
  absente reste NON_EXECUTE/PARTIEL ; ne pas inventer une qualification.

Les revues indépendantes bornées de conception, concurrence et intégrité suivent
AGENTS.md. Garder l'agent principal responsable des preuves et de l'intégration.
Ne pas déléguer simplement pour remplir des créneaux disponibles.

Pour les appareils, réutiliser les autorisations pertinentes déjà données dans
la session ; une ancienne preuve d'essai ne vaut pas état courant du matériel.
Après validation d'un essai Quest, récupérer les preuves puis arrêter HiBoP
sur la cible identifiée selon le contrat Quest, sans couper ADB. Ne pas arrêter
l'application pendant que le propriétaire doit encore effectuer sa recette.

## Rapport et registre

Créer `reports/SCENE-NNN.md` selon [le modèle](reports/TEMPLATE.md) et
`evidence/SCENE-NNN/manifest.json` selon [les consignes](evidence/README.md).
Mettre à jour seulement la ligne de cette tâche dans le registre local, avec
trois états indépendants : implémentation, technique, manuel.

Le rapport présente le comportement avant/après, 3–5 points de review, le chemin
commun réellement utilisé, les propriétaires de ressources, les preuves, limites
et éventuels chemins transitoires à retirer. Un inventaire de fichiers n'est pas
une preuve d'architecture commune.

Une validation manuelle nécessite un retour explicite daté/référencé. Préparer
d'abord les binaires, fixtures et actions exactes ; ne pas demander au propriétaire
de vérifier des hashes, conversions ou invariants que les tests couvrent.
Ne pas confondre approbation de cette spécification et réussite d'une future tâche.

Si la fiche doit être subdivisée, conserver les résultats acquis et proposer des
IDs stables SCENE-NNN-A/B avec dépendances. Le découpage ne doit pas masquer un
élargissement du périmètre initial. Actualiser les documents de ce dossier après
la décision requise, sans modifier le backlog historique en parallèle.

## Fin de tâche

Répondre avec le résultat, le lien absolu du rapport, les vérifications effectuées,
les limites réelles et les actions manuelles utiles. Proposer la prochaine tâche
prête sans l'exécuter. Après SCENE-008, transmettre son rapport comme contexte
supplémentaire à la future exécution de QUEST-024 ; ne pas la lancer implicitement.
