# Registre de réalisation

État initial documentaire au 2026-09-07 : aucune des tâches ci-dessous n'a été
implémentée ou testée dans ce chantier. La branche feature/xr-autonomous existe
déjà ; QUEST-001 ne doit pas la recréer.

Ce fichier est la source de statut. Les fiches décrivent le périmètre, les rapports
contiennent les preuves. Ne pas recopier des cases « terminé » dans plusieurs index.

- Implémentation : A_FAIRE, EN_COURS, IMPLEMENTEE, BLOQUEE, NON_APPLICABLE.
- Technique : NON_EXECUTE, PARTIEL, REUSSI, ECHEC, OBSOLETE, NON_REQUIS.
- Manuel : NON_DEMANDE, EN_ATTENTE, VALIDE, REFUSE, OBSOLETE, NON_REQUIS.
- Une qualification manuelle exige un retour daté et référencé dans le rapport.
- NON_APPLICABLE ne remplace pas un échec : réservé à un report/décision explicite.
- Aucune tâche n'est « validée » sur la seule base de son implémentation.

| ID | Implémentation | Technique | Manuel | Rapport / preuve / décision |
| --- | --- | --- | --- | --- |
| <a id="quest-001"></a>[QUEST-001](tasks/QUEST-001.md) | IMPLEMENTEE | REUSSI | VALIDE | [Rapport](reports/QUEST-001.md) · [Preuves](evidence/QUEST-001/manifest.json) ; M1 confirmée par le propriétaire le 2026-09-07 : ouverture de HiBoP 6.1.0 avec le cerveau MNI. |
| <a id="quest-002"></a>[QUEST-002](tasks/QUEST-002.md) | IMPLEMENTEE | REUSSI | VALIDE | [Rapport](reports/QUEST-002.md) · [Preuves](evidence/QUEST-002/manifest.json) ; retour propriétaire le 2026-09-07 : rotation, translation horizontale et zoom OK ; sensibilité verticale excessive préexistante (rapport 5), sans régression introduite par QUEST-002. |
| <a id="quest-002-a"></a>[QUEST-002-A](tasks/QUEST-002-A.md) | IMPLEMENTEE | REUSSI | VALIDE | [Rapport](reports/QUEST-002-A.md) · [Preuves](evidence/QUEST-002-A/manifest.json) ; New sur Windows/Android et Player Windows IL2CPP. M1–M3 validées par le propriétaire le 2026-09-07 à 16:34 +02:00. Simplification DesktopInput ensuite : 81 tests UI/Module3D et compilation des deux cibles réussis ; Player validé conservé. |
| <a id="quest-003"></a>[QUEST-003](tasks/QUEST-003.md) | IMPLEMENTEE | REUSSI | VALIDE | [Rapport](reports/QUEST-003.md), [preuves](evidence/QUEST-003/manifest.json) ; deux builds et tests réussis, M1 Windows et M2 Quest validés par le propriétaire. |
| <a id="quest-004"></a>[QUEST-004](tasks/QUEST-004.md) | IMPLEMENTEE | REUSSI | VALIDE | [Rapport](reports/QUEST-004.md), [preuves](evidence/QUEST-004/manifest.json) ; APK Quest et Player Windows construits, tests et isolation XR vérifiés ; M1/M2 confirmés par le propriétaire le 2026-09-07 à 19:34 +02:00. Messages natifs Meta documentés dans le rapport. |
| <a id="quest-005"></a>[QUEST-005](tasks/QUEST-005.md) | IMPLEMENTEE | REUSSI | NON_REQUIS | [Rapport](reports/QUEST-005.md), [preuves](evidence/QUEST-005/manifest.json) ; contrat HBNA v1 pur, 36 tests EditMode réussis, round-trip bit-exact et validation bornée. |
| <a id="quest-006"></a>[QUEST-006](tasks/QUEST-006.md) | IMPLEMENTEE | REUSSI | VALIDE | [Rapport](reports/QUEST-006.md), [preuves](evidence/QUEST-006/manifest.json) ; 53 tests EditMode réussis, Player Windows IL2CPP et capture MNI réelle ; M1/M2 confirmés visuellement par le propriétaire le 2026-09-08. |
| <a id="quest-007"></a>[QUEST-007](tasks/QUEST-007.md) | IMPLEMENTEE | REUSSI | VALIDE | [Rapport](reports/QUEST-007.md), [preuves](evidence/QUEST-007/manifest.json) ; APK final installé via ADB Wi-Fi, M1/M2 et lisibilité confirmés le 2026-09-08 ; HiBoP arrêté après validation, absence de PID vérifiée (D23). |
| <a id="quest-008"></a>[QUEST-008](tasks/QUEST-008.md) | IMPLEMENTEE | REUSSI | VALIDE | [Rapport](reports/QUEST-008.md), [preuves](evidence/QUEST-008/manifest.json) ; M1–M3 puis M4/M5 (index et frontière passthrough) validés le 2026-09-08 ; 84 tests réussis, APK installé ; ADB conservé après cycle Unity ; HiBoP arrêté après validation, casque éveillé sur chargeur et Wi-Fi préservés (D23/D26). |
| <a id="quest-009"></a>[QUEST-009](tasks/QUEST-009.md) | IMPLEMENTEE | REUSSI | NON_REQUIS | [Rapport](reports/QUEST-009.md), [preuves](evidence/QUEST-009/manifest.json) ; TLS embarqué + identité BouncyCastle (D27), 18 contrôles Player Windows et 9 scénarios Quest physique, MNI identique ; build Android autonome corrigé, sonde arrêtée. |
| <a id="quest-010"></a>[QUEST-010](tasks/QUEST-010.md) | IMPLEMENTEE | REUSSI | NON_REQUIS | [Rapport](reports/QUEST-010.md), [preuves](evidence/QUEST-010/manifest.json) ; publication après staging, reçus idempotents, 27 tests PlayMode, 86 tests EditMode Windows et 69 Android réussis ; transfert Windows IL2CPP–Quest réel, un mesh, conservation hors connexion et libération vérifiées ; HiBoP arrêté. |
| <a id="quest-011"></a>[QUEST-011](tasks/QUEST-011.md) | IMPLEMENTEE | REUSSI | VALIDE | [Rapport](reports/QUEST-011.md), [preuves](evidence/QUEST-011/manifest.json) ; 38 tests PlayMode, 87 EditMode et 23 contrôles de configuration réussis ; M0–M2 validés le 2026-09-08, appairage après réinspection puis transfert Published, progression/manipulation et vues indépendantes confirmées ; HiBoP arrêté, absence de PID vérifiée, ADB Wi-Fi conservé (D23/D26). |
| <a id="quest-012"></a>[QUEST-012](tasks/QUEST-012.md) | IMPLEMENTEE | REUSSI | VALIDE | [Rapport](reports/QUEST-012.md) · [Preuves](evidence/QUEST-012/manifest.json) ; 39 tests PlayMode et 88 EditMode réussis, six envois réels 0,969–1,083 s, hashes/poses conservés, coupure radio 60,02 s. Gestes hors réseau, vues indépendantes et durées validés le 2026-09-08 (D29). J2 qualifié ; HiBoP Quest arrêté, ADB conservé. |
| <a id="quest-013"></a>[QUEST-013](tasks/QUEST-013.md) | IMPLEMENTEE | REUSSI | VALIDE | [Rapport](reports/QUEST-013.md) · [Preuves](evidence/QUEST-013/manifest.json) ; 156 tests réussis, builds Windows/Quest et capture MNI réelle avec huit contacts vérifiés ; réception TLS du payload Player réussie. M1 validée et tâche clôturée explicitement par le propriétaire le 2026-09-09 après clarification ; rendu au casque réservé à QUEST-014. |
| <a id="quest-014"></a>[QUEST-014](tasks/QUEST-014.md) | IMPLEMENTEE | REUSSI | VALIDE | [Rapport](reports/QUEST-014.md) · [Preuves](evidence/QUEST-014/manifest.json) ; 157 tests réussis, builds Windows/Quest et diagnostic physique réussis ; M1/M2 validées, retard en mouvement brusque reproduit et corrigé par ordre LateUpdate explicite ; correctif validé au casque par le propriétaire le 2026-09-09 (« Tout est bon ! »). |
| <a id="quest-015"></a>[QUEST-015](tasks/QUEST-015.md) | IMPLEMENTEE | REUSSI | NON_REQUIS | [Rapport](reports/QUEST-015.md) · [Preuves CI](evidence/QUEST-015/ci-update.json) ; trois workflows réussis, dix artefacts GitHub installés après release 0.4.0, Android reproductible et 226 exports, 9 contrôles Android CI, 15 tests natifs par OS Desktop, 25 tests Unity, builds Windows/Quest et hashes vérifiés. Runtime casque réservé à QUEST-016. |
| <a id="quest-016"></a>[QUEST-016](tasks/QUEST-016.md) | IMPLEMENTEE | REUSSI | NON_REQUIS | [Rapport](reports/QUEST-016.md) · [Preuves](evidence/QUEST-016/manifest.json) ; Quest 3 physique, trois séries IL2CPP, 300 cycles et 1 800 handles libérés, callbacks/erreurs contrôlées, mémoire relevée, APK et hash installé vérifiés, 25 tests Unity ; HiBoP arrêté. |
| <a id="quest-017"></a>[QUEST-017](tasks/QUEST-017.md) | IMPLEMENTEE | REUSSI | VALIDE | [Rapport](reports/QUEST-017.md) · [Preuves](evidence/QUEST-017/manifest.json) ; HBNA v3, 136 tests EditMode et 42 PlayMode réussis, builds Windows/Quest et trois cycles physiques de reconstruction ; masques natifs relus, ressources libérées, HiBoP arrêté. M1 documentaire validée par le propriétaire le 2026-09-09 (« M1 OK »), après clarification des comportements attendus. |
| <a id="quest-018"></a>[QUEST-018](tasks/QUEST-018.md) | IMPLEMENTEE | REUSSI | VALIDE | [Rapport](reports/QUEST-018.md) · [Preuves](evidence/QUEST-018/manifest.json) ; wrappers natifs directs et préparation commune à toutes les colonnes, 57 tests EditMode et 45 PlayMode réussis, Player Windows IL2CPP et cycle 15 → 25 → 15 mm vérifiés sur 69 104 sommets ; référence PNG restaurée à l’identique, M1/M2 validées par le propriétaire le 2026-09-09 (« Je valide M1 et M2 »). |
| <a id="quest-019"></a>[QUEST-019](tasks/QUEST-019.md) | IMPLEMENTEE | REUSSI | VALIDE | [Rapport](reports/QUEST-019.md) · [Preuves](evidence/QUEST-019/manifest.json) ; densité native locale, 132 EditMode et 45 PlayMode distincts réussis, builds Windows/Quest, 18 comparaisons complètes par plateforme ; parité acceptée pour ce banc et ces versions (D30), réception TLS et trois recalculs durant 60,01 s hors radio. M1/M2 validées le 2026-09-09, coupure manuelle 60,02 s ; HiBoP arrêté, absence de PID vérifiée, ADB conservé. |
| <a id="quest-020"></a>[QUEST-020](tasks/QUEST-020.md) | IMPLEMENTEE | REUSSI | VALIDE | [Rapport](reports/QUEST-020.md) · [Preuves](evidence/QUEST-020/manifest.json) ; instant iEEG HBNA v4, unités/masques/provenance et plages préparées conservés, sémantique surface/sites explicite ; 150 EditMode et 46 PlayMode distincts réussis, captures Player à 0/250 ms et réception TLS vérifiées, builds Windows/Quest et APK installé. M1 résumé de transfert validée le 2026-09-09 par retour propriétaire ; HiBoP déjà arrêté, absence de PID vérifiée et ADB Wi-Fi conservé. Aucun rendu iEEG Quest avant QUEST-023. |
| <a id="quest-021"></a>[QUEST-021](tasks/QUEST-021.md) | IMPLEMENTEE | REUSSI | VALIDE | [Rapport](reports/QUEST-021.md), [preuves](evidence/QUEST-021/manifest.json) ; 70 EditMode + 45 PlayMode, Player Windows : 18 comparaisons exactes ; M1 Desktop validée par le propriétaire le 2026-09-09 ; aucun casque requis, métriques conservées à sa demande. |
| <a id="quest-022"></a>[QUEST-022](tasks/QUEST-022.md) | A_FAIRE | NON_EXECUTE | NON_DEMANDE | — |
| <a id="quest-023"></a>[QUEST-023](tasks/QUEST-023.md) | A_FAIRE | NON_EXECUTE | NON_DEMANDE | — |
| <a id="quest-024"></a>[QUEST-024](tasks/QUEST-024.md) | A_FAIRE | NON_EXECUTE | NON_DEMANDE | — |
| <a id="quest-025"></a>[QUEST-025](tasks/QUEST-025.md) | A_FAIRE | NON_EXECUTE | NON_DEMANDE | — |
| <a id="quest-026"></a>[QUEST-026](tasks/QUEST-026.md) | A_FAIRE | NON_EXECUTE | NON_DEMANDE | — |
| <a id="quest-027"></a>[QUEST-027](tasks/QUEST-027.md) | A_FAIRE | NON_EXECUTE | NON_DEMANDE | — |
| <a id="quest-028"></a>[QUEST-028](tasks/QUEST-028.md) | A_FAIRE | NON_EXECUTE | NON_DEMANDE | — |
| <a id="quest-029"></a>[QUEST-029](tasks/QUEST-029.md) | A_FAIRE | NON_EXECUTE | NON_DEMANDE | — |
| <a id="quest-030"></a>[QUEST-030](tasks/QUEST-030.md) | A_FAIRE | NON_EXECUTE | NON_DEMANDE | D28 ; fiche rédigée, exécution après les jalons J0–J7. |

## Décisions bloquantes et retour utilisateur

Aucune nouvelle décision d'implémentation prise par la rédaction de ces fiches.
Au lancement d'une tâche, consigner ici un pointeur vers le rapport et Dxx si
une décision en bloque réellement la suite. Le choix Linux reste futur.
