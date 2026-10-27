# Validation de la migration NCM/mDNS — 27 octobre 2026

Ces résultats sont des preuves de développement. La matrice matérielle de
`qualification.md` reste obligatoire avant de modifier `qualification.json`.
Les trois plateformes restent bloquées à la publication à ce stade.

## Environnement

- Unity 6000.5.2f1, poste Windows, Desktop Windows x86-64 IL2CPP.
- Quest 3, Android API 34, build Android `UP1A.231005.007.A1`, incrément
  `52433670048800520`. Le numéro commercial Horizon OS reste à relever dans le casque.
- Variante mDNS reproductible 0.38.0, SHA-256 du binaire adapté :
  `60c1dc4c040ba79e37b1a5f3b4a0b1100edc6092def90a9f7acebebbeb48f909`.

## Contrôles exécutés

- Première sélection EditMode : 36/36 réussis (appairage, réseau et prefab USB).
- Après les derniers changements du cache : 8/8 `QuestNetworkTests` réussis,
  incluant réception multicast réelle, réponses DNS séparées, scopes locaux,
  expiration/retrait, filtrage des services étrangers, TLS IPv6 et budget partagé
  de 72 admissions entre listeners IPv4/IPv6.
- Dernier PlayMode ciblé du gestionnaire : 4/4 réussis, dont regroupement USB/Wi-Fi,
  conservation du secret mémorisé, interruption d'un transfert manuel avec ouverture
  automatique désactivée et annulation explicite. La coupure conserve l'intention
  de reprise ; l'annulation la suspend.
- Suite de configuration UI élargie : 41/42 réussis. Échec de
  `SceneControls_UseNestedPrefabsAndSerializedOwners` avec `NullReferenceException` ;
  confirmé par relance isolée. Le test existant cherche le champ sérialisé `input`
  sur `QuestDisplayPanel` (ligne 56), alors que ce composant utilise `projection`.
  Ce composant et le prefab concerné ne sont pas modifiés par cette migration.
  Le nouveau test du prefab USB réussit.
- Premier Player Windows IL2CPP de développement construit : 0 erreur,
  73 avertissements, 241,83 s. Contrôle d'absence d'ADB réussi sur son dossier.
- Diagnostic TLS/appairage exécuté dans ce Player : 8 contrôles réussis,
  sortie du processus 0.
- Dernier Player Windows **release** IL2CPP, après la correction de reprise :
  construction réussie en 78,46 s, 0 erreur, 47 avertissements. Ces avertissements
  de build ne sont pas assimilés à une console sans avertissements. Contrôle
  d'absence d'ADB réussi ; les scénarios de reprise matériels restent à exécuter
  sur ce Player final.
- Contrôle statique des dépendances : 44 assemblies, 163 références, aucune
  violation. Compilation Java du bridge contre Android API 36 réussie.
- Reconstruction mDNS avec .NET SDK 10.0.201 : les quatre DLL reproduisent
  exactement les empreintes du verrou. Nouvelle exécution des 8 tests réseau après
  inclusion des indices IPv4/IPv6 dans les générations d'interface : 8/8 réussis,
  aucune erreur console Unity observée.
- APK Quest release IL2CPP construit : 0 erreur, 75 avertissements, 345,91 s.
  APK de 341 117 908 octets, signature v2 vérifiée, 9 bibliothèques ARM64 conformes
  aux contrôles de packaging existants, permissions réseau vérifiées, aucun ADB.
  Mise à jour en place avec `adb install -r` réussie, sans effacement des données.
- Player Quest lancé sur le casque : OpenXR/Vulkan prêt, aucune erreur réseau
  observée dans les logs examinés. Aucune nouvelle demande de consentement n'est
  apparue sur cette connexion ; le parcours refus/nouvelle tentative reste à tester.

## Premier essai matériel Windows / Quest 3

Le Player Windows IL2CPP de développement a découvert le service par mDNS sur le
chemin USB, puis obtenu une description TLS dont l'empreinte correspond à l'annonce
(2 contrôles, sortie 0). L'essai a été répété avec le Wi-Fi Quest désactivé : même
résultat. Le Wi-Fi a été rétabli dans un bloc `finally` après cet essai.

- Windows : `Ethernet 5`, `UsbNcm Host Device`, indice local 8,
  `fe80::9b8a:8c14:f462:dde6%8/64`.
- Quest : `usb0`, indice 18, `fe80::702:9531:a0f2:be2b/64`.
- La liaison TCP utilise donc IPv6 link-local, avec le scope Windows ; l'indice
  Quest n'est pas réutilisé côté Desktop.
- Les exécutables Desktop testés ne contiennent pas ADB. ADB reste actif sur le
  poste pour installation et lecture des logs : débogage USB désactivé et mode
  développeur désactivé **pas encore testés**.
- Contrôle des archives : rejet vérifié d'un binaire ADB avec séparateurs ZIP
  Windows (`\\`) ; les séparateurs `/` restent pris en charge.

Les essais complémentaires de reprise réelle et de scène sont consignés dans
[recovery-windows-quest3-2026-10-27.md](recovery-windows-quest3-2026-10-27.md).
Ils confirment le repli sans code et une scène conservée avec synchronisation
bidirectionnelle reprise. Le chronométrage complet de fin de restauration, les
autres parcours de consentement restent à qualifier. Quest 3S est hors périmètre
de cette qualification à la demande de l'utilisateur.

Les rapports locaux sont conservés dans `.artifacts/quest-ncm/` (dossier ignoré).
Les builds, tests et vérifications de dépendances ne constituent pas une
qualification matérielle Mac/Linux, ni celle du Quest 3S.
