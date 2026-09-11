# État du chantier

Révision du cadrage : **2026-09-10**. Lot A implémenté le **2026-09-10** (001, puis 002, puis 003). Validation runtime
différée selon la cadence du chantier. Voir [le journal](reports/JOURNAL.md).
Lot B implémenté le **2026-09-10** (004 → 005 → 006 → 007). Lot C implémenté le **2026-09-10**, qualification physique Quest à poursuivre après correction des premiers défauts ; voir [le rapport intégré](reports/FINAL.md) et [le prompt de reprise sur un autre poste](RESTART-PROMPT.md).

Avancement code : A_FAIRE, EN_COURS, IMPLEMENTEE, BLOQUEE.
Validation : DIFFEREE, PARTIELLE, REUSSIE, ECHEC.
Manuel : NON_REQUIS, DIFFERE, EN_ATTENTE, VALIDE, REFUSE.
IMPLEMENTEE avec DIFFEREE est un état attendu durant les lots A/B.

| Fiche | Travail | Code | Validation | Manuel |
| --- | --- | --- | --- | --- |
| [SCENE-001](tasks/SCENE-001.md) | Inventaire ciblé et choix concrets | IMPLEMENTEE | DIFFEREE | NON_REQUIS |
| [SCENE-002](tasks/SCENE-002.md) | Généraliser scène, colonnes et présentation | IMPLEMENTEE | DIFFEREE | NON_REQUIS |
| [SCENE-003](tasks/SCENE-003.md) | Partager les opérations de toutes les modalités | IMPLEMENTEE | DIFFEREE | NON_REQUIS |
| [SCENE-004](tasks/SCENE-004.md) | Préparer toutes les ressources et le Data Quest | IMPLEMENTEE | DIFFEREE | NON_REQUIS |
| [SCENE-005](tasks/SCENE-005.md) | Capturer et restaurer la visualisation complète | IMPLEMENTEE | DIFFEREE | NON_REQUIS |
| [SCENE-006](tasks/SCENE-006.md) | Afficher les colonnes communes sur Quest | IMPLEMENTEE | DIFFEREE | DIFFERE |
| [SCENE-007](tasks/SCENE-007.md) | Achever l’intégration et retirer le prototype remplacé | IMPLEMENTEE | DIFFEREE | NON_REQUIS |
| [SCENE-008](tasks/SCENE-008.md) | Stabiliser et qualifier la version intégrée | IMPLEMENTEE | PARTIELLE | EN_ATTENTE |

La validation finale peut couvrir plusieurs fiches en une même exécution.
Consigner les références dans le journal puis le rapport final, sans dupliquer
des résultats dans huit rapports.

## Références de départ

Reprise du **2026-09-11** sur le commit `f9db4b56e14d` : fixtures corrigées
(IDs des configurations de sites), deux diagnostics Windows réussis, builds
Windows/Android réussis. Migration de signature Android terminée, données
restaurées et vérifiées. L’appairage a réussi après correction des illustrations
facultatives ; la livraison a révélé une dépendance native math absente.
L’APK corrigé est installé et vérifié. Une livraison est confirmée `Published`
(16,8 s) et le propriétaire confirme voir les colonnes et leurs labels.
Interactions physiques et diagnostic scientifique Quest restent à qualifier. Les résultats courants sont dans le manifeste de continuation lié
depuis le rapport. La clé de développement commune reste hors Git.

Le registre historique déclare QUEST-018–023 implémentées et validées.
Sources inspectées pour le cadrage : feature/xr-autonomous@639e88306.
Les anciens résultats sont des références du prototype, pas des preuves du futur
code. Les anciens statuts SCENE n’attestent aucune réalisation de cette nouvelle
version.
