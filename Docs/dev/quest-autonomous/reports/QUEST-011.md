# Rapport QUEST-011 — Appairage et envoi depuis les applications

## Résultat

Le menu principal Desktop possède une entrée **Quest**. Son panneau permet
d'inspecter l'adresse du casque, de comparer son empreinte complète, de saisir
son code d'appairage, puis d'utiliser **Envoyer au Quest** sur la colonne
anatomique sélectionnée. Le panneau du casque affiche adresse, appairage,
réception, préparation et disponibilité hors connexion. Y recommence
l'appairage ; B masque ou réaffiche les informations après réception.

Auparavant, ces opérations demandaient le harness QUEST-010. Le parcours
produit utilise maintenant un serveur TLS embarqué dans le casque et le
Player Desktop réel. Le payload HBT v2/HBNA, la capture scientifique et la
publication atomique sont réutilisés. Le récepteur réseau conserve les mêmes
gardes d'annulation, de déduplication et de propriété de la session.

Le bouton d'envoi est désactivé pendant une opération ou si la sélection est
incompatible. **Retry same snapshot** conserve les octets et l'identité de
la capture après erreur ; **Envoyer au Quest** crée une nouvelle capture.
Les étapes de préparation Desktop, transfert et préparation Quest sont
distinctes. Il n'existe pas encore de calcul scientifique Quest dans le
périmètre anatomique : aucune progression de calcul fictive n'est affichée.

## État et provenance

- Implémentation : IMPLEMENTEE ; technique : REUSSI ; manuel : VALIDE.
- Branche `feature/xr-autonomous`, HEAD initial
  `77629706ebf7e108390957c25e7e43a554f7e22b`, checkout initial propre.
- Unity `6000.5.2f1`, transport TLS/BouncyCastle de QUEST-009/010 ; aucun
  dépôt scientifique voisin modifié. Aucun commit ni push.
- Dépendances : capture sélectionnée QUEST-006, manipulation QUEST-008,
  transport et session QUEST-010. Aucun code historique supplémentaire repris.
- [Manifeste](../evidence/QUEST-011/manifest.json).

## Ce que je conseille de reviewer

| Priorité | Fichier / symbole / objet | Règle à vérifier |
| --- | --- | --- |
| 1 | [DesktopQuestPanel](../../../../Assets/Scripts/HBP/UI/Quest/DesktopQuestPanel.cs), `RunAsync`, `SendAsync` | Garde avant le premier await, sélection réelle, capture immuable conservée pour retry, invalidation de confiance si l'adresse change. |
| 2 | [QuestPairing](../../../../Assets/Scripts/HBP/Transfer/Transport/QuestPairing.cs) | Inspection sans secret, confirmation de l'empreinte hors bande, pinning strict avant code/secret, cinq essais et cinq minutes, un propriétaire. |
| 3 | [QuestAnatomySession.ReceiveStreamAsync](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestAnatomySession.cs) | La réception entrante utilise le même propriétaire d'annulation et la même publication ; fermeture du stream lors de Disconnect/CloseSession. |
| 4 | [QuestConnectionPanel](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestConnectionPanel.cs) | Reset attend l'ancien serveur avant de libérer l'identité ; pause coupe la connexion sans effacer l'anatomie. |
| 5 | [QuestConnectionSetup](../../../../Assets/Scripts/HBP/Quest/Editor/QuestConnectionSetup.cs), [prefab Desktop](../../../../Assets/Prefabs/General/Quest%20Connection.prefab), [prefab Quest](../../../../Assets/Prefabs/Quest/QuestBootstrap.prefab) | Références sérialisées, panneau accessible depuis `Main menu/Left/Quest`, diagnostic fichier désactivé. |

## Appairage et erreurs

Le casque écoute en IPv4 sur TCP **45871**. Son identité, code et secret ne
sont pas persistés. Le code est aléatoire à six chiffres et ne suffit pas à
authentifier le serveur : l'utilisateur doit comparer les **huit groupes**
de l'empreinte SHA-256 complète avant de cocher la confirmation Desktop.
L'inspection ne transmet aucun secret. Les connexions suivantes imposent
le pin, puis le code initial ou le secret de session de 256 bits.

Chaque connexion expire après 30 secondes, y compris transfert et publication.
Le code expire après cinq minutes ; cinq essais sont admis pour l'identité
entière, même si le client se reconnecte. Y renouvelle l'identité et le code
sans supprimer la session anatomique. Une réponse d'appairage perdue demande
Y puis un nouvel appairage ; le secret n'est jamais redonné sur le seul code.

Les erreurs d'adresse, réseau, empreinte/code, sélection et interruption ont
un état lisible et une action de reprise. Une trame inconnue ou corrompue
ferme sa connexion, sans arrêter l'écoute. Les reçus Closed/Superseded ne
sont pas présentés comme une publication réussie. Aucun geste ni transform
de présentation n'est envoyé.

