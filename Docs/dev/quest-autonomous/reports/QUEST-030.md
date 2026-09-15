# QUEST-030 — Découverte, appairage mémorisé et reconnexion

**Mise à jour distribution du 2026-09-15 — D34 :** réseau principal sans ADB,
USB avancé optionnel avec ADB externe. Le build Windows décrit ici reste
inchangé ; la transition et le tutoriel sont différés dans
[QUEST-031](../tasks/QUEST-031.md). Les mentions D33 ci-dessous décrivent les
exigences à la date des essais ; consulter la [note actualisée](QUEST-030-distribution-and-adb.md).

## Résultat et périmètre

Décision D32 du 2026-09-14 : Windows/Quest maintenant, USB pour la recette physique,
Wi-Fi prévu mais non qualifié sur le réseau réel. Le propriétaire a confirmé le
casque rechargé et branché ; l'APK a été installé en USB le 2026-09-14.

**Validation propriétaire du 2026-09-14 :** parcours global USB accepté après
connexion du Quest, ouverture d'un projet et d'une visualisation, envoi et
visualisation dans le casque, puis débranchement/rebranchement avec reconnexion
réussie. [Preuve manuelle](../evidence/QUEST-030/manual-validation.json).
Une [anomalie d'état de projection d'activité](QUEST-030-activity-visibility.md)
est consignée et différée explicitement ; elle n'invalide pas ce parcours selon
le propriétaire. La distribution finale sans ADB (D33) reste à résoudre.

Le panneau Desktop propose les récepteurs HiBoP ouverts en USB ou sur le réseau
IPv4 local. La saisie IP reste accessible même en présence de résultats. Le
code est requis pour la première association ; les suivantes réutilisent
l'identité mémorisée. Le bouton **Envoyer au Quest** conserve le transfert existant.
Une interruption déclenche des tentatives périodiques sans réinstaller les données
globales ni effacer la scène lorsque la session Quest est toujours présente.

## Confiance et protocole

**Limite de distribution finale — D33 :** le propriétaire exige un parcours
sans ADB ni mode développeur. L'USB actuel ne satisfait pas cette exigence.
Voir la [note ADB, plateformes et CI](QUEST-030-distribution-and-adb.md).
Les tests manuels actuels continuent ; les adaptations de distribution restent
à décider et à implémenter séparément de cette consignation.

- J-PAKE NIST-3072 / SHA-256 de BouncyCastle **2.7.0**, DLL déjà embarquée, licence
  MIT inchangée. Trois rounds validés, dont confirmation mutuelle obligatoire.
- Chaque ID de participant inclut version, rôle, hash SHA-256 du certificat TLS
  réellement utilisé et nonce de session. Le correspondant vérifie la liaison
  au certificat de son propre canal. Le code n'est jamais transmis en clair,
  publié par découverte ou écrit dans un journal.
- Une annonce n'accorde aucune confiance. Le PAKE authentifie une nouvelle pin ;
  les reprises et transferts exigent ensuite cette pin et un credential de 32
  octets aléatoires. Les anciennes commandes d'appairage par code brut sont refusées.
- Cinq tentatives au maximum par code, tentatives interrompues et messages
  malformés inclus ; expiration après cinq minutes. **Y** renouvelle le code et
  révoque le credential précédent. Un redémarrage normal conserve la confiance.
- Identité et credential Quest écrits atomiquement dans un seul fichier ; sous
  Android, `Context.getNoBackupFilesDir()` fournit le répertoire privé exclu des
  sauvegardes. Les credentials Desktop utilisent DPAPI du compte Windows courant.
  Aucun secret dans PlayerPrefs ni dans la découverte.
- Desktop sauvegarde le credential avant l'installation globale afin de reprendre
  cette installation après interruption. Un identifiant de snapshot global évite
  de rejouer l'installation lors d'une simple reconnexion. Un nouveau Desktop ou
  un Quest redémarré réinstalle les globals avant les prochains envois.

