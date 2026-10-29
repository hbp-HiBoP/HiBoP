# Laboratoire autonome de sélection Quest

Le laboratoire démarre sur le vrai MNI, avec une coupe axiale à 50 %. Les cinq
dispositions contrôlées utilisent 25 sites synthétiques dans la moitié retirée.
Une sixième disposition ajoute 2 000 sites uniformes dans la bbox du cortex MNI
complet. Aucun Desktop, pairing, transfert ou jeu de données patient
n'est nécessaire. Les sites, la sélection et les gestes de manipulation utilisent
les composants communs de HiBoP.

La version courante affiche **Campagne v5**, avec `selection-lab-5` dans le champ
`Build` des journaux. Le preset de base validé utilise un halo blanc de rayon
4 mm, avancé de 30 mm selon Z, et la sélection **Préféré (sonde)**. La projection
d'activité/densité est désactivée dans le laboratoire : le MNI conserve son
aspect anatomique, y compris avec 2 000 sites. La v4 a ajouté les quatre curseurs
et les décalages réglables. La v3 a ajouté les labels réduits, le nuage de sites et
le preset préféré. La révision 2 corrige le changement
de disposition qui ne rafraîchissait pas toujours les sites visibles. Ses journaux
portent le suffixe `selection-lab-2` dans le champ `Build`. Conserver les anciens
journaux séparément : les essais de la première révision peuvent avoir été affectés
par ce décalage entre affichage et sélection.

## Démarrer

- Scène : `Assets/_Scenes/QuestSelectionLab.unity`.
- Activer le profil Quest, puis `HiBoP > Quest > Build selection laboratory`.
- APK : `.artifacts/quest-selection-lab/HiBoP.SelectionLab.apk`.
- Application indépendante : **HiBoP Selection Lab**,
  `fr.crnl.hibop.quest.selectionlab`. Elle cohabite avec HiBoP.
- Installer une fois avec `adb -s <serial> install -r <apk>`, puis ouvrir
  l'application dans le casque. Le premier démarrage installe les ressources MNI
  embarquées ; les suivants réutilisent les fichiers vérifiés.
- Si une connexion ADB Wi-Fi est nécessaire, utiliser
  `Tools/Connect-QuestAdbWifi.ps1`. Le casque doit être réveillé et porté.

## Commandes

Deux choix sont indépendants :

| Bouton | Effet |
| --- | --- |
| **Disposition**, fenêtre de campagne à droite | Change les sites affichés : 1 site, rangée de 8, 2 voisins, 2 en profondeur, groupe de 12, nuage de 2 000 |
| **Profil de sélection**, fenêtre de réglages à gauche | Change l'aide à la sélection ; les sites restent à leur place |
| **Méthode**, fenêtre de réglages à gauche | Choisit contact, tolérance de profondeur ou rayon ; les sites restent à leur place |
| **Comparer sélection A/B** | Rappelle deux jeux de réglages de sélection sur la même disposition |

Pour une première prise en main : cliquer **Disposition** jusqu'à la rangée,
choisir le profil **Référence**, puis **Commencer / refaire**. Maintenir A et
suivre le repère vert de R1 à R8. Refaire avec **Visibilité**, puis **Profondeur**.
Les réglages peuvent aussi être explorés sans commencer un essai chronométré.

Les deux sites de la disposition « en profondeur » sont volontairement alignés
depuis le point de vue initial : ils peuvent sembler n'en former qu'un. Bouger
légèrement la tête ou tourner le cerveau pour voir la différence avec les deux
voisins placés côte à côte.

Maintenir **A à droite** pour sélectionner en continu, dans les trois modes.
Relâcher conserve la dernière sélection. Les gâchettes et les deux mains
permettent de déplacer, tourner et agrandir le cerveau comme dans HiBoP.
Les fenêtres se déplacent par leur barre inférieure et peuvent être épinglées.
Le bouton de fermeture est masqué pour que les contrôles restent accessibles.
Maintenir **X à gauche pendant deux secondes** recentre le cerveau et les fenêtres
devant la position courante ; la manette gauche doit être suivie. Le bouton
**Restaurer pose initiale** sert à retrouver la pose du début de session pour les
comparaisons, même après un recentrage.

