# Préférences et atlas de session

Jalon dédié, distinct de T15. Desktop reste maître de toutes les préférences, y compris sans scène partagée. Chaque sauvegarde réussie capture un snapshot complet ; le Quest le valide sans effets de bord puis applique sur Unity, conserve l’objet et ses listeners, notifie une fois et ne sauvegarde pas sur disque. Les changements de normalisation sont refusés tant qu’une incarnation partagée est ouverte ; le cycle complet sera traité dans T17.

## Transport et durée de vie

Le propriétaire Desktop est `QuestManager` / `DesktopSessionPreferences` ; le récepteur Quest est détenu par `QuestAnatomySession`. Commande pairing authentifiée 15, TLS épinglé et credential existant. Framing version 1, corps maximal 1 MiB, texte résultat 1024 caractères, atlas 128 caractères. Identités : contexte de pairing, génération de connexion, opération UUID et révision croissante des sauvegardes. ACK après application. Cache Quest : 64 opérations, refus de réutilisation d’un UUID avec un autre contenu. La file Desktop est bornée à 64 sauvegardes ; un dépassement abandonne la connexion avec un résultat explicite. Les opérations en cours sont abandonnées à la déconnexion, sans replay à la reconnexion. Une inspection authentifiée permet de relire le snapshot appliqué et la présence d’une scène partagée.

## Atlas installés

`AtlasResources` décrit tous les adaptateurs existants, y compris les localizers. Il ne calcule plus d’empreinte globale des fichiers d’un atlas ou de MNI. Les opérations de provenance de `FMRI` et `Volume` (source, masque et companions) restent systématiques lors d’un chargement effectif. Aucun fichier d’atlas n’est transféré ici. `SessionAtlasCatalog` autorise les dépendances lorsque les deux appareils déclarent l’atlas chargé ; les références utilisent les identifiants, noms et indices, sans comparer les contenus installés. Leur cohérence relève de l’utilisateur. Les contrôles d’intégrité des données utilisateur transférées et du transport restent actifs. Mesh/MRI ne sont pas invalidés par un nouvel atlas global.

Les chargements restent séquencés, attendables, partagés par atlas et ne publient un candidat qu’après préparation native complète. Un atlas déjà chargé est réutilisé sans inspection du disque ; Unload puis Load permet de prendre en compte des fichiers modifiés. Une annulation attend le natif avant libération. Load conserve les succès partiels et permet une nouvelle tentative Quest. Unload réserve d’abord les deux côtés, interdit de nouveaux usages puis décharge. Réservations distantes expirent après 30 s ; usages de scènes et travaux natifs empêchent la libération. L’auto-load ne s’exécute qu’au démarrage Desktop et à l’installation d’une nouvelle session Quest. Les boutons agissent immédiatement ; Cancel n’annule pas ces actions.

Le build Quest exclut par défaut les localizers. Un asset `QuestStandardDataSettings` à `Assets/Settings/QuestStandardData.asset` permet de les inclure ; leur disponibilité runtime dépend des fichiers installés et du succès du chargement natif, sans exigence de manifeste. Le manifeste Android existant reste réservé à l’installation et aux mises à jour des ressources embarquées ; il ne conditionne pas leur synchronisation.

## Vérification

Contrôles du 9 octobre 2026 : compilation Unity sans erreur après retrait de l’option `captureProvenance`. Les opérations de provenance de `FMRI` et `Volume` sont de nouveau systématiques. Contrôle statique des assemblies réussi : 44 assemblies HBP, 158 dépendances directes.

