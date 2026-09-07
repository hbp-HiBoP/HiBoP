# Tâches exécutables Quest

Commande conseillée : **« Implémente QUEST-001 »**, puis une tâche à la fois.
Lire le [contrat](../TASK-WORKFLOW.md) et le [registre](../TASK-STATUS.md).
Les tâches ont un ID stable, une fiche et des critères spécifiques. Les dépendances
définissent l'ordre technique, pas une autorisation de lancer des agents en parallèle.

Les plus grosses anciennes unités étaient J2 (transport + rendu + gestes + UX)
et J4 (natif + entrées + extraction + parité). Elles sont maintenant réparties.
J0–J7 restent les étapes produit, avec [une fiche de démonstration chacune](../milestones/README.md).
Une tâche peut encore être divisée si un obstacle concret l'exige, selon le contrat.

| Tâche | Résultat / fiche | Jalon | Dépendances |
| --- | --- | --- | --- |
| QUEST-001 | [Établir la référence de travail et la fixture anatomique](QUEST-001.md) | J0 | Aucune |
| QUEST-002 | [Unifier les packages et préserver les deux systèmes d'entrée](QUEST-002.md) | J1 | QUEST-001 |
| QUEST-003 | [Produire deux Players avec des profils isolés](QUEST-003.md) | J1 | QUEST-002 |
| QUEST-004 | [Afficher passthrough et contrôleurs Quest](QUEST-004.md) | J1 | QUEST-003 |
| QUEST-005 | [Définir le contrat de snapshot anatomique](QUEST-005.md) | J2 | QUEST-001 |
| QUEST-006 | [Capturer la colonne anatomique Desktop réelle](QUEST-006.md) | J2 | QUEST-005 |
| QUEST-007 | [Rendre un snapshot anatomique sur Quest](QUEST-007.md) | J2 | QUEST-004, QUEST-006 |
| QUEST-008 | [Manipuler localement le groupe cerveau](QUEST-008.md) | J2 | QUEST-007 |
| QUEST-009 | [Qualifier le transport embarqué avant intégration](QUEST-009.md) | J2 | QUEST-004, QUEST-005 |
| QUEST-010 | [Livrer et publier une session complète](QUEST-010.md) | J2 | QUEST-006, QUEST-007, QUEST-009 |
| QUEST-011 | [Exposer appairage, envoi et progression dans l'application](QUEST-011.md) | J2 | QUEST-008, QUEST-010 |
| QUEST-012 | [Qualifier la démonstration anatomique et la déconnexion](QUEST-012.md) | J2 | QUEST-011 |
| QUEST-013 | [Préparer et transférer les contacts synthétiques](QUEST-013.md) | J3 | QUEST-012 |
| QUEST-014 | [Afficher les contacts et masquer le cerveau](QUEST-014.md) | J3 | QUEST-013 |
| QUEST-015 | [Rendre le build natif Android reproductible](QUEST-015.md) | J4 | QUEST-003 |
| QUEST-016 | [Charger et libérer hbp_core dans un Player Quest](QUEST-016.md) | J4 | QUEST-004, QUEST-015 |
| QUEST-017 | [Transférer et reconstruire les entrées de projection](QUEST-017.md) | J4 | QUEST-014, QUEST-016 |
| QUEST-018 | [Extraire la densité commune et l'utiliser sur Desktop](QUEST-018.md) | J4 | QUEST-017 |
| QUEST-019 | [Calculer et qualifier la densité locale Quest](QUEST-019.md) | J4 | QUEST-018 |
| QUEST-020 | [Capturer un instant iEEG et ses paramètres préparés](QUEST-020.md) | J5 | QUEST-019 |
| QUEST-021 | [Partager le pipeline de projection iEEG côté Desktop](QUEST-021.md) | J5 | QUEST-020 |
| QUEST-022 | [Partager l'apparence scientifique des sites](QUEST-022.md) | J5 | QUEST-021 |
| QUEST-023 | [Afficher la projection iEEG calculée sur Quest](QUEST-023.md) | J5 | QUEST-022 |
| QUEST-024 | [Qualifier le prototype Windows complet](QUEST-024.md) | J5 | QUEST-023 |
| QUEST-025 | [Préparer le Player Mac Apple Silicon](QUEST-025.md) | J6 | QUEST-024 |
| QUEST-026 | [Qualifier Mac vers Quest](QUEST-026.md) | J6 | QUEST-025 |
| QUEST-027 | [Décider de la qualification Linux](QUEST-027.md) | J7 | QUEST-026 |
| QUEST-028 | [Préparer le Player Linux retenu](QUEST-028.md) | J7 | QUEST-027 |
| QUEST-029 | [Qualifier Linux vers Quest](QUEST-029.md) | J7 | QUEST-028 |

QUEST-015/016 peuvent être préparées dès les profils prêts pour lever tôt le
risque natif ; cela ne change pas l'ordre des démonstrations au propriétaire.
QUEST-027 est une décision, pas une commande d'installer Linux.
QUEST-028/029 sont conditionnelles à une décision de tester.

Les anciennes entrées B01–B19 de la roadmap sont remplacées par ces IDs :
elles regroupaient plusieurs responsabilités et ne doivent plus servir de tâches.