La fenêtre gauche contient trois pages de sélection, une page **Curseur** (page 4)
et une page de notes (page 5). Cliquer une valeur ouvre le
pavé numérique ; les distances sont en **millimètres physiques dans le casque**,
la portée du rayon en mètres, son demi-angle en degrés. Les contrôles sans effet
dans le mode courant sont grisés. Appliquer un réglage termine l'essai en cours
et exige de relâcher puis réappuyer A.

| Profil | Différence par rapport à la référence |
| --- | --- |
| Référence | Contact strict, rayon de contact et de boule de 0,5 mm, vibration actuelle |
| Visibilité | Contour, liaison et nom du site touché |
| Profondeur | Visibilité + tolérance de ±10 mm suivant l'axe tête–sonde |
| Stabilité | Profondeur + marge de changement de 15 % du score normalisé |
| Rayon | Rayon de 1,5 m, assistance locale de 3 mm et demi-angle de 1° |
| Préféré (sonde), base validée | Profondeur ; Contact 3 mm ; Boule visible 1 mm ; Profondeur 10 mm ; Attraction 3 mm activée ; Stabilité 15 % activée ; nom seul ; halo blanc 4 mm ; Z +30 mm |

**Taille label**, page 1, vaut désormais 40 % de la taille initiale (division par
2,5). Utiliser 50 % pour une division par 2, ou 33 % pour environ 3. Le réglage
est accessible lorsque l'affichage du nom est activé.

Les tailles Contact et Boule visible sont des rayons, pas des diamètres. Avec
Contact 3 mm et Attraction 3 mm, le rayon latéral de recherche atteint 6 mm avant
ajout du rayon du site ; le contact direct prioritaire reste à 3 mm. La boule
visible ne participe pas au calcul. En mode Rayon, le demi-angle ajoute une
tolérance croissant avec la distance : 1° ajoute environ 8,7 mm à 50 cm.

Les réglages personnalisés permettent d'activer séparément les aides visuelles,
l'attraction bornée, la stabilisation et la vibration. La taille de la boule
visible est indépendante du rayon de contact. Aucune loupe ni commande de
départage joystick n'est incluse.

L'assistance ne choisit que dans son volume borné. Un contact direct reprend
toujours la main ; la stabilisation ne retient jamais un site sorti du volume.
Le rayon ne garantit pas l'accès à un site exactement derrière un autre depuis
le même point de vue : déplacer la tête ou la main doit permettre de le départager.
Cette limite fait précisément partie du test.

Le diagnostic jaune montre le volume de la sonde ou du cône, sans inclure le rayon
propre des sites (le contact additionne les deux volumes). Les aides cyan montrent
le site retenu et la liaison avec la position réelle de la sonde. Le repère vert
indique la **cible demandée**, indépendamment du site retenu. Masquer ce repère
après identification permet d'évaluer la sélection sans cet indice supplémentaire.
Le mode Référence conserve l'anneau de sélection normal de HiBoP.

Enregistrer A et B puis utiliser **Comparer A/B** pour rappeler alternativement
leurs valeurs sans modifier la pose du cerveau ou l'exercice. Les réglages et les
deux configurations sont sauvegardés entre lancements. Le diagnostic est inclus
dans la configuration : le désactiver avant une comparaison mesurée.

## Protocole conseillé

Faire d'abord quelques minutes d'exploration libre. Utiliser le diagnostic pour
comprendre les volumes et ajuster les valeurs, puis l'éteindre. Pendant les blocs
mesurés, ne pas modifier la configuration. Garder le même état du repère vert et
la même pose pour toutes les comparaisons.

La fenêtre droite donne la consigne, la cible et le site actuellement sélectionné.
**Commencer/refaire** efface la sélection et démarre l'horloge. **Valider** clôt
l'essai en enregistrant le site réellement sélectionné, même s'il est incorrect.
**Cible suivante** passe à la suivante ; **Abandonner et continuer** enregistre
explicitement l'abandon. Un changement de mode, de disposition, une perte de
focus ou une pause interrompt l'essai avec un motif distinct.

| Disposition | Actions à répéter | Critères à noter |
| --- | --- | --- |
| Site isolé S1 | L'atteindre depuis devant, puis derrière, trois fois par approche | Tâtonnements en profondeur, temps du premier contact |
| Rangée R1–R8 | Un parcours complet en gardant A, trois fois | Fluidité, détours, sites involontaires, sites manqués |
| Voisins N1/N2 | Six alternances, un essai par cible | Facilité à quitter un site, attraction du voisin |
| Profondeur D1/D2 | Six alternances ; essayer d'abord depuis la pose initiale, puis en bougeant légèrement la tête | Accès au site arrière, stratégie de correction |
| Groupe dense C1–C12 | Parcourir les cibles, avec au moins six cibles par profil | Erreurs finales et correction d'une mauvaise sélection |
| Nuage aléatoire U1–U2000 | Tester 20 cibles guidées par profil, puis explorer librement et changer de point de vue | Voisins très proches, occlusion, facilité à quitter un site, fluidité |

