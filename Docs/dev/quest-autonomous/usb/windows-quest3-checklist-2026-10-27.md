# Campagne finale Windows / Quest 3 — 27 octobre 2026

Périmètre : Quest 3 uniquement. Quest 3S exclu à la demande de l'utilisateur.
Campagne manuelle assistée : pas de nouveau build avant collecte des échecs ;
corrections regroupées ensuite, puis relance des seuls scénarios en échec.

Builds utilisés :

- Desktop : `.artifacts/quest-ncm/sleep-null-reference/WindowsRelease/HiBoP.6.2.0.win64/`.
- Quest : APK existant `.artifacts/quest-ncm/Android/HiBoP.Quest.apk`, déjà installé.
- Preuves : `.artifacts/quest-ncm/windows-quest3-qualification-20261027/`.

## Tests déjà documentés

- [x] NCM et TLS sur USB seul, Wi-Fi Quest coupé.
- [x] Repli USB vers Wi-Fi sans code, au repos puis avec une scène publiée.
- [x] Conservation de la scène et reprise bidirectionnelle après la grâce de 500 ms.
- [x] Correction de la référence nulle d'annulation TCP, tests et Player IL2CPP.

Voir `validation-2026-10-27.md`, `recovery-windows-quest3-2026-10-27.md` et
`sleep-null-reference-2026-10-27.md` pour les limites et les preuves antérieures.

## Parcours à effectuer dans les builds existants

- [x] Connexion automatique désactivée ; déconnexion explicite respectée ;
  connexion manuelle USB sans code.
- [x] Même option désactivée : repli d'une session ouverte vers Wi-Fi.
- [x] Retour du câble sans migration de la connexion Wi-Fi active.
- [x] Veille/réveil, sans code.
- [x] Redémarrage de l'application Quest, sans code.
- [x] Redémarrage complet du Quest, sans code.
- [x] Redémarrage Desktop et ordres de lancement, connexion sans code.
- [x] Retrait du câble pendant transfert ; reprise et scène utilisable.
- [x] Annulation explicite : arrêt des tentatives non sollicitées.
- [ ] Consentement USB/refus/retry, selon les possibilités réellement exposées
  par Horizon OS sur cet appareil ; relever tout cas impossible à provoquer.
- [x] Communication USB seule sans serveur ADB.
- [x] Essai sans débogage USB, via désactivation du mode développeur sur le
  téléphone ; conserver la distinction avec le simple arrêt du serveur ADB.
- [x] Manual IP : IPv4 réseau local et IPv6 USB avec sélection d'interface locale.
- [ ] Relevé de version Horizon OS et revue finale des logs.

Les cas non exécutés ou non reproductibles doivent rester identifiés, pas être
cochés implicitement. Le verrou de release n'est levé qu'après examen des résultats.

## Résultats et corrections regroupées

Collecte initiale terminée ; corrections regroupées en cours. Aucun nouveau build
n'a été effectué pendant cette collecte.

1. Préférence persistée `AutomaticallyConnect=false` vérifiée. L'utilisateur
   confirme l'absence de reconnexion pendant 10 s après Disconnect, puis une
   connexion manuelle USB sans code. Les connexions observées utilisent le scope
   Windows USB 8 et l'ouverture de session est confirmée dans les logs.
2. **ÉCHEC observé** : même option désactivée, retrait USB à 15:10:28,533 (Paris).
   L'utilisateur croit la connexion reprise, mais aucune connexion TCP réseau
   local n'est observée et les logs répètent « The selected local IPv6 interface
   is unavailable » plus de 15 s après le retrait. Ne pas assimiler l'état affiché
   à une connexion fonctionnelle. Reconnexion manuelle demandée pour poursuivre.
3. La reconnexion manuelle sans câble ouvre une session réseau sans nouveau code,
   confirmée par les logs et les connexions TCP. Après réapparition NCM à
   15:14:13,366 (Paris), les échanges restent sur l'adresse réseau local via
   l'interface Windows 4 : pas de migration spontanée vers USB.
4. Veille/réveil : l'utilisateur rapporte une reprise probable ; les logs
   confirment la suspension/reprise XR, des timeouts pendant la veille puis une
   reprise des globals et une ouverture de session (116,74 ms), sans code.
   Aucune référence nulle observée.