## Vérifications effectuées

Les logs volumineux restent sous `C:\HBP\Software\HiBoP\.test-results\quest-011`.
Les preuves techniques automatisées sont distinguées du parcours physique et
du retour propriétaire. Le contrôle Windows a été arrêté avec Échap ; aucune
action Computer Use n'a été exécutée après cette interruption.

| ID | Scénario | Résultat |
| --- | --- | --- |
| T1 | PlayMode `HBP.Quest.PlayModeTests`, fixture `.artifacts/quest-008/fixture/quest-anatomy.hbna` | **38/38 réussis**, 7,72 s, exit 0, `playmode-release.xml`. |
| T2 | Revue indépendante appairage, annulation, retry | Deux défauts corrigés : une InvalidDataException pouvait arrêter le serveur entier ; l'expiration est désormais revérifiée après lecture du code. Tests de commande inconnue/corruption et cinquième essai ajoutés. |
| T3 | EditMode `HBP.Transfer.Anatomy.Tests;HBP.Transfer.Anatomy.Desktop.Tests;HBP.Quest.Anatomy.Tests;HBP.PlatformConfiguration.Tests` | **87 réussis, aucun échec, un ignoré** réservé au profil Android ; exit 0, `editmode-final.xml`. |
| T4 | Test de disposition du menu réel et retour au profil DesktopWindows | **23 réussis, aucun échec, un ignoré** réservé Android ; exit 0, `layout.xml` puis `final-desktop.xml`. |
| T5 | `Tools/format-code.cmd` | Exit 0 après les dernières modifications, `format-release.log`. Solution régénérée pour inclure les 13 scripts concernés. Contrôle de whitespace C#/Markdown/asmdef/PowerShell réussi. |
| T6 | `Tools/Build-QuestConnectionPlayers.ps1 -Target Windows` | **Réussi**, IL2CPP, 44,89 s, 0 erreur, 39 warnings, `windows-build.log` et `DesktopWindows.build-report.json`. |
| T7 | Même script `-Target Android` | **Réussi**, ARM64 IL2CPP, 141,60 s, 0 erreur, 41 warnings, `android-build.log` et `Quest.build-report.json`. |
| T8 | `Tools/Test-QuestApk.ps1 -Apk .artifacts/quest-011/Android/HiBoP.Quest.apk -ReportPath .test-results/quest-011/apk-content.json` | **Réussi**, 7 bibliothèques ARM64, 162 990 184 octets ; aucun binaire scientifique Desktop. |
| T9 | ADB Wi-Fi, installation et lancement APK final | **Réussi**, `adb install -r` exit 0, `am start -W` Status ok, lancement à froid en 363 ms. |
| T10 | Initialisation du panneau sur Quest physique, sans probe | Adresse/code/empreinte affichés, passthrough COMPOSITION READY, **No anatomy received** malgré l'ancienne fixture HBNA encore présente dans les fichiers persistants. Aucun `quest010-config.json`. Les longues lignes visibles hors champ ont ensuite été repliées et la taille du texte réduite dans l'APK final. |
| T11 | Parcours entre deux Players, sans probe ; indépendance des vues | **Réussi**, retour propriétaire du 2026-09-08 : appairage après réinspection, transfert et manipulation ; progression lisible et vues indépendantes confirmées séparément. Le Player Desktop journalise `delivery=Published`, 3 870 258 octets, hash `7052315487cc911f642cc7cc06970b4db340bf38a855b2ff12751d837cd0536b` (copie figée `desktop-player-validated.log:42`). |
| T12 | Fin de recette D23/D26 | **Réussi**, HiBoP arrêté à 17:17:12 +02:00, `am force-stop` exit 0, `pidof` vide / exit 1 ; ADB Wi-Fi `device`, maintien éveillé sur alimentation `15`. Preuve `device-final-state.json`. |

Les tests T1 couvrent erreurs d'adresse/code/pin, cinq essais sur connexions
successives, cinquième code correct, appairage unique, secret erroné, commande
inconnue, chunk corrompu, nouvelle tentative réussie, publication et reçu,
déduplication, fermeture pendant réception, sélection invalide et double clic UI.

Le premier essai visuel Windows a montré que le HorizontalLayoutGroup du menu
déplaçait le panneau hors fenêtre. Le prefab définit maintenant
`Quest Connection/LayoutElement.ignoreLayout=true` et
`Panel/Canvas.overrideSorting=true`, `sortingOrder=100`. Le test T4 exerce
ce menu réel ; le propriétaire a confirmé ensuite **« Oui c'est bon pour le
panneau ! »**, le 2026-09-08, avant la recette réseau.

Les premiers runs non conclusifs sont conservés : erreur de compilation du
test `NonParallelizable` non disponible dans NUnit Unity, puis test de sélection
qui oubliait de réinitialiser `CutsNeedUpdate`. Aucun de ces runs ne compte
comme succès. Les étapes finales ont été relancées après correction.

