# Préférences et atlas de session

Jalon dédié, distinct de T15. Desktop reste maître de toutes les préférences, y compris sans scène partagée. Chaque sauvegarde réussie capture un snapshot complet ; le Quest le valide sans effets de bord puis applique sur Unity, conserve l’objet et ses listeners, notifie une fois et ne sauvegarde pas sur disque. Les changements de normalisation sont refusés tant qu’une incarnation partagée est ouverte ; le cycle complet sera traité dans T17.

## Transport et durée de vie

Le propriétaire Desktop est `QuestManager` / `DesktopSessionPreferences` ; le récepteur Quest est détenu par `QuestAnatomySession`. Commande pairing authentifiée 15, TLS épinglé et credential existant. Framing version 1, corps maximal 1 MiB, texte résultat 1024 caractères, atlas 128 caractères. Identités : contexte de pairing, génération de connexion, opération UUID et révision croissante des sauvegardes. ACK après application. Cache Quest : 64 opérations, refus de réutilisation d’un UUID avec un autre contenu. La file Desktop est bornée à 64 sauvegardes ; un dépassement abandonne la connexion avec un résultat explicite. Les opérations en cours sont abandonnées à la déconnexion, sans replay à la reconnexion. Une inspection authentifiée permet de relire le snapshot appliqué et la présence d’une scène partagée.

## Atlas installés

`AtlasResources` décrit tous les adaptateurs existants, y compris les localizers. Les empreintes SHA-256 couvrent les fichiers requis, leurs chemins et les companions NIfTI. Aucun fichier n’est transféré ici. `SessionAtlasCatalog` autorise les dépendances seulement après vérification des deux contenus chargés. Les ressources propres aux scènes et identités de livraison restent immuables. Mesh/MRI ne sont pas invalidés par un nouvel atlas global.

Les chargements sont attendables, partagés par atlas et ne publient un candidat qu’après préparation native complète et vérification du contenu. Une annulation attend le natif avant libération. Load conserve les succès partiels et permet une nouvelle tentative Quest. Unload réserve d’abord les deux côtés, interdit de nouveaux usages puis décharge. Réservations distantes expirent après 30 s ; usages de scènes et travaux natifs empêchent la libération. L’auto-load ne s’exécute qu’au démarrage Desktop et à l’installation d’une nouvelle session Quest. Les boutons agissent immédiatement ; Cancel n’annule pas ces actions.

Le build Quest exclut par défaut les localizers. Un asset `QuestStandardDataSettings` à `Assets/Settings/QuestStandardData.asset` permet de les inclure ; leur disponibilité runtime dépend uniquement des fichiers installés et vérifiés.

## Vérification

Validation Unity MCP, HiBoP 6000.5.2f1, 6 octobre 2026 :

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
