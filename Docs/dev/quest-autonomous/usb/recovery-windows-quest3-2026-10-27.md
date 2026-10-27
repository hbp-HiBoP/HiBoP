# Reprise USB vers Wi-Fi et scène — Windows / Quest 3

Essais manuels du 27 octobre 2026, avec surveillance des connexions et des logs.
Heures ci-dessous : Europe/Paris (UTC+1). Les traces locales sont conservées dans
`.artifacts/quest-ncm/recovery-20261027/`, dossier ignoré par Git.

## Préconditions et limites

- Player Windows x86-64 release IL2CPP construit lors de la migration, Unity
  6000.5.2f1 ; APK Quest release installé pendant la première validation.
- Quest 3 avec Wi-Fi actif. Le poste Windows rejoint le même réseau par Ethernet ;
  « reprise Wi-Fi » désigne ici le chemin Wi-Fi du Quest.
- Identité du Quest conservée : préfixe d'empreinte `8CB870`.
- Au début, le casque a refusé le secret anciennement enregistré sur Desktop.
  Avec l'accord de l'utilisateur, une association de référence a été établie par
  code avant les essais chronométrés. Ces essais ne prouvent donc pas la validité
  de cette ancienne association avant le réappairage ; l'origine du refus n'a pas
  été établie.
- Une seule scène a fait l'objet de l'essai avec restauration. Le projet original
  `visu_full_test.hibop` a été chargé manuellement par l'utilisateur, qui a autorisé
  son utilisation sans sauvegarde. Une copie préparée au préalable n'a finalement
  pas été utilisée. Le hash du fichier original est identique avant/après.
- Les opérations d'interface ont été réalisées par l'utilisateur lorsque
  l'automatisation Windows ne reflétait pas correctement l'état interactif.

## 1. Coupure USB sans scène

La session initiale utilisait IPv6 link-local sur l'interface USB Windows 8.
Les annonces mDNS USB et réseau local portaient la même identité TLS.

- Disparition de l'interface NCM : **14:01:26,871**.
- Première connexion TCP établie sur le réseau local : **14:01:29,055**, soit
  **2,18 s** après la disparition de l'interface.
- Les logs confirment ensuite la reprise des globals et l'ouverture de session.
  Le panneau Quest présente le même casque via le réseau local et permet la
  déconnexion de la session active.
- Aucun clic Reconnect ni nouvelle saisie de code pendant la reprise. L'unique
  authentification PAKE du log correspond à l'association de référence préalable.

Résultat : **reprise automatique constatée, sans réappairage après la coupure**.

## 2. Coupure USB avec scène publiée et synchronisée

L'utilisateur a rebranché USB, déconnecté puis reconnecté la session pour repartir
sur USB, et envoyé une scène déjà ouverte. Aucun nouveau code n'a été nécessaire.
La publication `4801fee2ff2f442a819df9157a2ba6fb` est confirmée côté Quest à
14:08:19,806 ; la synchronisation initiale est terminée côté Desktop avant la
coupure. L'utilisateur a confirmé la scène visible dans le casque.

- Disparition de l'interface NCM : **14:09:05,212**.
- Première connexion TCP réseau local : **14:09:07,304**, soit **2,09 s**.
- Chemin repris : IPv6 link-local sur l'interface Windows **4**, vers le Wi-Fi
  Quest. Le scope USB **8** n'est pas réutilisé.
- Les logs montrent la perte du replica, la tentative dans la grâce de 500 ms,
  puis sa fin et la reconstruction de la session sur le nouveau chemin.
- Aucune nouvelle authentification PAKE et aucun réencodage de scène après la
  coupure dans la fenêtre de test. Le fichier d'association mémorisée est identique
  avant/après la coupure.
- Après au moins 15 s sans câble, sans Reconnect ni Send, l'utilisateur confirme
  la scène conservée et ses colonnes, couleurs et coupes préservées.
- Toujours sans câble, l'utilisateur confirme une modification synchronisée de
  Windows vers Quest, puis de Quest vers Windows. Cela confirme la reprise active
  de la synchronisation, au-delà du seul maintien d'une image locale.

Résultat : **scène et état conservés ; synchronisation bidirectionnelle reprise
automatiquement après expiration de la grâce, sans code ni renvoi manuel**.

## Portée du chronométrage et suites

Le collecteur échantillonne l'état de l'interface et les connexions TCP établies
environ toutes les 100–150 ms. Les délais de 2,18 s et 2,09 s mesurent la première
connexion TCP réseau local après la disparition NCM. Ils ne mesurent pas exactement
la fin de toute réconciliation applicative ; le critère complet « restauration
en moins de 15 s » nécessite encore un jalon de fin corrélé à cette horloge.

Pendant la préparation avec casque posé/en veille, les logs Desktop ont également
contenu des timeouts et des avertissements `Object reference not set to an instance
of an object`. Les timeouts seuls sont compatibles avec une application Unity
endormie ; les références nulles restaient à diagnostiquer à l'issue de cet essai
et n'étaient pas considérées comme normales. Le Quest a aussi journalisé des erreurs du
runtime OpenXR, notamment de visibilité de boundary lors du retrait du casque.
Le diagnostic et la correction ultérieurs de la course TCP sont décrits dans
[sleep-null-reference-2026-10-27.md](sleep-null-reference-2026-10-27.md).

Restent notamment : reprise avec ouverture automatique de nouvelles connexions
désactivée, interruption pendant transfert, scènes multiples et conflits, restart
de l'application/du casque, refus/retry USB, et la matrice Mac/Linux. Quest 3S a
été retiré du périmètre de qualification à la demande de l'utilisateur.
`qualification.json` reste inchangé : aucune release n'est débloquée
par ces deux essais seuls. Aucun fichier C# de production n'a été modifié pendant
ces essais.
