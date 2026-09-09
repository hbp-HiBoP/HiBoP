# Exécuter une tâche Quest dans une nouvelle conversation

## Invocation

L'utilisateur peut écrire simplement « Implémente QUEST-008 ».
Pour pointer explicitement la fiche sans dépendre de sa recherche par ID :
« Implémente la tâche décrite dans Docs/dev/quest-autonomous/tasks/QUEST-008.md ».
Les instructions spécifiques au chantier sont portées par ces fiches et ce
contrat ; aucun routage Quest n'est ajouté au AGENTS.md global.
Chercher l'ID dans [tasks/README.md](tasks/README.md), lire la fiche
tasks/QUEST-008.md, puis appliquer le présent contrat. Les chemins de travail
sont ceux du dépôt courant ; ne pas supposer un répertoire Windows sur Mac.

Autres demandes prises en charge : « Reprends QUEST-008 », « Review QUEST-008 »,
« Implémente la prochaine tâche Quest ». Pour cette dernière, choisir la première
tâche dont les dépendances utiles sont établies dans le registre ET le code.
Une demande de review seule n'autorise pas une nouvelle feature.

La demande explicite d'implémenter une tâche autorise son périmètre. Ne pas demander
une nouvelle autorisation générique parce que les documents de cadrage antérieurs
disaient « avant implémentation ». Les questions de décision précises restent
à poser lorsqu'une réponse nécessaire manque. Ne pas enchaîner automatiquement
les autres tâches après la tâche demandée.

## Sources à lire

1. AGENTS.md applicable, présente fiche et fiche de la tâche.
2. [Journal des décisions](01-decisions-and-open-questions.md).
3. [Registre](TASK-STATUS.md) et rapports des dépendances pertinents.
4. Document thématique indiqué et code réellement présent.
5. Fiche de jalon pour comprendre la démonstration globale, sans absorber son
   périmètre dans la tâche.

Les décisions explicites récentes de l'utilisateur priment. Les fiches déterminent
le périmètre détaillé ; la roadmap est une vue d'ensemble. L'ancien prompt et
les documents XR historiques ne réintroduisent aucune décision abandonnée.
En cas de contradiction matérielle non résolue par les décisions, exposer le
point précis, demander une décision et continuer le travail indépendant.

## État réel et provenance

Au début : état Git/branche, modifications locales et rapports existants.
Le 2026-09-07, la branche courante a été confirmée comme feature/xr-autonomous,
à 8b868a5b8, issue de develop/origin/develop local. Ne pas recréer la branche :
les instructions historiques de création étaient une proposition désormais dépassée.

Les anciens fichiers XR/Shared/Spikes ne sont pas nécessairement dans le checkout.
Dans les points d'entrée des fiches, Core/..., Data/... et UI/... abrègent
Assets/Scripts/HBP/Core/..., Assets/Scripts/HBP/Data/... et Assets/Scripts/HBP/UI/....
Un nom de classe sans chemin complet est à localiser avec rg --files/rg ; il
ne prescrit pas la création d'un fichier du même nom. Les préfixes hbp_core/ et
hbp_math/ désignent les dépôts voisins, dont les instructions propres s'appliquent.
Consulter les fichiers versionnés par git show feature/xr:chemin ou un SHA
audité, notamment eb26c323e, et git ls-tree pour les localiser. Vérifier que
la référence existe. Ne pas confondre artefacts locaux, sources versionnées et
preuves physiques anciennes. Un fichier manquant localement n'autorise pas un
merge global. Préserver les GUID .meta lorsqu'une reprise ciblée le requiert et
consigner source/commit, adaptations et dépendances de chaque reprise.

Ne pas purger les anciens dossiers, lancer un git clean, changer de branche,
stager tous les fichiers, committer ou pousser pour « préparer » une tâche.
Utiliser le checkout choisi par l'utilisateur. Une opération Git supplémentaire
doit rester justifiée et dans le périmètre demandé.

L'état Unity/appareils se vérifie selon AGENTS.md : MCP et ressources avant
action si l'éditeur est ouvert, CLI appropriée sinon. Demander tôt si un état
interactif important est incertain. Pour Quest, adb devices hors sandbox avant
opération ; si aucun casque autorisé n'est visible, demander sa connexion et
utiliser Tools/Connect-QuestAdbWifi.ps1 s'il est disponible, en reprenant le script
historique sélectivement si la tâche le nécessite. Ne pas réinstaller/redémarrer
des applications pour contourner une incertitude que l'utilisateur peut lever.