Les primitives et la séquence suivent [BouncyCastle JPakeParticipant](https://raw.githubusercontent.com/bcgit/bc-csharp/master/crypto/src/crypto/agreement/jpake/JPakeParticipant.cs)
et [RFC 8236](https://www.rfc-editor.org/rfc/rfc8236.html).
L'intégration applicative et sa liaison TLS ont reçu une revue indépendante
bornée ; ceci ne constitue pas une certification cryptographique.

## Découverte et durée de vie

- USB : ADB autorisé par le casque, forward local automatique sur un port alloué
  par ADB, interrogation du récepteur TLS HiBoP. Le build Windows inclut adb et
  ses deux DLL avec NOTICE. HiBoP ne configure pas le mode ADB Wi-Fi et ne lance aucune commande de redémarrage du serveur. Le client ADB gère sa connexion au serveur local.
- Wi-Fi : requête UDP IPv4 sur le port 45872, réponse unicast contenant seulement
  nom/version/pin. Diffusion sur les interfaces IPv4 actives. Les réseaux invités,
  pare-feu ou isolation Wi-Fi peuvent empêcher cette découverte ; l'IP manuelle
  utilise toujours la même authentification sécurisée.
- Transport TLS : port Quest 45871 ; le port localhost USB est alloué par appareil.
- Liste actualisée cinq secondes après la fin de chaque recherche. Les annonces
  absentes sont retirées ; l'identité sélectionnée reste réservée lorsqu'elle
  disparaît, sans basculer vers un autre casque.
- Le Quest recrée aussi son écoute réseau après une erreur, avec le même objet
  d'appairage, le même budget d'essais et les mêmes globals. Le test associé valide
  cette conservation d'état ; il ne simule pas encore une panne d'interface réelle.
- Heartbeat toutes les cinq secondes, sérialisé avec appairage et transfert.
  L'onglet peut être fermé sans arrêter la reconnexion. Une liaison perdue est
  distinguée d'une identité oubliée. Les scènes déjà reçues restent disponibles.

## Lecture prioritaire du diff

| Fichier | Point à examiner |
| --- | --- |
| [PairingPake.cs](../../../../Assets/Scripts/HBP/Transfer/Transport/PairingPake.cs) | Bornes du protocole, trois validations et liaison aux identités TLS. |
| [QuestPairing.cs](../../../../Assets/Scripts/HBP/Transfer/Transport/QuestPairing.cs) | Ownership, limites d'essais, refus du protocole ancien, reprise idempotente des globals. |
| [PairingStorage.cs](../../../../Assets/Scripts/HBP/Transfer/Transport/PairingStorage.cs) | Écriture atomique et DPAPI Windows ; répertoire Android choisi dans QuestConnectionPanel. |
| [DesktopQuestPanel.cs](../../../../Assets/Scripts/HBP/UI/Quest/DesktopQuestPanel.cs) | Conservation de la pin en mode manuel, sélection, annulation et reconnexion. |
| [QuestConnectionSetup.cs](../../../../Assets/Scripts/HBP/Quest/Editor/QuestConnectionSetup.cs) | Dropdown et références sérialisées dans Quest Connection.prefab. |

## Vérifications

- **61 tests EditMode distincts réussis**, zéro échec ; trois contrôles propres au profil
  Android ignorés sur Windows. Suites `HBP.Transfer.Scene.Tests` et
  `HBP.PlatformConfiguration.Tests`, puis contrôle ciblé de conservation après
  recréation du listener.
- **12 tests PlayMode distincts réussis** : onze lors du premier passage, puis
  le contrôle du prefab corrigé et relancé. Ce contrôle ciblait un ancien prefab
  de prototype dont la session était incompatible avec le composant runtime ;
  il vérifie maintenant les références du prefab Quest de production.
- Couverture ciblée : mauvais code puis correction, verrouillage après cinq
  essais, abandon des tentatives, message malformé, certificat/credential erroné,
  refus du protocole ancien, appairages concurrents, interruption des globals,
  reprise sans réinstallation, persistance après redémarrage, stockage DPAPI,
  découverte sans authentification, liaison PAKE à deux empreintes différentes,
  double clic UI et conservation de la pin en mode IP manuel.
- La première exécution EditMode a révélé un filtre d'exceptions incomplet :
  `InvalidDataException` doit être traitée explicitement au niveau de chaque
  connexion. Correction validée par la relance complète des deux suites.
- Build Android ARM64 IL2CPP réussi, APK signé et vérifié : **394 447 911 octets**,
  neuf bibliothèques ARM64 attendues, pins hbp_core/hbp_math conformes au lock.
- Build Windows IL2CPP final réussi : zéro erreur. **Huit contrôles dans le
  Player final réussis, fermeture automatique et exit code 0**. Les deux
  premières exécutions avaient réussi leurs contrôles mais attendaient la boîte
  de confirmation de fermeture ; elles ont été arrêtées par l'agent. Le dernier
  binaire inclut le contournement limité à l'argument explicite de diagnostic.
- `Tools/format-code.cmd` exécuté sur les 14 C# modifiés ; seuls les espaces de
  fin de ligne ajoutés par Unity dans les prefabs ont été normalisés ensuite.
  Métadonnées de build et migration d'éditeur remises à leur état initial.
  Après le build Android, seuls des commentaires XML de TransportIdentity ont
  été précisés côté code ; son comportement compilé est inchangé.
- ADB livré avec Windows vérifié : version **37.0.0-14910828**, lancement de
  `adb version` réussi avec les seules dépendances embarquées.

### Correction après premier essai physique USB

Le propriétaire a observé `No Quest detected`. ADB listait le Quest et le
récepteur Android écoutait sur 45871, mais aucun forward n'était créé par Windows.
Le code source de Unity 6000.5.2f1 (`libil2cpp/icalls/System/System.Diagnostics/Process.cpp`)
confirme que `Process.CreateProcess_internal` n'est pas implémenté : la vérification
initiale d'ADB depuis PowerShell ne couvrait pas son lancement depuis IL2CPP.

`QuestAdbProcess` remplace cet appel par `CreateProcessW`, sans shell ni fenêtre,
avec liste explicite des handles héritables et délai maximal de quatre secondes.
L'annulation termine uniquement le client ADB créé. La sortie est bornée à 64 Kio
et son fichier temporaire supprimé. Revue indépendante des signatures Win64 et
du cycle de vie des handles : aucun défaut bloquant identifié.
Le lancement, la capture de sortie et le timeout ont été vérifiés directement
sous Windows (timeout observé : 4036 ms). Un diagnostic explicite
`-questUsbSmoke` couvre désormais le lancement depuis le Player IL2CPP et la
découverte TLS du casque physique, sans l'appairer ni lire son code.

Le build Windows corrigé a réussi (zéro erreur), puis **5/5 contrôles USB dans
ce Player IL2CPP ont réussi, exit code 0** : lancement/capture d'ADB, rejet d'un
exit non nul, annulation avant lancement, découverte TLS réelle du Quest,
stabilité de l'identité et du forward sur deux recherches. Le forward créé
pour le diagnostic a été retiré avant la relance interactive. Le propriétaire
a fermé l'ancien Player ; le corrigé a été relancé. L'APK Quest est inchangé.
Les huit contrôles d'appairage précédents portent sur le build Windows antérieur ;
la modification présente concerne le lancement du client ADB et son diagnostic.
La nouvelle recette d'appairage/envoi/reconnexion a ensuite été validée par le
propriétaire, avec l'anomalie d'affichage différée décrite ci-dessus.
Le formateur obligatoire a été exécuté avec succès sur les 15 C# modifiés.

Voir le [manifeste](../evidence/QUEST-030/manifest.json) pour les commandes,
versions, résultats finaux et chemins des binaires. La recette USB physique ne
constitue pas une qualification exhaustive : expiration en temps réel, rejeu de
transcriptions complètes, vecteurs officiels séparés et endurance ne sont pas
encore qualifiés par cette série. Le code refuse les cas invalides, mais la
présence des contrôles ne remplace pas ces preuves manquantes.

Revue indépendante : quatre anomalies identifiées puis corrigées et relues :
conservation de l'ancienne pin avec un credential en mode manuel, décodage borné
des IDs PAKE, calendrier de découverte qui ne bloque pas les heartbeats, remise
à zéro du compteur d'installations dans les tests. Tests de régression associés.

## Binaires préparés

- Windows : `.artifacts/quest-030/Windows/HiBoP.6.1.0.win64/HiBoP.exe` avec son
  dossier complet ; le client ADB et ses DLL sont dans `HiBoP_Data/StreamingAssets/QuestUsb`.
- Quest signé : `.artifacts/quest-030/Android/HiBoP.Quest.apk`, SHA-256
  `40aae101eb963842b6c651b7c91df655ffd7e37c5441de838449e58eb9d34247`.
- Rapports de compilation, XML et logs : `.test-results/quest-030/` et
  `.artifacts/quest-030/{Windows,Android}/*.build-report.json`.
- Déploiement USB effectué sur Quest 3 : `adb install -r` réussi. Après validation
  par le propriétaire de la fenêtre système demandant les manettes, HiBoP est
  actif dans le casque (rendu OpenXR et suivi confirmés dans le journal).
- Le Player Windows a été lancé ; le propriétaire confirme son interface visible
  sans projet chargé. Le chemin du seul processus HiBoP actif confirme le build
  QUEST-030. Voir la [preuve de déploiement](../evidence/QUEST-030/deployment.json).

## Recette manuelle

Le propriétaire a validé le parcours global le 2026-09-14. Les détails non
rapportés restent distingués de cette validation ; ils ne sont pas déduits de
la réussite générale.

| ID | Action | Résultat attendu | État |
| --- | --- | --- | --- |
| M1 | Connecter et appairer le Quest depuis Desktop. | Connexion établie via le parcours prévu. | VALIDE par le propriétaire après correction de la détection USB. Détails de saisie du code non rapportés séparément. |
| M2 | Ouvrir un projet et une visualisation, puis Envoyer au Quest. | Contenu reçu et visible dans le casque. | VALIDE avec réserve différée QUEST-030-OBS-01 : projection d'activité activée à tort sur Quest. |
| M3 | Débrancher/rebrancher l'USB. | Reconnexion automatique. | VALIDE explicitement. Conservation détaillée de scène/poses non rapportée séparément. |
| M4 | Fermer puis relancer HiBoP, sélectionner le même Quest puis Pair. | Reconnaissance mémorisée sans code ; envoi possible après restauration. | NON CONFIRME MANUELLEMENT dans ce retour. |
| M5 | Vérifier l'accès IP manuel alors que le Quest USB est listé. | Le champ reste accessible ; aucune bascule imposée vers le casque détecté. | NON CONFIRME MANUELLEMENT dans ce retour. |

Wi-Fi, plusieurs casques physiques, Mac et Linux restent non qualifiés.
Le parcours manuel demandé est validé, avec le défaut d'activité différé.
Les points D33 de distribution finale restent ouverts et la qualification ne
s'étend pas aux scénarios non rapportés. Aucun jalon scientifique ni tâche
suivante n'est exécuté. HiBoP Quest a été arrêté après validation selon D23 ;
absence de PID vérifiée, ADB conservé ([preuve](../evidence/QUEST-030/device-stop.json)).
