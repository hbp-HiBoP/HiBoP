# P06 — spike transport, sécurité de canal et codec

## Objectif et résultat observable

Mesurer sur Windows, macOS, Linux et Quest IL2CPP les candidats nécessaires pour fermer D10/D11. Livrer un ADR accepté choisissant bibliothèque serveur/client, séparation WSS/HTTPS, mécanisme TLS/pinning, schéma de contrôle et format des gros buffers.

## Decision gate

**Hérité :** D10 baseline HTTPS + WSS, découverte facultative/IP manuelle, TLS/pinning ; D11 contrôle AOT-safe + buffers little-endian float32 ; D17 données en mémoire.

**À fixer avant le spike :**

- `P06-A` : candidats et versions à comparer, avec licences ;
- `P06-B` : profils de charge et réseaux de test ;
- `P06-C` : candidats de schéma/codec AOT-safe ;
- `P06-D` : protocole de preuve de possession/pinning à tester, sans crypto maison ;
- `P06-E` : critères pondérés et autorité qui accepte l'ADR.

Le code de spike est autorisé après P06-A–E. Aucun candidat ne devient dépendance de production avant l'ADR final accepté.

## Périmètre autorisé

- hosts/clients minimaux jetables ;
- WSS/HTTPS, TLS, pinning, ranges/chunks ;
- sérialisation de Contracts et buffers synthétiques ;
- instrumentation réseau/GC/allocations.

## Hors périmètre

- intégration HiBoP/RenderModel réelle ;
- persistance patient ;
- découverte production ;
- protocole crypto propriétaire ;
- optimisation fonctionnelle.

## Hypothèses fixées

- un client Quest V1 ;
- contrôle prioritaire au bulk ;
- gros assets content-addressed ;
- float32 sans compression comme baseline ;
- compatibilité trois OS obligatoire.

## Dépendances et état initial

- P02 Contracts et P04 APK bootstrap ;
- machines/CI des trois OS accessibles ;
- Quest et réseaux de test confirmés.

## Fichiers/modules pressentis

- dossier spike isolé/jetable ;
- expérimentation autour de `com.crnl.hibop.protocol`, sans dépendance de production avant l'ADR P06 ;
- rapports sous Docs/dev/xr ;
- aucun branchement au module 3D production.

## Étapes

1. Résoudre P06-A–E.
2. Faire compiler chaque candidat sur quatre plateformes.
3. Implémenter handshake minimal, echo commande et asset range.
4. Transférer bulk 100 MiB pendant 20 commandes/s.
5. Tester coupures 1/5/30 s, identité changée, chunks corrompus et payloads invalides.
6. Mesurer codecs contrôle et buffers float32 ; quantification/compression séparément.
7. Auditer licences, maintenance, copies, allocations et surface d'attaque.
8. Produire ADR comparatif et obtenir acceptation.
9. Supprimer/isoler les candidats rejetés.

## Tests et commandes

- builds Desktop 3 OS et APK IL2CPP ;
- RTT/throughput p50/p95/max ;
- contrôle sous bulk, reprise, ranges et hash final ;
- GC/allocations/copies ;
- malformed input/allocation bomb ;
- vérification TLS/pinning et identité changée ;
- scan licences/SBOM.

## Critères de sortie binaires

- [ ] P06-A–E fixées ;
- [ ] au moins une pile passe les quatre plateformes ;
- [ ] contrôle p95 ≤ 100 ms sous bulk nominal ;
- [ ] identité modifiée et corruption sont rejetées ;
- [ ] codec contrôle fonctionne sous IL2CPP/AOT ;
- [ ] format buffers, endian, framing et compression baseline décidés ;
- [ ] ADR D10/D11 accepté ;
- [ ] aucun candidat rejeté n'est une dépendance production.

## Artefacts à remettre

Prototypes isolés, mesures brutes, matrice licences/risques, golden wire vectors et ADR P06 accepté.

## Conditions d'arrêt

Arrêter si aucun candidat compile sur les quatre plateformes, si le modèle de confiance nécessite une décision sécurité non disponible ou si les machines/appareils ne permettent pas une comparaison équitable.

## Prompt de démarrage

> Exécute P06 depuis `Docs/dev/xr/implementation-packets/P06-transport-spike.md`. Cette phase est décisionnelle : fixe P06-A–E avant de prototyper, garde tout code candidat isolé et ne l'intègre pas à la production. Mesure les quatre plateformes, produis l'ADR D10/D11 et attends son acceptation avant toute généralisation.