### Batterie et fin des essais sur Quest

À la demande du propriétaire (D23), après chaque essai sur casque déclaré validé,
l'agent doit récupérer les preuves nécessaires puis arrêter le processus HiBoP
sur ce casque pour économiser la batterie. Pour un essai manuel, attendre le
retour de validation du propriétaire avant cet arrêt ; ne pas fermer l'application
pendant qu'il doit encore l'observer ou manipuler les contrôleurs.

Utiliser le transport ADB vérifié, en ciblant explicitement le casque et le
package de l'APK testé. Pour l'application Quest actuelle :

```powershell
adb -s <serial-ou-IP:port> shell am force-stop fr.crnl.hibop.quest
adb -s <serial-ou-IP:port> shell pidof fr.crnl.hibop.quest
```

Vérifier que `pidof` ne retourne plus de PID (son code 1 est normal si le
processus est absent). Consigner l'arrêt dans le rapport de l'essai. Ne pas
arrêter le serveur ADB, les services système ou d'autres applications : la
connexion doit rester disponible pour la reprise. Si le casque est déconnecté,
indiquer que l'arrêt n'a pas pu être vérifié plutôt que le prétendre effectué.

Pour le casque de développement, D26 demande désormais
`Tools/Connect-QuestAdbWifi.ps1 -KeepAwakeWhilePluggedIn` : conserver l'override
de proximité et le maintien éveillé sur alimentation entre les essais.
L'arrêt de HiBoP prévu par D23 ne remet pas le casque en veille.
Sur un autre casque, ou à la demande d'un usage normal, employer
`-NoProximityOverride`. Pour restaurer le mode normal sur le casque configuré,
exécuter `adb -s <IP:port> shell svc power stayon false` puis
`adb -s <IP:port> shell am broadcast -a com.oculus.vrpowermanager.automation_disable`.
Après redémarrage du casque, relancer le script ; le mode ADB TCP et l'override
ne sont pas garantis persistants. Dans Unity / Preferences / External Tools,
désactiver `Kill ADB server on exit` et `Kill external ADB instances` pour
préserver la session de développement. Les clients ADB doivent partager une
version compatible ; éviter les arrêts/redémarrages automatiques du serveur.
Après connexion Wi-Fi, employer `adb -s <IP:port>` : `adb -d` cible uniquement
le transport USB. Exécuter les opérations ADB hors sandbox Windows.

## Dépendances et granularité

Chaque tâche correspond à un changement explicable ou à une preuve bornée.
Elle peut traverser quelques fichiers/dépôts si c'est nécessaire à ce résultat.
Les jalons J0–J7 restent des démonstrations, pas des unités de diff.

Vérifier les dépendances dans le code et les rapports, pas seulement leur statut.
Une validation manuelle en attente n'interdit pas mécaniquement tout travail
suivant : identifier si elle conditionne réellement la tâche demandée. Un contrat
technique testé peut être réutilisé alors que le confort des gestes attend un
retour ; une tolérance scientifique non décidée interdit un verdict de parité.
Dire ce qui reste provisoire sans créer d'approbation générale supplémentaire.

Si une dépendance technique indispensable manque, préciser laquelle et proposer
la prochaine tâche prête ; ne pas implémenter silencieusement tout le backlog.
Continuer la partie indépendante utile. Si l'audit révèle une tâche encore trop
large, proposer un découpage de son reste en sous-tâches avec IDs stables
QUEST-NNN-A/B et mettre à jour index/dépendances après l'accord de périmètre
nécessaire ; conserver les preuves déjà acquises.

## Questions et décisions

Ne pas poser une question dont le dépôt ou une décision enregistrée donne la réponse.
Pour une vraie décision, fournir : fait observé, impact sur cette tâche, options,
recommandation motivée et partie pouvant continuer. Ne pas demander le choix
d'une API ou d'un nom de fichier courant sans conséquence produit.

Inscrire la réponse dans le journal (nouvel ID Dxx, date et contexte), puis
mettre à jour les seules fiches affectées. Ne pas prétendre qu'une proposition
technique ou le silence est une validation utilisateur.
Les réglages UX provisoires sont à présenter pour essai ; les règles scientifiques
ne changent pas sans justification et décision explicites.

## Réutilisation du code Desktop dans les tâches Quest

Retour d’expérience de [QUEST-018](reports/QUEST-018.md) : demander un chemin commun
Desktop/Quest n’impose pas de créer une nouvelle classe.

