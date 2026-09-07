# ADR P12 — coupes canoniques distantes

- **Statut :** ACCEPTED pour P12-A–E et l'architecture ; `FAIL D20 / NO-GO production`
- **Date :** 2026-09-07
- **Base mesurée :** `6c81fcedf93f2ffd2dfdd8c494d4c626a79d5b8c`
- **Décisions héritées :** D05, D08, D09, D12, D20, D23 et ADR P03/P07/P08/P11

## P12-A — scope, cycle de vie et autorité

La coupe est un scope Desktop canonique identifié dans la session par son `cutId` et rattaché par l'application à sa visualisation et son cerveau. Le Quest ne modifie jamais directement la coupe scientifique : il envoie `SetCut` avec `SessionEpoch`, `baseScopeRevision`, `interactionId`, `sequence` et un plan P03 normalisé. Le `CommandGate` Desktop conserve l'autorité D12 et arbitre tout conflit d'acteurs.

Une interaction ordonne seulement ses propres séquences. Une nouvelle interaction peut repartir à 1 ; une interaction déjà supplantée ne peut pas redevenir courante. La révision canonique de coupe reste monotone entre interactions. Fermeture de coupe/cerveau, changement de source ou nouvel epoch invalident commandes, résultats et preload associés.

## P12-B — sortie scientifique finale

Le Desktop est l'unique producteur du résultat canonique. Il publie un `CutRenderResult` composé de la pose P03, de la géométrie éventuelle (positions, normales, UV et indices), de la texture de base éventuelle et d'exactement une image RGBA8 sRGB finale par colonne du manifeste. Les images issues de `CutGenerator.CopyOverlayPixels()` sont déjà composées avec la base ; le shader Quest les affiche directement et ne refait pas de mélange scientifique. Les contours ne voyagent que si le Desktop les matérialise dans ce résultat final.

Géométrie et base sont adressées par hash et réutilisées en mémoire. L'absence d'octets inline signifie « résoudre le hash déjà admis », jamais « recalculer ». Le codec binaire SHA-256 valide schéma, session, bornes, manifeste exact, dimensions, SampleAndHold et espace canonique P03 avant toute préparation visible.

## P12-C — pending, erreur et publication atomique

Le gizmo répond localement et porte l'état pending ; il n'est jamais présenté comme résultat scientifique. Pendant un nouveau calcul, l'ancien résultat Desktop complet reste visible dans son ancien plan. Il n'est ni déplacé vers le gizmo ni mélangé à de nouveaux overlays.

Après résolution de tous les assets et préparation invisible, une seule publication remplace géométrie, matériau et images. Les contrôles stale ont lieu avant préparation et immédiatement avant commit. Erreur, corruption, résultat ancien ou refus D23 gardent l'ancien résultat cohérent et donnent un feedback explicite.

## P12-D — cadence et latest-wins

Le gizmo coalesce à 60 Hz maximum pendant le geste et force toujours l'envoi de la dernière pose au release. Par coupe, le Desktop conserve au plus un travail scientifique actif et un pending latest ; remplacer le pending annule l'actif si possible. L'accès aux générateurs scientifiques mutables est sérialisé.

Le scheduler utilise un numéro interne monotone entre interactions, conserve son high-watermark après une queue idle et refuse les interactions déjà supplantées. Un travail non annulable peut finir, mais son résultat stale n'est ni encodé ni transféré. Seul un outcome `Completed` exactement égal à `(session, cutId, interactionId, sequence)` peut devenir une publication.

## P12-E — autorité après FAIL

Un échec D20 est transmis au propriétaire du dépôt et au responsable scientifique avec mesures, invariants et options. Il n'autorise ni approximation, ni suppression de colonnes, ni paging, ni port Android. PX2 et tout démarrage de `hbp_core` sur Quest exigent une autorisation utilisateur explicite ultérieure.

## Plan stable et preload P11 sous D23

Une publication peut référencer un `PreloadedDynamicTimeline` seulement si session, révision source, cutId, dimensions, mapping revision et ensemble exact de colonnes correspondent à son `CutPlanIdentity`. Chaque colonne attendue possède exactement une timeline de cette coupe. L'admission réserve le pic de remplacement : ressources actives + géométrie/base/images courantes + payload CPU unique + représentation GPU réellement développée.

Le renderer lie le `Texture2DArray` P11 et son index GPU de 4 octets seulement à l'objet timeline de la publication courante. Au commit d'un autre plan, ce binding est désactivé avant tout rendu du nouveau résultat ; il ne redevient actif qu'après admission et préparation du nouveau preload. Un refus ne libère ni ne dégrade le résultat actif.

## Décision de production

Les invariants fonctionnels sont implémentés et les tests ciblés passent, mais la mesure optimiste D20 échoue avant calcul Desktop, transport réel, upload GPU et attente de frame. La production reste donc **NO-GO**. La [fiche de décision P12/D20](../evidence/P12/P12-D20-fail-decision.md) est normative pour la suite.
