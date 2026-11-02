# Qualification USB et réseau Windows 11 / Quest 3 — 27 octobre 2026

**Windows 11 / Quest 3 a été accepté fonctionnellement pour les builds testés.**
Cette campagne terminée ne qualifie pas le checkout courant, macOS, Linux ou
Quest 3S. Le refus/retry du consentement NCM n'a pas été exercé et a été accepté
non bloquant. Ce résultat documente une validation matérielle ; il ne conditionne
pas la publication des plateformes dans la CI.

## Conditions de mesure

Windows 11, Quest 3, Android 14, identifiant système `52433670048800520` ; version
commerciale Horizon non confirmée. Le Player final était `WindowsRelease2`,
Release IL2CPP, avec l'APK déjà installé. La préférence Desktop
`AutomaticallyConnect` est restée désactivée pendant la campagne.

Les artefacts locaux de campagne et de retest étaient sous `.artifacts/quest-ncm/`,
avec les preuves additionnelles sans mode développeur sous `usb-debug-off-20261027/`.
Ces chemins ne garantissent pas la disponibilité des fichiers sur un autre clone.
Aucun nouveau build, test ou essai casque n'a été effectué pour cette consolidation.

## Résultats observés

| Parcours | Résultat final | Portée de l'observation |
| --- | --- | --- |
| USB seul, Wi-Fi Quest coupé, mode développeur désactivé depuis Meta Horizon | Connexion sans nouveau code, Small visible, modification Desktop reçue dans le casque. TLS sur NCM IPv6, scope Desktop 54 lors de l'essai. | Confirmation utilisateur et absence d'appareil accessible par ADB ; aucune relecture de `adb_enabled=0` pendant la coupure revendiquée. |
| IPv6 manuelle | Connexion avec adresse complète et interface Desktop correcte. | Le timeout antérieur utilisait une adresse tronquée ; ce n'était pas une preuve de panne IPv6. |
| Repli USB vers LAN | Premiers TCP LAN environ **4,20 s** après disparition NCM, sans nouveau code. | Délai réseau, pas délai de restauration complète de la scène. |
| Interruption pendant le transfert | Après correction, reprise TCP Wi-Fi en **7,06 s**, publication Small **10 504,53 ms**, synchronisation initiale **1 232,87 ms**, sans Retry/Send/Reconnect ni code. | Durées distinctes ; la restauration complète en moins de 15 s n'est pas démontrée. |
| Redémarrage de HiBoP Quest depuis son menu | Retour automatique de Small sous **30 s** et réglages dans les deux sens confirmés par l'utilisateur ; logs Desktop : publication **9 881,07 ms**, synchronisation **857,76 ms**. | ADB indisponible à cette étape : aucun nouveau PID ni log Quest revendiqué. |

Deux échecs de reprise du transfert avaient précédé la correction de l'admission
et du heartbeat. Les corrections Desktop empêchent le heartbeat de préempter la
découverte pendant une session déconnectée et son délai d'attente. Le verdict
repose sur les retests finaux, sans reclasser les échecs initiaux en succès.
Le contrôle du Player sans ADB avait réussi et les trois variantes ciblées de
reprise avaient été rejouées.

## Limites et interprétation

Le refus/retry NCM reste non exercé. La désactivation du débogage USB seul n'a pas
été testée indépendamment de celle du mode développeur. Les messages OpenXR
caméra/boundary et avertissements DontDestroyOnLoad déjà présents n'ont pas été
déclarés corrigés. Une réassociation manuelle intermédiaire explique la ressaisie
du code à cette étape, sans démontrer une perte spontanée de secret.

Le fonctionnement actuel et la recette pour un autre OS sont décrits dans
[USB et connectivité réseau](../../../Docs/dev/quest-autonomous/usb-and-network-connectivity.md).
La [provenance des anciennes preuves](../retained-results.md#retrouver-une-ancienne-preuve)
permet de consulter le rapport à son ancien chemin dans Git :
`Docs/dev/quest-autonomous/usb/windows-quest3-checklist-2026-10-27.md`.
