# P14 — sécurité, confidentialité, purge et observabilité

## Objectif et résultat observable

Prouver que la session est authentifiée/chiffrée, que les données patient ne sont jamais persistées ni journalisées sur Quest, que les secrets d'appairage sont protégés et que chaque transition de cycle de vie purge ou conserve uniquement ce qui est explicitement autorisé.

## Decision gate

**Hérité :** D10 TLS/pinning, D12 epochs/resync, D17 zéro cache patient persistant, D18 compatibilité, D22 métadonnées UI transitoires et D23 absence de paging/persistance scientifique.

**Décisions sécurité obligatoires :**

- `P14-A` : classification complète des données et IDs, dont l'allowlist des noms/libellés utiles affichables transitoirement ;
- `P14-B` : matrice de cycle de vie exacte pour disconnect, retry, background, timeout, logout, close, crash et reboot ;
- `P14-C` : stockage sécurisé autorisé pour endpoint/clé/empreinte et politique de rotation/révocation ;
- `P14-D` : schéma de logs/metrics, durée, export et redaction ;
- `P14-E` : threat model, risques acceptés et propriétaire sécurité/vie privée ;
- `P14-F` : réponse à identité changée, appareil perdu et révocation d'appairage.

Le terme « courte reprise » ou « background prolongé » n'est pas suffisamment explicite. Sans valeurs/événements P14-B et propriétaire P14-E, aucun code de rétention/purge production.

## Périmètre autorisé

- threat model et classification ;
- secure storage limité ;
- purge mémoire/lifecycle ;
- redaction/diagnostics ;
- tests malveillants, replay et compatibilité.

## Hors périmètre

- stockage patient ;
- analytics cloud ;
- nouvelle cryptographie ;
- conformité médicale ;
- politique juridique inventée par l'agent.

## Hypothèses fixées

- aucune donnée source persistante ;
- aucun nom/path patient dans logs ;
- les noms patient, libellés de site et noms de colonne autorisés peuvent exister en mémoire de session pour l'UI, jamais sur disque ou dans les logs ;
- IDs opaques ;
- TLS/pinning selon P06 ;
- Desktop peut révoquer/fermer une session.

## Dépendances et état initial

- P06 transport ;
- P07 session ;
- P08 cache lifecycle ;
- P13 UX d'erreur peut avancer après définition des états.

## Fichiers/modules pressentis

- DesktopHost/Client security/lifecycle ;
- secure storage adapter ;
- logging/diagnostics communs ;
- tests D6 et documentation opérateur.

## Étapes

1. Résoudre P14-A–F avec propriétaire.
2. Modéliser flux/menaces/trust boundaries.
3. Centraliser classification et redaction.
4. Implémenter secure storage minimal et révocation.
5. Implémenter matrice purge/lifecycle.
6. Tester pairing/replay/identity change/session replace.
7. Scanner filesystem, logs et dumps avec sentinelles D6.
8. Faire relire risques résiduels et obtenir acceptation.

## Tests et commandes

- tests lifecycle pour chaque ligne P14-B ;
- affichage des métadonnées allowlistées puis preuve de purge à la fermeture/nouvel epoch ;
- background/kill/reboot/crash simulé ;
- logcat/filesystem/search sentinelles ;
- duplicate/replay/out-of-order ;
- certificat/clé changée et appareil révoqué ;
- malformed payload/allocation bomb ;
- revue dépendances/CVE/licences de la pile P06.

## Critères de sortie binaires

- [ ] P14-A–F acceptées ;
- [ ] aucune sentinelle patient sur stockage/log après chaque scénario ;
- [ ] les métadonnées humaines nécessaires sont affichables uniquement en mémoire et purgées selon P14-B ;
- [ ] seuls endpoint et matériau autorisé sont persistés ;
- [ ] identité changée bloquée et révocation effective ;
- [ ] purge déterministe testée ;
- [ ] diagnostics utiles sans contenu sensible ;
- [ ] threat model et risques résiduels signés.

## Artefacts à remettre

Threat model, classification/lifecycle matrix, secure storage/redaction/purge, tests D6, rapport scan et acceptation sécurité.

## Conditions d'arrêt

Arrêter si le propriétaire sécurité manque, si une donnée n'est pas classifiée, si P14-B est vague ou si une dépendance exige un service/stockage non autorisé.

## Prompt de démarrage

> Exécute P14 depuis `Docs/dev/xr/implementation-packets/P14-security.md`. Commence en décision/revue : P14-A–F doivent être acceptées avant tout code de rétention. N'invente aucune politique juridique ou durée. Implémente ensuite stockage minimal, purge et redaction, puis prouve avec D6 qu'aucune donnée patient ne persiste ni ne fuit dans les logs.
