# Preuves regroupées

Aucun dossier de preuves ni manifeste obligatoire par fiche.
Durant A/B, noter les rares contrôles utiles dans le journal.

En SCENE-008, créer `evidence/final/manifest.json` à partir du
[modèle](manifest-template.json). Y référencer les sources, fixtures, binaires
et résultats effectivement utilisés, avec les chemins et empreintes utiles
à leur identification. Les sorties volumineuses peuvent rester dans les
répertoires d’artefacts existants ; ne pas les dupliquer ici.

Un même essai peut couvrir plusieurs fiches/critères et les besoins pertinents
de QUEST-024. Référencer la preuve une fois, puis indiquer sa couverture.
Les résultats historiques non rejoués restent des références de comparaison,
pas des exécutions de la nouvelle version.

Ne jamais remplir un résultat positif par défaut. Distinguer NON_EXECUTE,
REUSSI, ECHEC, PARTIEL et NON_REQUIS pour les vérifications.
Les retours manuels exigent une réponse explicite ; consigner les contrôles différés
et leurs limites sans bloquer artificiellement les lots d’implémentation.
