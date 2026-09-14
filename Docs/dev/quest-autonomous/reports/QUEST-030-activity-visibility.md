# QUEST-030-OBS-01 — Activité projetée sur Quest alors que désactivée sur Desktop

Date du signalement : 2026-09-14.
Statut : **OUVERT — correction différée à la demande du propriétaire**.

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
- Le défaut n'a pas été reproduit indépendamment par l'agent. Sa cause n'est
  pas diagnostiquée : capture Desktop, transfert, état initial et affichage
  Quest sont des points à examiner lors de la reprise, pas des causes établies.

Le propriétaire valide le parcours global de connexion, envoi, visualisation
et reconnexion USB avec ce défaut explicitement différé. Cette validation ne
vaut pas correction ni acceptation définitive de l'écart d'affichage.

## Reprise ultérieure

Reproduire avec une visualisation dont la projection d'activité est désactivée,
puis avec la même projection activée. Vérifier l'état capturé, transporté et
appliqué dans le Quest, y compris lors d'un second envoi après une scène déjà
chargée. Ne pas engager cette correction sans reprise du sujet par le propriétaire.

Voir le [rapport QUEST-030](QUEST-030.md) et le [registre](../TASK-STATUS.md#quest-030).