Le nuage utilise toute la bbox, sans espacement minimal ni rejet des positions
extérieures au maillage cortical. Une graine fixe `20261029` conserve les mêmes
positions pour toutes les comparaisons. Les 2 000 sites restent actifs dans ce
scénario ; seules les cibles guidées sont choisies dans la moitié dégagée par
la coupe. Les autres sites restent présents comme distracteurs ou sites occultés.
La bbox n'est pas le volume anatomique exact du cerveau.

Pour distinguer profondeur et stabilité, partir du preset préféré puis enregistrer
dans A la même configuration avec Stabilité désactivée, et dans B avec Stabilité
activée. Comparer A/B sans autre changement. La stabilité ajoute une marge au
choix du candidat ; elle ne change pas le volume de recherche et ne retarde pas
la sélection. Un contact direct garde priorité sur elle.

Pour la rangée, chaque cible correcte fait avancer automatiquement le repère
pendant le même maintien de A ; R8 termine le parcours. Il n'y a pas de validation
intermédiaire dans un menu : **Valider** et **Cible suivante** sont grisés.
**Commencer / refaire** repart toujours de R1. Les événements conservent aussi les sites traversés
hors de la séquence demandée.

Ajouter deux vérifications qualitatives sur S1 ou les voisins : tenir cinq secondes
sur un site avec les petits mouvements naturels de la main, puis sélectionner
volontairement son voisin. Ensuite refaire les cas difficiles après rotation et
agrandissement. **Restaurer pose initiale** revient exactement à la pose enregistrée
au démarrage de cette session, sans changement des réglages.

Comparer d'abord Référence/Visibilité, puis Visibilité/Profondeur, puis
Profondeur/Stabilité. Comparer ensuite le meilleur profil de sonde au Rayon.
L'attraction est une comparaison supplémentaire à activer seule sur le meilleur
profil de sonde. Alterner l'ordre A/B entre blocs ou utilisateurs pour réduire
l'effet d'apprentissage. Ne pas mélanger recherche de réglages et mesures finales.

Après chaque bloc, renseigner page 5 les notes **Facilité**, **Confiance** et
**Effort**, de 1 à 5. Pour l'effort, 5 signifie un effort élevé. Chaque clic est
journalisé ; conserver la dernière note de chaque bloc lors de l'analyse.

## Comparer les curseurs (v4)

Conserver le profil **Préféré (sonde)** et la disposition **2 000 sites
aléatoires**, puis ouvrir **Réglages 4/5 — Curseur** à gauche.

| Variante | Proposition et observation attendue |
| --- | --- |
| Sphère bleue | Référence visuelle précédente, sans repère ajouté |
| Halo contrasté | Anneau face aux yeux, couleur claire et bord noir ; repérer le centre sans agrandir le contact |
| Cage 3D | Trois anneaux orthogonaux liés à la manette ; lire l'orientation et le volume par la stéréoscopie et le mouvement |
| Stylet court | Halo et tige de 40 mm suivant le rayon ; son extrémité est exactement le point de sélection |

Le preset validé utilise un halo blanc de rayon 4 mm. Le rayon des anneaux reste
réglable de 2 à 20 mm pour les autres comparaisons.
Blanc, ambre et cyan sont comparables sur le même fond ; le contour noir demeure.
Ces repères sont opaques, avec test de profondeur : ils ne s'affichent pas à
travers le cerveau. Ils n'ont aucun collider et ne modifient ni Contact,
ni Attraction, ni Profondeur, ni Stabilité. La boule centrale conserve son rayon
réglable indépendamment des anneaux. Tous les repères sont affichés seulement
pendant le maintien de A, comme le curseur précédent.

Les décalages sont des millimètres physiques dans le repère du rayon : **X positif
vers la droite**, **Y positif vers le haut**, **Z positif vers l'avant**. Le haut
suit l'orientation de la manette autour du rayon. Le bouton `+/-` du pavé autorise
les valeurs négatives ; chaque axe accepte de −300 à +300 mm. Le point qui cherche
les sites et le centre du curseur se déplacent ensemble. En méthode Rayon, son
origine de sélection se déplace aussi ; les rayons des menus restent inchangés.

