# QUEST-030-OBS-01 — Activité projetée sur Quest alors que désactivée sur Desktop

Date du signalement : 2026-09-14.
Statut : **OUVERT — analyse reprise le 2026-09-15, plan proposé, correction non engagée**.

## Observation et résultat attendu

Lors de la recette manuelle QUEST-030, le propriétaire a connecté le Quest,
ouvert un projet et une visualisation sur Desktop, puis envoyé cette
visualisation au casque. Le contenu a bien été reçu et était visible dans le
Quest, mais l'activité y était projetée alors qu'elle ne l'était pas dans la
visualisation Desktop envoyée.

Résultat attendu : l'état d'activation de la projection d'activité de la
visualisation source doit être respecté à réception sur Quest. Une activité
non projetée sur Desktop ne doit pas être activée automatiquement dans le casque.

Retour exact : « l'activité était projetée sur le quest alors qu'elle ne
l'était pas dans la visualisation que j'ai envoyé, mais c'est un problème à
régler plus tard ».

## Portée et preuves

- Source : retour manuel du propriétaire dans la discussion QUEST-030.
- Plateformes : Windows IL2CPP et Quest 3 Android ARM64, transfert USB via ADB.
- Binaires : ceux de la [validation manuelle](../evidence/QUEST-030/manual-validation.json).
- Le projet, la visualisation et la modalité d'activité exacts ne sont pas
  consignés ; ne pas les déduire d'une recette antérieure.
- Le défaut de cette recette n'a pas été reproduit indépendamment par l'agent.
  L'analyse du 2026-09-15 établit un calcul forcé dans le parcours de réception
  et des divergences d'initialisation. Leur contribution à cette recette reste
  à vérifier. Les préférences conservées depuis l'appairage constituent le
  fonctionnement voulu, confirmé par le propriétaire.

Le propriétaire valide le parcours global de connexion, envoi, visualisation
et reconnexion USB avec ce défaut explicitement différé. Cette validation ne
vaut pas correction ni acceptation définitive de l'écart d'affichage.

## Reprise du 2026-09-15

Le propriétaire a demandé une analyse complète de la parité Desktop/Quest,
puis le retrait de l'implémentation proposée prématurément. Tous les ajouts de
code de cette intervention, y compris les préférences propres aux scènes/colonnes
et les changements natifs, ont été retirés.

Voir l'[analyse et le plan d'implémentation](QUEST-030-state-transfer-analysis.md).
La cible est le chargement commun d'un snapshot de visualisation fondé sur les
configurations existantes, enrichies pour les paramètres que l'on veut persistants.
Il n'est pas demandé de conserver exhaustivement l'état temporaire de la scène,
notamment le site sélectionné. Les préférences restent accessibles via
`PersistentDataManager.UserPreferences` et sont envoyées uniquement à l'appairage.
Leur synchronisation est différée. Le plan n'est pas implémenté dans cette passe.

La parité comportementale se vérifie avec les mêmes configurations et les mêmes
préférences. Une préférence modifiée sur Desktop après appairage peut différer
sur Quest : le transfert de scène ne doit pas la synchroniser implicitement.

## Vérification à réaliser lors de l'implémentation

Reproduire avec auto compute désactivé puis activé dans les préférences capturées
à l'appairage. Vérifier les configurations capturées, transportées et chargées
dans Quest, y compris lors d'un second envoi après une scène déjà chargée.
Vérifier les transitions et initialisations communes. Un changement de préférences
Desktop après appairage doit laisser les préférences Quest inchangées, selon la
matrice du plan.

Voir le [rapport QUEST-030](QUEST-030.md) et le [registre](../TASK-STATUS.md#quest-030).