5. **ÉCHEC observé pendant transfert** : scène « Small », retrait USB à
   15:20:20,639 (Paris). Une IOException de connexion réinitialisée apparaît
   pendant BlockDelivery.SendAsync, ouvre la fenêtre de bug report, puis le
   bouton devient Retry. Aucune reprise spontanée de la scène n'est observée ;
   les tentatives répètent ensuite l'indisponibilité de l'ancien scope IPv6 USB.
   Une reprise manuelle par Retry est demandée uniquement pour continuer les
   scénarios indépendants ; elle ne transforme pas cet échec en réussite.
6. Retry manuel : publication et synchronisation de Small réussies sans code
   (transfert `2b51bf11a9ee4e46972e87cdcc7567b8`, réception Quest à 15:23:42,247).
7. **ÉCHEC de restauration après redémarrage Quest** : processus Quest redémarré
   (PID 20413 vers 22944), reprise des globals et ouverture de session sans code.
   La scène ne revient pas et Desktop ouvre une exception :
   « The interrupted Quest transfer needs an explicit retry », dans
   PrepareSceneOfferAsync, QuestManager.SceneLifecycle.cs:317. Cet essai suit
   l'interruption puis le Retry réussi ci-dessus ; conserver cette séquence pour
   la régression, sans attribuer le défaut à tous les redémarrages possibles.
8. Desktop fermé sans sauvegarde puis même Player relancé. Avec connexion
   automatique désactivée : aucune connexion spontanée au démarrage. Tentative
   manuelle avec Quest en veille, annulation par l'utilisateur, puis réveil :
   l'annulation est respectée, sans nouvelle tentative observée. La mention
   Paired côté Quest reste une association mémorisée, pas une connexion active.
9. Après ce redémarrage Desktop, une nouvelle commande Connect ouvre la session
   USB sans code ; confirmation utilisateur et logs d'ouverture de session.
10. USB seul sans serveur ADB : zéro processus adb vérifié sur Windows,
    utilisateur confirmant Wi-Fi Quest désactivé. Les échanges TLS et les TCP
    IPv6 scope USB 8 restent actifs. Ceci ne prouve pas encore que le réglage
    de débogage USB Android est désactivé.
11. Redémarrage complet Quest : utilisateur confirme la reprise sans code après
    ouverture de HiBoP. Logs d'ouverture de session retrouvés ; aucune demande
    système USB présentée. Le parcours refus/retry n'est donc pas exercé sur cet
    état d'autorisation du casque et n'est pas coché implicitement.
12. **ÉCHEC Manual IPv6** : IPv4 `192.168.1.18` fonctionne sans code. L'adresse
    `fe80::702:9531:a0f2:be2b` avec sélection d'Ethernet 5 ne connecte pas (deux
    essais utilisateur). Même résultat avec `%8` explicite. Adresse USB actuelle
    et index local 8 vérifiés ; une ouverture TCP directe sur 45871 aboutit.
    Le sélecteur seul ne suffit donc pas à expliquer le défaut. Logs conservés
    dans `test10-player.log` et `test10-quest.log` ; retester le parcours applicatif.
13. Version lue par l'utilisateur : « 2.7 », identification commerciale à
    confirmer. Version Android observée : 14, display `UP1A.231005.007.A1`,
    incrémental `52433670048800520`, fingerprint
    `oculus/eureka/eureka:14/UP1A.231005.007.A1/52433670048800520:user/abl_signing_keys:release,amss_signing_keys:release,release-keys`.
    Option de débogage USB non trouvée : essai désactivé non exécuté.

Les reprises Wi-Fi avec connexion automatique désactivée, l'interruption de
transfert, la restauration après redémarrage de l'application Quest et Manual IPv6
restent à retester après correction. Les autres scénarios ne sont pas à rejouer
systématiquement. Aucune qualification finale Windows n'est encore déclarée.

## Passe de corrections et retest

- Une interface IPv6 disparue produit désormais une erreur de transport et libère
  la connexion pour permettre la sélection d'un autre endpoint du même casque.
- Une interruption réseau pendant Display conserve une reprise automatique,
  y compris après le nettoyage de la présentation. Annulation et erreur de données
  conservent leur traitement explicite.
- Une publication acquittée mais absente après redémarrage Quest est retirée par
  son identité exacte et acquittement avant remplacement. Un refus conserve
  les informations locales de récupération.
- Manual IP sélectionne explicitement l'adresse à la validation et expose les
  échecs de connexion dans une fenêtre ; l'origine de l'échec IPv6 matériel reste
  à confirmer pendant le retest.

