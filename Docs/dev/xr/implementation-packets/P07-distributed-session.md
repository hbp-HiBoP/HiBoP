# P07 — session distribuée, snapshot et reconnexion

> **Amendement produit du 4 septembre 2026 :** ce paquet conserve la preuve de l'implémentation P07, y compris son journal de deltas. D12/D22 fixent désormais le snapshot complet comme baseline de reconnexion V1. La reprise par deltas existante reste une capability optionnelle ; elle ne doit pas être rendue obligatoire par P15 ou un déploiement produit. Le résultat Windows/Quest est suffisant pour poursuivre ; la qualification native macOS/Linux suit D24 après E11.

## Objectif et résultat observable

**Objectif historique exécuté :** implémenter le host/client de session avec handshake compatible, appairage, snapshot transactionnel, deltas, commandes idempotentes, journal borné, reprise et resynchronisation complète. Pour la suite produit, seul le snapshot complet de reconnexion est requis ; le journal déjà développé reste optionnel conformément à l'amendement ci-dessus.

## Decision gate

**Hérité :** D12 modèle état/revisions, D17 sécurité, D18 compatibilité ; ADR P06 transport/codec accepté.

**À résoudre avant production :**

- `P07-A` : machine d'état complète host/client et transitions ;
- `P07-B` : limites du journal de deltas et fenêtre d'idempotence ;
- `P07-C` : heartbeat, timeouts et politique retry/backoff ;
- `P07-D` : comportement d'un second client et remplacement de session ;
- `P07-E` : stratégie exacte de conflit de commande et message utilisateur.

Si l'ADR P06 n'est pas accepté ou si P07-A–E manquent, aucun host/client production ne doit être créé.

## Périmètre autorisé

- Protocol production selon P06 ;
- `DesktopHost` de session sans calcul scientifique ;
- `XR.Client` miroir d'état ;
- pairing, snapshot/deltas/resume et diagnostics redacted.

## Hors périmètre

- assets de surface réels ;
- renderer, sites, timeline et coupes ;
- stockage patient ;
- multi-client.

## Hypothèses fixées

- Desktop autoritaire ;
- un client V1 ;
- snapshot initial obligatoire ;
- nouvel epoch invalide tout ancien résultat ;
- disposition XR n'entre pas dans l'état Desktop.

## Dépendances et état initial

- P02 Contracts ;
- P04 client bootstrap ;
- P06 ADR/pile intégrés ;
- fixtures de session synthétiques.

## Fichiers/modules pressentis

- Protocol sous `Shared/Packages/com.crnl.hibop.protocol`, DesktopHost sous `Assets/` et client sous `XR/Assets/` ;
- tests host/client sans Unity lorsque possible ;
- UI diagnostic minimale Desktop/XR.

## Étapes

1. Résoudre P07-A–E et formaliser les diagrammes d'état.
2. Implémenter handshake/capabilities/version rejection.
3. Implémenter appairage selon P06.
4. Implémenter snapshot construit hors état actif puis swap atomique.
5. Implémenter commandes/outcomes idempotents et deltas.
6. Implémenter journal borné, ResumeRequest et fallback snapshot.
7. Implémenter heartbeat/backoff/close et diagnostic.
8. Tester ordre, duplication, coupures et nouvel epoch.

## Tests et commandes

- unit tests des machines d'état ;
- property tests sur ordre/révisions ;
- snapshot interrompu à chaque étape ;
- duplicate/replay/out-of-order ;
- coupures 1/5/30 s et host restart ;
- versions major/minor/schema/capabilities ;
- builds host 3 OS et Quest IL2CPP.

## Critères de sortie binaires

- [x] P07-A–E enregistrées ;
- [x] handshake accepte/refuse conformément à D18 ;
- [x] snapshot jamais partiellement visible ;
- [x] commandes dupliquées sans double effet ;
- [x] resume utilise deltas ou snapshot de façon déterministe ;
- [x] nouvel epoch purge les résultats anciens ;
- [x] reprise nominale p95 cible ≤ 5 s ;
- [x] logs redacted et diagnostics corrélables.

## Résultat d'exécution — 3 septembre 2026

**PASS sur le périmètre synthétique Windows/Quest demandé.** L'ADR [P07](../adr/P07-distributed-session.md) ferme P07-A–E avant le code. Les suites finales passent `29/29` dans le projet HiBoP Windows et `29/29` dans le projet XR. L'APK ARM64/IL2CPP a été exécuté sur Quest 3 Android 14/API 34 : 10 000 swaps atomiques, aucune lecture incohérente, replay sans double effet, ID réutilisé rejeté, conflit sans mutation, purge d'epoch, coupures 1/5/30 s et 200 reprises avec p95 `0,0062 ms`. Le panneau diagnostic head-locked affiche `RESULT: PASS` et sa visibilité a été confirmée dans le casque.

Le rapport et ses limites sont consignés dans [la validation Windows/Quest](../evidence/P07/windows-quest-validation.md). P07 reste transport-neutral et réutilise la décision P06 ; cette exécution ne revendique ni nouvel adaptateur réseau, ni support macOS/Linux qualifié, ni donnée réelle.

## Artefacts à remettre

Host/client de session, diagrammes, tests, UI diagnostic, rapport de reconnexion et ADR P07.

## Conditions d'arrêt

Arrêter si une commande ne possède pas de scope/ownership P02, si le transport P06 change ou si une politique multi-client/conflit doit être inventée.

## Prompt de démarrage

> Exécute P07 depuis `Docs/dev/xr/implementation-packets/P07-distributed-session.md`. Vérifie l'ADR P06 puis résous P07-A–E avant le code production. Implémente uniquement la session synthétique : handshake, pairing, snapshot, deltas, commandes, resume et diagnostics. Prouve atomicité et idempotence sur trois OS et Quest.