- Avant toute extraction, examiner les classes de base et wrappers existants.
  Les appeler directement ou les compléter si la responsabilité leur appartient
  déjà. Une nouvelle couche doit apporter une responsabilité distincte, expliquée
  dans le rapport ; relayer quelques appels existants ne suffit pas.
- Placer une opération au niveau de sa portée réelle : une création de grille ou
  un binding générique ne doit pas dépendre d’une classe nommée pour la densité,
  l’anatomie ou Quest. Réserver aux spécialisations les différences effectives.
- Si une correction de préparation, de concurrence ou de durée de vie concerne
  plusieurs colonnes, l’appliquer au chemin commun des colonnes concernées, avec
  spécialisation au besoin et tests adaptés. Expliciter cette extension nécessaire
  sans engager un refactor global anticipé.
- Conserver les conventions existantes, notamment `UniTask` pour les opérations
  attendues et `UniTaskVoid` pour les entrées détachées appelées avec `Forget()`.

## Exécution et contrôles

Faire une annonce courte du résultat visé et de la vérification. Implémenter
seulement la tâche, en préservant le Desktop. Respecter prefab-first et le code
scientifique unique ; pas de nouveau framework anticipant les futures features.

L'agent exécute les tests, construit les binaires et prépare la recette avant
de demander une manipulation utilisateur. Les tests de pure logique ne sont
pas une checklist manuelle à déléguer au propriétaire. Si une ressource manque,
dire précisément ce qui n'a pas été exécuté et laisser les preuves restantes ouvertes.

Appliquer Tools/format-code.cmd aux C# modifiés avant handoff, puis vérifier
le diff. Les tests Unity async attendent directement les opérations, sans
Wait/Result ou assertions async bloquantes. Toute commande lançant Unity sur
Windows suit les règles d'exécution hors sandbox d'AGENTS.md.

Pas de lancement de CI distante, publication, push ou déploiement de distribution
implicite. Les builds locaux et essais sur le matériel identifié nécessaires
à la tâche suivent les autorisations de la session ; ne pas redemander une
approbation déjà donnée. La qualification ne demande pas un commit automatique.

## Rapport durable et réponse dans la conversation

Créer reports/QUEST-NNN.md selon le [modèle](reports/TEMPLATE.md).
Le rapport est une aide à la review : comportement avant/après, raison du choix,
3–5 points d'entrée prioritaires dans le diff, preuves, recette manuelle et limites.
Un inventaire exhaustif de fichiers ou un récit chronologique ne suffit pas.

Les liens de code doivent viser les vrais fichiers/symboles/lignes constatés.
Dans la réponse utilisateur, utiliser les chemins absolus du workspace courant.
Pour les prefabs/settings, fournir objet/champ et valeur effective ; pour les
binaires, chemin exact, source et fixture. Ne pas inventer le nom d'un bouton
ou demander « tester que ça marche ».

Publier un manifeste de preuves à evidence/QUEST-NNN/manifest.json :
ID, dates, état source (SHA et modifications non commitées identifiables),
versions, références natives, fichiers/commandes/exit codes et résultats.
Les gros logs, APK et captures peuvent rester dans les artefacts ignorés,
avec chemins et hashes dans le manifeste. Le rapport distingue liens locaux
et preuves présentes dans Git. Pas de token/identifiant sensible dans les logs.

Mettre à jour TASK-STATUS.md : implémentation, vérifications techniques et
validation manuelle restent trois champs indépendants.
Ne jamais marquer une validation manuelle acquise sur la seule base d'un APK construit.
À la reprise, vérifier que les preuves correspondent encore au code ; les
marquer obsolètes lorsqu'un changement affecte ce qu'elles démontraient.

## Format de la réponse finale

- Résultat concret en quelques phrases, avec lien vers le rapport.
- Points à reviewer : 3–5 liens commentés, chacun expliquant le risque ou la règle.
- Vérifications exécutées et résultat ; non exécutées explicitement séparées.
- À faire manuellement : étapes numérotées, résultat attendu et ce qui doit être
  retourné (OK/KO + observation), ou « aucune validation manuelle nécessaire ».
- Décisions encore attendues, s'il y en a, avec impact précis.
- État de la tâche et prochaine tâche proposée, sans l'exécuter automatiquement.

Un retour « validations 1 et 2 OK, 3 KO » met à jour ces items et déclenche
la correction ciblée de la tâche lorsqu'elle est demandée. Une tâche déjà
implémentée ne doit pas être recréée dans une nouvelle conversation.