**Position rapide** fait défiler origine, position haute (+30 mm Y, +20 mm Z),
puis position avancée (+60 mm Z). La position haute est un point de départ à
ajuster dans le casque ; elle n'est pas un étalonnage du bord de la manette.
**Annuler les décalages** remet les trois axes à zéro.

Comparer d'abord les quatre formes avec décalages nuls, même pose du cerveau et
mêmes tailles. Rechercher le curseur au milieu du nuage, approcher un site,
tenir quelques secondes puis passer volontairement au voisin. Refaire sur un
fond cortical clair, dans l'espace dégagé et après rotation. Noter la visibilité,
la lecture devant/derrière et l'éventuel masquage des petits sites par le repère.
Ensuite comparer origine et position haute avec la meilleure forme, puis ajuster
Y et Z séparément. Vérifier qu'un mouvement de la tête ou une rotation de la
manette ne provoque pas de décalage inattendu du centre actif.

Les formes, couleurs, tailles et décalages sont sauvegardés et rappelés par A/B.
Changer de profil de sélection conserve ces choix de curseur. **Réglages de base
validés** restaure l'ensemble préféré, halo blanc 4 mm et Z +30 mm inclus. Le profil
**Référence** reste disponible dans le sélecteur des profils. Les réglages courants
et A/B déjà sauvegardés restent conservés au démarrage.

## Curseur intégré à HiBoP Quest

HiBoP Quest utilise directement ce même preset, sans démarrer le laboratoire :
maintenir A à droite active la sonde, avec le centre de sélection avancé de 30 mm
dans le repère du rayon. Le halo et le nom du site utilisent le prefab partagé
`Assets/Prefabs/Quest/3D/QuestSelectionFeedback.prefab`. Les gestes de manipulation,
les rayons des menus et les événements de sélection vers le Desktop utilisent
leur chemin habituel. La projection scientifique de l'application connectée reste
pilotée par la session ; seule la scène du laboratoire impose son absence.

## Résultats

Les journaux v4 enregistrent également le style du curseur (`Sphere`, `Halo`,
`Cage`, `Stylus`, valeurs 0 à 3), la couleur (`White`, `Amber`, `Cyan`, valeurs
0 à 2), le rayon des anneaux et les trois décalages dans `Settings`.
L'export CSV ajoute ces paramètres en colonnes, avec les distances converties
en millimètres ; les anciennes versions sans ces champs restent vides.

Le dossier `Application.persistentDataPath/selection-lab` contient :

- `settings.json` : réglages courants et configurations A/B ;
- un fichier `.jsonl` par lancement : débuts/fins d'essais, transitions de
  sélection, cibles successives, notes, valeurs complètes, pose/échelle du cerveau,
  position de la tête et durée moyenne des images.

Les journaux v3 ajoutent `TargetCue`, `RandomSeed` et `StabilizedFrames`. Ce dernier
compte les images où l'ancien candidat a été conservé face à un voisin grâce à la
marge de stabilité. Ce n'est pas un nombre d'erreurs évitées. Les versions anciennes
ne mesuraient pas ce compteur ; leurs colonnes CSV correspondantes restent vides.

Récupérer les résultats et produire un CSV des fins d'essais :

```powershell
.\Tools\Export-QuestSelectionLab.ps1 -Serial <serial>
```

Le temps total d'un essai inclut le geste de validation. `FirstTargetSeconds`
donne le temps de la première acquisition correcte, ou -1 si elle n'a jamais eu
lieu. Pour la rangée, les événements `sequence-target` et `selection` permettent
de reconstruire les temps par cible. Une transition de sélection n'est pas à elle
seule une erreur : distinguer parcours transitoire, erreur finale, abandon et essai
interrompu. Les pauses, réglages et réinitialisations ne sont pas des échecs de
sélection. La durée moyenne des images est indicative et ne remplace pas un
profilage de performances.

Une session correspond à un identifiant horodaté. Noter séparément quel utilisateur
a réalisé chaque session. Les changements de réglages déjà prévus ne demandent
ni rebuild ni réinstallation. Un nouvel algorithme demande un nouveau build.

Terminer la campagne par une validation courte des profils retenus dans HiBoP
connecté au Desktop, notamment pour les changements de colonne et la synchronisation.