- EditMode via Unity MCP : 47/47 (`SessionPreferencesTests`, `PairingBackgroundPreparationTests`, `TransferOptimizationTests`, `PreparedSceneArchiveTests`).
- PlayMode via Unity MCP : neuf cas validés, couvrant `SessionAtlasLoadingTests`, `SessionPreferencesOwnerTests`, le test isolé `InstalledAtlasReferencesIgnoreContentHashesAndUnselectedLocalizerBlocs` et les deux variantes de `CompleteScene_RestoresNativeModalitiesAndIndependentColumns_ThenRecaptures`. Les tests vérifient la réutilisation sans lecture disque, les contenus installés différents, les ressources manquantes, la provenance et la restitution des données utilisateur.
- Le scénario supplémentaire `S2_ReplaysCorrelationsAcrossDeliveredSixModalityScenesWithoutReplacingQuestPresentation` échoue sur l’assertion existante « Different mesh vertices under the same name must not bind » : aucune exception n’est levée. Cet échec se reproduit après retrait des ajouts de test d’atlas dans ce scénario ; le contrôle des meshes concerné n’a pas été modifié dans cette tâche.
- Console sans erreur après la dernière validation isolée des atlas. Un premier suivi MCP PlayMode a perdu son état lors du changement de domaine ; les résultats XML Unity correspondants ont aussi été inspectés. Traces ignorées : `.test-results/atlas-loading/`. Le gain de temps de chargement reste à mesurer.

Validation historique Unity MCP, HiBoP 6000.5.2f1, 6 octobre 2026 (avant suppression des comparaisons de contenu) :

- EditMode : 78/78 (`V2SceneMutationBoundaryTests`, `SessionPreferencesTests`), puis 27/27 pour les snapshots, le transport TLS de session, le pairing et son handshake global. Les 7 tests de snapshots sont inclus dans les deux runs.
- PlayMode : 3/3 (`SessionPreferencesUiTests`, `SessionAtlasLoadingTests`, `SessionPreferencesOwnerTests`). Le test localizer utilise un NIfTI installé dans un répertoire temporaire et exécute le chargement natif, le double Load, l’annulation d’un consommateur et le refus de libération pendant un usage.
- Console sans erreur après ces runs. Contrôle statique : 44 assemblies HBP, 154 dépendances directes, aucun cycle ni dépendance Core vers un autre HBP.

Recette exécutée sur Quest 3, sans port du casque, avec le build Unity installé par mise à jour sans désinstallation ni perte de données :

- Deux sauvegardes rapides sans scène : dernier snapshot appliqué, ancienne révision refusée. Modifier auto-load ne charge rien immédiatement.
- IBC chargé et déchargé sur les deux appareils ; une rétention de travail refuse le déchargement avant libération.
- DiFuMo 64 chargé après publication d’une scène MNI : sélection mesh/MRI et affichage atlas synchronisés. Déchargement refusé pendant l’affichage, puis réussi après désactivation. Transport V2 connecté, files et trames fiables entièrement acquittées.
- Normalisation bloquée dans le brouillon de la fenêtre, sans modification globale ni sauvegarde. Un snapshot envoyé directement au Quest est également refusé sans application partielle.
- Actions manuelles sans sauvegarde et annulation de la fenêtre : chargement conservé, préférence du brouillon abandonnée.
- Fichier IBC temporairement absent : succès Desktop conservé, erreur Quest explicite, nouvelle tentative réussie après restauration. Contenu installé différent : utilisation partagée refusée. Tous les fichiers de recette ont été restaurés.
- Le fichier de préférences Quest conserve la même empreinte pendant les sauvegardes et toute la recette, après l’initialisation normale de l’application. Le fichier Desktop original est également préservé.
- Dernier contrôle Desktop hors connexion : une session terminée ne bloque pas les changements sans incarnation partagée ; une incarnation distante connue conserve la protection.

Traces locales ignorées par Git : `.test-results/session-preferences/device-*.json`, `desktop-offline-normalization.json`, résultats Unity et captures de la fenêtre. APK : `.artifacts/session-preferences/HiBoP.Quest.apk`, validation des neuf bibliothèques ARM64 dans `apk-content.json`. Signature exclusivement issue de la clé configurée dans Unity ; certificat SHA-256 `d5dcf67868bd08a3c7028a8495cd39550d4afaa7936286989d0e42253b081acf`, identique à la version déjà installée. Le build aboutit malgré un diagnostic Unity de texture multisample liée à un sampler non multisample ; aucune erreur console pendant la recette fonctionnelle.

Limites de validation : interruption pendant une opération et absence de replay couvertes par les tests automatisés de transport/propriétaire, sans coupure réseau physique pendant un chargement natif. Le test natif temporaire localizer ne constitue pas une validation d’un APK embarquant les localizers complets. Le rechargement Desktop de scènes uniquement locales conserve son chemin existant ; son cycle complet avec scènes partagées reste le périmètre futur de T17.