Le log Quest conserve la `ClassNotFoundException` non fatale concernant
`com.google.android.play.core.assetpacks.AssetPackManager`, déjà documentée
dans QUEST-003/007. L'initialisation continue jusqu'à COMPOSITION READY.
Les réécritures automatiques Unity de BuildInfo, URP, icônes Android et
migrations OpenXR ont été inspectées, conservées dans `generated-settings.diff`,
puis rétablies ; elles ne font pas partie de QUEST-011.

## Validation manuelle réalisée

Binaires locaux, construits depuis les modifications non commitées identifiées
dans le manifeste :

- Windows : `C:\HBP\Software\HiBoP\.artifacts\quest-011\Windows\HiBoP.6.1.0.win64\HiBoP.exe`.
- Quest : `C:\HBP\Software\HiBoP\.artifacts\quest-011\Android\HiBoP.Quest.apk`, installé sur `192.168.1.18:5555` ; SHA-256 `2aa695d4402a0bb03a638c0aae63e50e6f9574ec458c9a440a2272cdd04fbe3a`.
- Fixture : `C:\HBP\Software\HiBoP\.artifacts\quest-006\fixture\quest-mni-anatomy.hibop`, visualisation **MNI Anatomy**, données MNI du dépôt autorisées par D14.

Le Player Desktop est conservé ouvert ; HiBoP sur Quest a été arrêté après
validation. Pour relancer Desktop :

```powershell
& 'C:\HBP\Software\HiBoP\.artifacts\quest-011\Windows\HiBoP.6.1.0.win64\HiBoP.exe' -pf 'C:\HBP\Software\HiBoP\.artifacts\quest-006\fixture\quest-mni-anatomy.hibop' -v 'MNI Anatomy' -screen-fullscreen 0
```

| ID | Action exacte | Résultat attendu | Statut |
| --- | --- | --- | --- |
| M0 | Cliquer **Quest** dans le menu supérieur Desktop. | Panneau Quest connection entièrement visible et lisible. | **VALIDE**, retour propriétaire du 2026-09-08 cité ci-dessus. |
| M1 | **Inspect Quest**, comparer les huit groupes, cocher **All groups match…**, saisir le code, puis **Pair**. Après refus de sécurité, recommencer l'inspection, la comparaison et la saisie. | Erreur de connexion récupérable et appairage réussi. | **VALIDE**, retour du 2026-09-08 ci-dessous. Le cas `abc` était proposé ; son exécution physique n'a pas été confirmée séparément, mais il est couvert par les tests automatisés. |
| M2 | **Envoyer au Quest** pour MNI Anatomy ; observer la progression puis manipuler le cerveau avec les gâchettes d'index. | Anatomie reçue par réseau, étapes lisibles, manipulation locale et vues indépendantes. | **VALIDE**, transfert/manipulation puis progression/indépendance confirmés le 2026-09-08. Les raccourcis B/X ne sont pas revendiqués comme revalidés séparément dans cette recette. |

Le premier appairage a affiché « pairing/security check failed ». Le propriétaire
a ensuite confirmé : « j'ai pu refaire l'étape inspect, puis comparer les
fingerprint puis remettre le code et pair là c'était bon. J'ai ensuite pu
transférer le mesh et interagir avec ». À la question complémentaire sur la
progression lisible et l'indépendance de la vue Desktop : **« Oui, les deux sont
bons »**. Ces retours constituent la validation manuelle ; aucun probe n'a
effectué le transfert à sa place.

La réinspection a résolu l'échec sans reconstruire les Players. Un renouvellement
d'identité pendant une pause est une explication possible, non démontrée par
les journaux disponibles : ni le refus du code ni le changement d'empreinte
n'y sont distingués. Après Y ou une reprise de l'application, refaire
**Inspect Quest**, comparer la nouvelle empreinte puis saisir le code actuel.

Les journaux finaux ont été conservés, puis seul `fr.crnl.hibop.quest` a été
arrêté et l'absence de PID vérifiée selon D23. Le casque reste éveillé sur
alimentation, ADB Wi-Fi conservé (D26). Aucune validation manuelle restante
ni décision propriétaire n'est nécessaire pour clôturer QUEST-011.

## Décisions, limites et suite

Emplacement UI choisi dans le menu principal, cohérent avec le Desktop
existant. L'appairage manuel et ses réglages sont des choix d'implémentation
à essayer, sans nouvelle décision scientifique. La découverte automatique,
le multicasque et la persistance après arrêt restent hors périmètre.

Une pause Android coupe le serveur ; sa reprise renouvelle l'appairage,
sans supprimer l'anatomie déjà publiée. Une simple interruption réseau sans
pause conserve l'identité et permet **Retry same snapshot**. La qualification
d'une minute hors réseau relève de QUEST-012 et n'est pas déclarée acquise ici.

Prochaine tâche proposée : QUEST-012, sans exécution automatique.
