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

- [x] P06-A–E fixées ;
- [ ] au moins une pile passe les quatre plateformes ;
- [x] contrôle p95 ≤ 100 ms sous bulk nominal sur P06-W et P06-WQ ; P06-ML natif reste requis ;
- [x] identité modifiée et corruption sont rejetées sur Windows et Quest physique ;
- [x] codec contrôle et trois golden vectors fonctionnent sous IL2CPP/AOT ;
- [x] format buffers, endian, framing et compression baseline décidés dans l’ADR candidat ;
- [x] ADR D10/D11 accepté au statut provisoire Windows/Quest ;
- [x] aucun candidat rejeté n'est une dépendance production.

## Progression au 2 septembre 2026

- `P06-W` : **PASS** sur Windows N0, cinq répétitions 120 s, sécurité négative et launcher invisible validés ;
- `P06-WQ` : **PASS** — Quest 3 physique Android 14/API 34, APK ARM64/IL2CPP, cinq répétitions N2 de 120 s, 12 000 commandes sans échec, pire p95 `19,1079 ms`, bulk moyen `38,159 Mbit/s`, golden vectors et rejets identité/corruption validés ;
- `P06-ML` : **CROSS-PUBLISH PASS / NATIVE PENDING** — packages Linux x64 et macOS ARM64 produits, sans mesure native ;
- NativeWebSocket verrouillé : **VETO** faute de callback de certificat WSS ;
- recommandation D10/D11 : **ACCEPTÉE** au statut `PROVISIONAL — WINDOWS/QUEST VALIDATED` le 2 septembre 2026 ; P06-ML natif est différé et non bloquant pour P07+, aucun support macOS/Linux n’est encore qualifié ;
- revue sécurité externe : abandonnée par décision du propriétaire, qui accepte la revue de base et les risques résiduels documentés ;
- packaging : le sidecar compressé représente environ 25 % des quelque 200 Mio de HiBoP et dépasse le budget de 10 %, sans bloquer la suite ; édition XR séparée ou alternative embarquée moins coûteuse à décider avant distribution.

Le propriétaire conclut et accepte P06 sur ce périmètre le 2 septembre 2026. Le critère original d’exécution sur quatre plateformes est transféré à la qualification `P06-ML` et ne bloque pas P07+; il interdit seulement de déclarer macOS/Linux supportés avant leurs essais natifs. Une alternative embarquée au sidecar peut rouvrir D10 ultérieurement sans invalider la progression actuelle.

## Artefacts à remettre

Prototypes isolés, mesures brutes, matrice licences/risques, golden wire vectors et ADR P06 accepté.

## Conditions d'arrêt

Arrêter si aucun candidat compile sur les quatre plateformes, si le modèle de confiance nécessite une décision sécurité non disponible ou si les machines/appareils ne permettent pas une comparaison équitable.

## Prompt de démarrage

> Exécute P06 depuis `Docs/dev/xr/implementation-packets/P06-transport-spike.md`. Cette phase est décisionnelle : fixe P06-A–E avant de prototyper, garde tout code candidat isolé et ne l'intègre pas à la production. Mesure les quatre plateformes, produis l'ADR D10/D11 et attends son acceptation avant toute généralisation.
