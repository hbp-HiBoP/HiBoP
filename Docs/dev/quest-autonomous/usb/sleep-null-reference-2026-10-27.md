# Référence nulle lors d'une reconnexion au Quest en veille

## Cause reproduite

Le chemin client de `QuestPairing.ConnectAsync` enregistrait `TcpClient.Close`
comme callback du token d'annulation et du timeout de connexion de cinq secondes.
Cette fermeture met `TcpClient.Client` à `null`. Dans le runtime livré avec Unity
6000.5.2f1, `BeginConnect` et `EndConnect` utilisent encore cette propriété sans
contrôle de nullité. La fermeture pendant une connexion en cours peut donc
remplacer l'erreur réseau attendue par une `NullReferenceException`.

Le comportement a été vérifié dans le runtime Mono local et dans le code C++
IL2CPP généré pour le Player Windows. Une revue indépendante en lecture seule a
confirmé le même chemin d'exécution.

Deux reproductions avant correction :

- Token déjà annulé, IPv4 puis IPv6 : deux tests en échec, avec une pile dans
  `TcpClient.BeginConnect` et `QuestPairing.ConnectAsync`.
- Connexion à `10.86.13.37`, adresse IPv4 de l'interface `usb0` réellement annoncée
  par le Quest pendant la session : après **5,0015 s**, référence nulle dans
  `TcpClient.EndConnect`. Aucun établissement TLS n'avait encore eu lieu.

Cette deuxième reproduction explique les rafales sans mesure `desktop.tls`
observées lors des essais précédents. La veille peut laisser les sockets Android
accessibles alors que Unity ne répond plus ; les tentatives sur les autres
endpoints rencontrent ensuite la course de fermeture TCP.

## Correction

L'annulation ferme désormais la **socket capturée**, puis le propriétaire
`TcpClient` est libéré après la fin de l'opération asynchrone. Le callback ne
remet donc plus sa propriété `Client` à `null` pendant `BeginConnect`/`EndConnect`.
L'erreur de socket fermée passe par la conversion existante en
`OperationCanceledException` ou `TimeoutException`.

La correction ne capture pas les références nulles pour les ignorer et conserve
les budgets de timeout existants. Le diagnostic temporaire détaillé a été retiré.
La gestion des identités, secrets, scènes et de la grâce de 500 ms ne change pas.

## Vérification

- Avant correction : **2/2** nouveaux tests de token préannulé reproduisent la
  référence nulle.
- Après correction : **12/12** tests réseau réussis, dont annulation immédiate
  IPv4/IPv6 avec conservation du token et annulation pendant TLS avec fermeture
  effective de la connexion.
- **29/29** tests existants d'appairage réussis, notamment annulation en lecture/
  écriture, refus d'authentification, reconnexion et restauration après redémarrage.
- Même adresse USB après correction : **5,0007 s**, `TimeoutException` avec
  `ObjectDisposedException` de socket en cause interne, aucune référence nulle.
- Contrôle des dépendances d'assemblies : 44 assemblies et 163 références, réussi.
- Player Windows IL2CPP de développement construit : 0 erreur, 48 avertissements,
  227,40 s. Diagnostic exécuté dans ce Player : **10 contrôles réussis**, sortie 0,
  incluant les annulations IPv4/IPv6 ajoutées au diagnostic d'appairage existant.
  Aucun binaire ADB dans le dossier construit.
- Player Windows **release** IL2CPP construit : 0 erreur, 44 avertissements,
  175,23 s. Dossier validé sans ADB :
  `.artifacts/quest-ncm/sleep-null-reference/WindowsRelease/HiBoP.6.2.0.win64/`.
  Le build release est disponible pour les prochains essais manuels. La correction
  concerne le client Desktop ; aucune réinstallation de l'APK n'est nécessaire.

Les preuves locales (XML avant/après, piles complètes et rapports de build) sont
dans `.artifacts/quest-ncm/sleep-null-reference/`. Les timeouts d'une application
Quest endormie restent des indisponibilités réseau attendues ; une référence nulle
n'est pas considérée comme telle.