Vérification : 41 tests PlayMode ciblés et 12 tests réseau EditMode réussis ;
aucune erreur console. Le premier lancement EditMode n'avait pas démarré avant
son timeout d'initialisation ; la relance a effectivement exécuté les 12 tests.
Contrôle d'assemblies réussi : 44 assemblies, 163 dépendances directes.
Revue indépendante ciblée sans blocage supplémentaire identifié.

Un seul nouveau build Windows release IL2CPP : succès, 0 erreur, 50 avertissements,
244,07 s. Avertissements conservés dans `build-warnings.json` des preuves, notamment
les fichiers XR Simulation temporaires déjà présents et des avertissements C# /
sérialisation existants. Contrôle sans ADB réussi sur la sortie ; aucune archive
distribuable produite par cette commande. APK installé conservé.
Les champs générés BuildInfo et XR du profil de rendu ont été rétablis à leurs
valeurs observées avant le build.

Nouveau Player : `.artifacts/quest-ncm/recovery-fixes-20261027/WindowsRelease/HiBoP.6.2.0.win64/`.
Preuves et journal de retest : `.artifacts/quest-ncm/recovery-fixes-20261027/`.

1. Manual IPv6 avec zone numérique : premier timeout expliqué par le journal
   indiquant `fe80::702:9531:a0f2%8` (groupe `:be2b` manquant). Une fois l'adresse
   complète placée directement dans le presse-papiers Windows et collée dans
   HiBoP, l'utilisateur confirme une connexion parfaite ; reprise des globals,
   ouverture de session et TCP USB scope 8 vérifiés. Aucune nouvelle association.
   Le timeout avec l'adresse tronquée ne constitue pas une panne IPv6 HiBoP.
2. Sélection d'Ethernet 5 avec l'adresse complète sans zone : connexion confirmée
   par l'utilisateur et les logs. L'utilisateur indique avoir utilisé Forget
   association entre-temps et saisi le code : nouvelle association de référence,
   pas une perte spontanée de secret pendant ce test.
3. Repli, préférence `AutomaticallyConnect=false` revérifiée : disparition NCM
   à 16:06:54,558 (Paris), premiers TCP LAN à 16:06:58,758, soit environ 4,20 s.
   Le candidat IPv6 global LAN est sélectionné automatiquement, puis reprise des
   globals (50 ms), ouverture de session (83,20 ms) et échanges ultérieurs.
   Aucun nouveau code signalé. L'utilisateur juge la reprise probable ; la preuve
   de connexion fonctionnelle provient ici des TCP et logs d'ouverture de session.
4. **Second échec de transfert interrompu** : Small, transfert
   `d60202d72a8146549d583a8766344b45`, retrait NCM à 16:10:39,778 (Paris).
   Plus de fenêtre d'erreur, mais pas de reprise spontanée sous 30 s. Les essais
   reviennent ensuite en boucle sur l'ancienne adresse USB après le délai de
   reconnexion : heartbeat autorisé malgré une session déconnectée, pouvant
   préempter le cooldown de découverte à chaque échec. Les preuves sont conservées
   dans `transfer-failure-*` du dossier de retest. Le casque est rebranché pour
   relever ses logs, sans Retry manuel.

Seconde correction ciblée : heartbeat uniquement avec une session connectée ;
la découverte possède les tentatives après perte de session et leur cooldown.
Le cas Manual IP reste renouvelable même sans annonces mDNS, sous le même cooldown.

Vérification de cette seconde correction : les 3 variantes du test de reprise ont
été rejouées et réussies, avec vérification que le heartbeat ne reprend pas
l'ancienne adresse pendant l'attente de découverte. Console sans erreur.
Build Windows release IL2CPP `WindowsRelease2` : succès, 0 erreur,
45 avertissements, 174,21 s ; contrôle sans ADB réussi. Aucun nouveau build Quest.

5. **Retest du transfert réussi** dans WindowsRelease2 : transfert initial
   `858cd811e6e84f5d88ffa348145b3583`, coupure USB à 16:21:02,368 (Paris),
   premiers TCP Wi-Fi à 16:21:09,431, environ 7,06 s plus tard. Après un essai
   d'adresse USB périmée, la sélection atteint bien le LAN au lieu de boucler sur
   le heartbeat. Une nouvelle publication
   `480504393fd64823b7e19685b0cd7e91` aboutit automatiquement (publication 10 504,53 ms,
   synchronisation initiale 1 232,87 ms). L'utilisateur confirme le fonctionnement
   sans Retry/Send/Reconnect ni code, câble toujours débranché. La durée de reprise
   TCP et celle de restauration de la scène sont distinguées.
