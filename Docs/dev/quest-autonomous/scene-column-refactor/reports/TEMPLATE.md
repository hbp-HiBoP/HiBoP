# Rapport SCENE-NNN — Titre

> Modèle de rapport, pas une preuve d'exécution. Remplacer les champs et retirer
> les sections non applicables. Aucun succès ou retour utilisateur n'est prérempli.

## Résultat

Comportement avant/après, périmètre réalisé et valeur pour la scène commune.
Lien vers la fiche ; écarts de périmètre et décision associée s'il y en a.

## État et provenance

- Implémentation : à renseigner selon le registre local.
- Technique : à renseigner ; distinguer partiel, réussi et non exécuté.
- Manuel : à renseigner ; chaque retour est daté/référencé.
- Branche, commit, changements non commités précisément identifiés.
- Versions Unity/packages et provenance native effective.
- Baseline et dépendances réellement utilisées, limites de leurs preuves.
- Lien vers le manifeste local `evidence/SCENE-NNN/manifest.json`.

## Chemin commun et ressources

Expliquer d'où viennent les données et comment Desktop/restauration alimentent
les mêmes scène/colonnes. Identifier l'opération publique, les dépendances de
recalcul et la publication. Distinguer les ressources possédées, prêtées, copiées
et partagées, leur mutabilité et le moment réel de leur libération.

Si la tâche ne change pas ces chemins, référencer la preuve applicable et expliquer
pourquoi elle reste valide. Lister les façades transitoires, callers et sortie prévue.

## Points de review

| Priorité | Fichier/symbole/objet réel | Changement et raison | Invariant à vérifier |
| --- | --- | --- | --- |
| 1 | À renseigner | À renseigner | I01–I10 ou obligation spécifique |

Limiter la lecture prioritaire à 3–5 points utiles. Pour une revue indépendante,
consigner son périmètre, constats, réponses et preuves après correction.

## Vérifications

| ID | Scénario et commande exacte | Environnement/fixture | Résultat, exit code et preuve |
| --- | --- | --- | --- |
| T1 | À renseigner | À renseigner | NON_EXECUTE avant réalisation |

Indiquer nombres de tests, valeurs/unités/tolérances utiles et sources des
artefacts. Distinguer test de modèle sans UI, Editor, Player et appareil physique.
Comparer avant/après quand requis. Expliquer les preuves reprises sans les
présenter comme des exécutions nouvelles.

## Validation manuelle

Si nécessaire, fournir binaires/fixtures/contrôles/valeurs exacts avant demande.

| ID | Action précise | Résultat attendu | Statut et retour explicite |
| --- | --- | --- | --- |
| M1 | À renseigner | À renseigner | EN_ATTENTE avant réponse |

Sinon : NON_REQUIS et justification. Ne pas déléguer les tests techniques au
propriétaire. Distinguer arrêt de HiBoP vérifié, non exécuté ou non applicable.

## Décisions, limites et suite

Règles respectées, décisions SC-Dxx ou ultérieures, hypothèses restant à lever,
preuves obsolètes/absentes, modalités hors migration et compatibilités effectives.
Conclusion sur les critères de fin et prochaine tâche prête, sans l'exécuter.
Pour SCENE-008, inclure le bloc de reprise concret de QUEST-024.
