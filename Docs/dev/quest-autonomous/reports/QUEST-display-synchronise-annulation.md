# Display synchronisé : annulation et reprise — 14 octobre 2026

## Corrections

- Après annulation d'un Display laissant des scènes incomplètes, le même Quest
  encore connecté et la synchronisation automatique toujours active imposent un
  dialogue modal en anglais : **Transfer** ou **Close**. Les scènes déjà
  synchronisées ne sont pas fermées. Une nouvelle annulation ou un échec de
  reprise ramène à ce choix ; aucune reprise automatique cachée.
- Le dialogue s'ouvre dans la continuation qui ferme le loading circle, sans
  frame d'interaction intermédiaire. Close utilise la suppression normale du
  Module3DMain, avec drainage natif sous chargement. La fermeture distante est
  conservée dans la file existante des fermetures et réessayée si nécessaire.
- Si la connexion ou la préférence change pendant le choix, les scènes Desktop
  sont conservées et un message explique comment reprendre manuellement.
- Une socket fermée par son token d'annulation peut produire une IOException
  contenant une SocketException sous Windows/Mono. Le transport la transforme
  maintenant en OperationCanceledException associée au token utilisateur. Une
  expiration devient TimeoutException ; une panne sans annulation reste un échec.
- La fin d'un lot incomplet arrête sa réception Quest active avant d'attendre son
  drainage. Une réception annulée ne publie pas d'erreur système localisée.
- Le menu Send visualization to Quest utilise le parcours de reprise explicite,
  épinglé à la scène sélectionnée, même en l'absence de spool complet réutilisable.
- Un snapshot incomplet sans receipt ne peut pas être renvoyé aveuglément.
  La reprise ferme les anciennes incarnations exactes et attend leurs ACK avant
  de libérer l'offre et de recapturer. Les tombstones Quest interdisent à une
  publication tardive de ressusciter l'ancienne scène. Une fermeture refusée ou
  incertaine conserve l'état de reprise et ne crée pas de nouveau snapshot.
- Les receipts Closed/Superseded d'un spool réutilisable déclenchent aussi une
  récupération explicite bornée, au lieu de bloquer chaque nouvelle tentative.
- Les messages produit des opérations Quest traduisent les catégories d'erreurs
  réseau en anglais ; les détails techniques restent disponibles dans les logs.

## Validation automatisée

- **34 tests EditMode réussis**, job `9bf30a2124ab4ea182601c265a3aa1b4` :
  transport TLS annulé pendant une lecture, écriture interrompue, panne réelle
  conservée, messages anglais, intégrité des blocs, drainage et retries exacts.
- **18 tests PlayMode réussis**, job `d9b1eb771dbd43f1a671407dc54b8b3a` :
  dialogue réel Transfer/Close, fermeture Desktop retirée du Module3DMain,
  déconnexion pendant le choix, fermeture de toutes les incarnations exactes,
  refus conservant l'offre, arrêt de réception avant libération de son gate,
  admission, annulation indépendante et absence de reprise automatique.
- La première exécution de la nouvelle fixture de fermeture utilisait une
  génération de session nulle ; corrigée à une génération valide avant le run
  réussi. Aucun changement de protocole requis pour cette correction de fixture.
- Console Unity sans erreur après les tests réussis.
- Le premier essai matériel a révélé que le callback du helper de chargement de
  Close démarre sur un worker. La fermeture bascule maintenant explicitement sur
  Unity avant de retirer une scène et de notifier la toolbar. Une assertion du
  thread des notifications a été ajoutée ; les deux tests concernés repassent,
  job `997b990c6b974ab593011ad630e4d6d3`.
- Contrôle des dépendances réussi : 44 assemblies, 162 arcs directs.
- Revue indépendante ciblée : aucune course bloquante ni remplacement d'une autre
  incarnation trouvé. La fermeture distante de Close conserve le mécanisme
  existant de file/reprise ; elle n'est pas annoncée comme un ACK immédiat.

Résultats : `.test-results/display-sync-cancellation-20261014`.

## Essais sur Quest

Trois cas réels réussis dans Unity Desktop avec le Quest USB éveillé, fixture
`visu_full_test`, scène Small :

1. Annulation après préparation Desktop, avant envoi : le dialogue obligatoire
   reste ouvert jusqu'au choix Close. La scène est retirée du Module3DMain.
2. Annulation pendant l'envoi à **23 %**, puis Transfer : la scène devient
   synchronisée, sans exception réseau affichée ni seconde copie Desktop.
3. Synchronisation automatique temporairement désactivée pour exercer le menu :
   annulation de l'envoi à **37 %**, puis nouvelle demande depuis le vrai prefab
   Quest Send window. La reprise réussit malgré le transfert interrompu.

Résultat inspecté : `quest-exercise.json`, `success=true`, zéro erreur Desktop,
zéro dialogue restant, zéro utilisateur de présentation. Les scènes de diagnostic
ont été fermées et la préférence originale de synchronisation rétablie à true.
Le log Quest collecté après les essais ne contient pas d'erreur Unity de réception.
L'inventaire distant final demandé après les essais n'a pas été confirmé avant
l'arrêt du mode Play ; aucune absence de scènes distante n'est revendiquée sur
cette seule base. Les ACK de fermeture exacte sont couverts par les tests TLS.

APK Android Release construit avec zéro erreur (job `build-7313e40021`), installé
et vérifié sur le Quest USB `2G0YC5ZHB20370` :
`9447cac953ec31f88a8f094a5e3bc192e1d614b094c6d93314aa1af84d8509de`.
La dernière correction Close concerne le parcours Desktop ; ce parcours n'est
pas utilisé par l'application Quest.