6. **Restauration après redémarrage de HiBoP Quest réussie** : l'utilisateur quitte
   complètement l'application depuis le menu du casque, la relance et confirme
   le retour automatique de Small sous 30 s, sans commande Windows ni code.
   Nouvelle publication `2c24ed4c36694340bf0afebd401e86ef` (9 881,07 ms),
   synchronisation initiale 857,76 ms. L'utilisateur confirme ensuite les échanges
   de réglages dans les deux sens.
7. La liaison ADB n'était pas disponible au moment du redémarrage final malgré
   le câble signalé branché. La commande de diagnostic en attente a été annulée
   avant toute action ; le redémarrage a été manuel. Aucune nouvelle trace Quest
   ni changement de PID n'est donc revendiqué pour cette dernière étape. Preuves :
   confirmation utilisateur, journaux Desktop et connexions LAN. La vérification
   finale après casque posé ne retrouvait toujours aucun appareil ADB ; elle ne
   constitue pas une nouvelle qualification du branchement USB au premier plan.

## État à la fin de la campagne

Les quatre scénarios ayant échoué sont résolus dans les essais effectués :
Manual IPv6 (adresse complète), repli avec connexion automatique désactivée,
interruption de transfert avec reprise automatique, restauration après relance
Quest et synchronisation bidirectionnelle.

La préférence Desktop `AutomaticallyConnect` reste désactivée, comme pendant les
essais. Le projet original n'a pas été sauvegardé. Le Player final reste ouvert.

Limites conservées : version commerciale Horizon à confirmer (identifiant système
exact relevé ci-dessus), consentement refus/retry non provoqué, réglage Android de
débogage USB désactivé non testé. Les traces Quest recueillies avant le dernier
redémarrage contiennent aussi des messages OpenXR de caméra/boundary et les
avertissements DontDestroyOnLoad déjà présents dans l'APK ; ils ne sont pas annoncés
comme corrigés par les changements Desktop. Aucune référence nulle Desktop observée
pendant cette campagne.

Quest 3S reste hors périmètre. Mac et Linux restent à qualifier matériellement.
Le 27 octobre, l'utilisateur demande une validation pragmatique des builds réels :
le refus/retry USB ne doit plus bloquer Windows s'il ne se teste pas facilement.
Un dernier essai avec débogage USB effectivement désactivé est engagé, sans
reconstruction. Les cas non exercés restent visibles ; la liste n'est plus une
obligation de rejouer exhaustivement chaque scénario avant chaque OS.

## Validation Windows et essai sans mode développeur

Essai effectué le 27 octobre 2026 sur le même Player `WindowsRelease2` et l'APK
déjà installé, sans build ni réinstallation. Preuves supplémentaires :
`.artifacts/quest-ncm/usb-debug-off-20261027/`.

- Référence initiale lue dans Android : `adb_enabled=1`, `adb_wifi_enabled=0`.
  La page Mode développeur de Meta Horizon sur le téléphone est accessible.
- L'interrupteur de débogage USB individuel n'étant pas exposé, l'utilisateur
  désactive Mode développeur depuis le téléphone et confirme l'étape. Le test
  porte donc sur ce mode désactivé, pas sur une valeur `adb_enabled=0` lue par ADB
  pendant sa coupure, qui n'est pas revendiquée.
- Wi-Fi Quest coupé, Disconnect puis Connect côté Desktop : l'utilisateur
  confirme la connexion USB sans code, Small visible et une modification Windows
  reçue dans le Quest. Les TCP TLS du Player passent par l'interface NCM IPv6
  (scope local 54 pendant cet essai) ; aucun appareil n'est accessible par ADB.
- Retour arrière effectué : Wi-Fi réactivé et Mode développeur remis sur On.
  ADB redevient accessible ; `adb_enabled=1`, `adb_wifi_enabled=0` et wlan0 actif
  sont relus. Le retour aux réglages initiaux est donc vérifié.

**Windows 11 / Quest 3 validé fonctionnellement pour ces builds.** Le verrou
Windows peut être levé sur cette base. Le refus/retry NCM reste non exercé et
non bloquant conformément à la décision utilisateur ; aucun scénario n'a été
coché comme réussi sur la seule base d'un refus simulé. La version commerciale
Horizon reste non confirmée, l'identifiant système précis étant consigné plus haut.
Les messages XR existants signalés plus haut restent hors des corrections réseau.
Quest 3S demeure hors périmètre ; Mac/Linux attendent leurs essais matériels ciblés.
