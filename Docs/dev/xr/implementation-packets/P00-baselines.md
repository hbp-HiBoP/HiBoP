# P00 — baselines, datasets et golden outputs

## Objectif et résultat observable

Créer une baseline reproductible du comportement scientifique et visuel actuel : inventaire D0–D6, métadonnées, captures de buffers/images, métriques Desktop et procédure de régénération. Le résultat ne change aucun comportement applicatif.

## Decision gate

La fiche de décision de l'exécution courante est disponible dans
[`../P00-decision-catalog.md`](../P00-decision-catalog.md). Son statut `GO`
autorise uniquement les fixtures synthétiques et les assets MNI qui y sont
énumérés.

**Hérité :** invariants produit, D06 calcul Desktop, D17 confidentialité, baselines de commits du README.

**À résoudre avant toute capture :**

- `P00-A` : liste exacte et chemins locaux des datasets D0–D6 ;
- `P00-B` : autorisation d'utiliser chacun dans tests/artefacts, et niveau de sensibilité ;
- `P00-C` : emplacement versionné des fixtures synthétiques et emplacement non versionné des données réelles ;
- `P00-D` : propriétaire scientifique qui valide les résultats attendus.

Si une de ces décisions manque, statut `DECISION_ONLY` : produire le catalogue proposé et attendre validation. Ne copier aucune donnée réelle.

## Périmètre autorisé

- documentation, scripts de mesure/tests et petites fixtures synthétiques ;
- capture hors Git des counts, hashes, buffers et images redacted ;
- définition de tolérances, sans les inventer pour les données scientifiques.

## Hors périmètre

- correction d'un résultat scientifique ;
- refactor de renderer ;
- ajout XR/réseau ;
- déplacement de données patient dans le dépôt.

## Hypothèses fixées

- D0/D5/D6 sont synthétiques et versionnables après revue ;
- D1–D4 peuvent rester référencés par manifeste/hash local ;
- toute valeur patient est absente des logs et noms d'artefacts.

## Dépendances et état initial

- HiBoP Desktop à la révision de baseline ou différence explicitement enregistrée ;
- Unity ouvert : utiliser MCP ; fermé : ne lancer le CLI qu'avec les règles du dépôt ;
- worktree inspecté et modifications utilisateur préservées.

## Fichiers/modules pressentis

- `Docs/dev/xr/07-validation-plan.md` ;
- nouveau manifeste de fixtures sous un emplacement décidé par P00-C ;
- tests/outils existants de projection et Module3D ;
- `.artifacts/xr/` pour toutes les captures, buffers, images et goldens générés ; seules de petites fixtures synthétiques sources explicitement autorisées peuvent être versionnées.

## Étapes

1. Résoudre P00-A–D et enregistrer la décision.
2. Inventorier versions, hashes, géométries, colonnes, sites et timelines.
3. Créer D0, D5 et D6 minimaux.
4. Capturer résultats Desktop avant sérialisation : surfaces, sites, coupes et timeline.
5. Produire images golden uniquement si leur diffusion est autorisée.
6. Mesurer temps/mémoire Desktop sans extrapolation Quest.
7. Documenter commande de régénération et critères de comparaison.

## Tests et commandes

- tests EditMode ciblés des générateurs/temporal sampling ;
- vérification des hashes et du manifeste ;
- scan de données sensibles dans artefacts/logs ;
- exécution répétée pour stabilité des golden outputs.

Les commandes exactes sont consignées avec la version Unity et le commit.

## Critères de sortie binaires

- [x] P00-A–D sont explicitement enregistrées ;
- [x] D0–D6 ont un manifeste, un propriétaire et une politique de diffusion ;
- [x] au moins un golden surface, site, coupe et D5 temporel est reproductible ;
- [x] aucune donnée sensible n'est versionnée/loggée par P00 ;
- [x] égalité numérique fixée ; aucune tolérance visuelle globale inventée ;
- [x] procédure de régénération testée.

## Artefacts à remettre

Manifeste, fixtures synthétiques autorisées, hashes et rapport textuel de baseline versionnables, commandes et ADR P00. Les captures/buffers/images/goldens générés restent sous `.artifacts/xr/`.

## Conditions d'arrêt

Arrêter si un dataset réel ne peut être classifié, si la baseline n'est pas reproductible ou si une tolérance scientifique doit être choisie sans propriétaire.

## Prompt de démarrage

> Exécute le paquet P00 dans `Docs/dev/xr/implementation-packets/P00-baselines.md`. Commence par lire AGENTS.md, D01–D24 et le plan de validation. Exécute le Decision gate et annonce GO, DECISION_ONLY ou BLOCKED. Ne capture ni ne versionne aucune donnée tant que P00-A–D ne sont pas explicitement résolues. Livre uniquement les baselines, fixtures autorisées, preuves et documentation prévues.
