# P08 — assets distants, hashes et cache mémoire

## Objectif et résultat observable

Transférer un `SurfaceAsset` P03 depuis le Desktop, le valider par hash, le conserver uniquement en mémoire, le partager entre instances et le libérer/recharger sans état partiel.

## Decision gate

**Hérité :** D14 assets immuables partagés, D17 aucune persistance Quest, P06 HTTP ranges/hash, P07 session/epoch.

**À résoudre avant cache production :**

- `P08-A` : politique de pression mémoire et éviction des assets actifs/inactifs ;
- `P08-B` : comportement utilisateur lorsqu'un asset ne tient pas ou doit être rechargé ;
- `P08-C` : cycle de vie exact sur déconnexion, nouvel epoch, background et fermeture ;
- `P08-D` : limites de sécurité par type d'asset et méthode de négociation, sans plafond fonctionnel métier ;
- `P08-E` : règles de dépendances/hashes pour variantes anatomical/inflated.

Les limites de sécurité protègent les allocations malveillantes ; elles ne doivent pas masquer un nombre de cerveaux/sites. Toute politique qui retire un contenu actif exige une décision produit explicite.

## Périmètre autorisé

- descriptors/chunks/ranges et validation ;
- cache mémoire par hash/refcount ;
- partage renderer statique ;
- reprise/annulation et métriques mémoire.

## Hors périmètre

- cache disque patient ;
- timeline/coupes/sites dynamiques ;
- compression non décidée P06 ;
- réduction silencieuse de contenu.

## Hypothèses fixées

- assets immuables ;
- hash SHA-256 ou choix P06 explicitement accepté ;
- téléchargement authentifié ;
- aucun nom/path patient dans descriptor/log ;
- renderer ne voit qu'un asset complet validé.

## Dépendances et état initial

- P03 SurfaceAsset ;
- P05 renderer ;
- P06/P07 transport/session ;
- D1 MNI baseline.

## Fichiers/modules pressentis

- Desktop asset provider ;
- XR in-memory asset cache ;
- Protocol descriptors/transfers ;
- intégration Rendering et tests.

## Étapes

1. Résoudre P08-A–E.
2. Produire descriptor/hashes depuis Desktop.
3. Implémenter ranges/chunks, staging et hash final.
4. Publier atomiquement dans le cache.
5. Gérer refcounts, déduplication et dépendances.
6. Implémenter annulation/reprise et nouvel epoch.
7. Implémenter pression mémoire conforme P08-A/B.
8. Prouver absence de fichiers et libération.

## Tests et commandes

- chunks manquants/dupliqués/corrompus ;
- resume range et hash final ;
- deux instances/un seul asset physique ;
- cycles open/close/epoch/background ;
- memory profiler et recherche de fichiers après session ;
- asset dimensions malveillantes/allocation bornée.

## Critères de sortie binaires

- [ ] P08-A–E enregistrées ;
- [ ] asset incomplet/corrompu jamais exposé ;
- [ ] déduplication physique démontrée ;
- [ ] aucune écriture de payload sur disque ;
- [ ] cycle de vie et purge conformes ;
- [ ] pression mémoire visible et non destructive selon décision ;
- [ ] reprise/annulation testées.

## Artefacts à remettre

Provider/cache, intégration statique, tests corruption/lifecycle, rapport mémoire et ADR P08.

## Conditions d'arrêt

Arrêter si la politique mémoire exige un plafond produit, si un asset transporte une donnée non autorisée ou si background/purge P08-C entre en conflit avec P14.

## Prompt de démarrage

> Exécute P08 depuis `Docs/dev/xr/implementation-packets/P08-remote-assets.md`. Résous P08-A–E avant le cache production. Implémente staging, hash, partage et purge exclusivement en mémoire ; ne crée aucun cache disque et ne retire jamais silencieusement un contenu actif. Prouve corruption, reprise, déduplication et lifecycle.
