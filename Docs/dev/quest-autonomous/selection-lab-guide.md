# Laboratoire de sélection Quest

Le laboratoire est une application indépendante pour comparer les profils de sélection sur un MNI fixe. Il ne synchronise pas un Desktop. Le code courant annonce **Campagne v5**, champ `Build=<version produit>/selection-lab-5` ; les versions des anciennes campagnes restent attachées à leurs mesures.

## Construire et utiliser

Suivre le [workflow Quest](../../../.agents/skills/hibop-unity-validation/references/quest.md), activer le profil Quest puis `HiBoP > Quest > Build selection laboratory`. La scène est `QuestSelectionLab`, le package `fr.crnl.hibop.quest.selectionlab`, la sortie `.artifacts/quest-selection-lab/HiBoP.SelectionLab.apk`. Le package est distinct de HiBoP. Ne pas attribuer au produit le choix du laboratoire de désactiver la projection d'activité.

| Contrôle | Usage |
| --- | --- |
| Maintenir A à droite | Sélection continue avec la sonde. |
| Méthode | Contact, tolérance de profondeur ou rayon ; ne déplace pas les sites. |
| Commencer / refaire | Démarre un essai sur la disposition courante. |
| Valider / cible suivante | Termine/avance selon la disposition ; ne signifie pas nécessairement échec d'acquisition. |
| Comparer sélection A/B | Rappelle deux configurations sauvegardées sur les mêmes sites. |
| Maintenir X à gauche deux secondes | Recentre cerveau et fenêtres ; conserver ensuite échelle/position comparables. |

La rangée R1→R8 avance automatiquement à la bonne acquisition, A maintenu. Les autres dispositions utilisent les commandes de validation. Le repère de cible peut être masqué ; noter ce réglage dans la comparaison.

## Dispositions reproductibles

[QuestSelectionLayouts](../../../Assets/Scripts/HBP/Quest/Runtime/QuestSelectionLayouts.cs) fournit six cas : site isolé, rangée de huit sites, voisins proches, deux profondeurs, groupe dense et nuage de **2 000 sites**. Le nuage utilise `System.Random(20261029)` dans la **bbox MNI**, pas dans le volume cortical. Les cibles guidées sont prises dans la moitié dégagée par la coupe ; tous les sites restent actifs dans la disposition et peuvent être explorés. Cette population ne représente pas une implantation clinique.

Comparer un seul paramètre à la fois, mêmes disposition, pose, échelle et cible. Alterner l'ordre A/B entre blocs/utilisateurs. Une courte série suggérée est 20 cibles guidées par profil dans le nuage, puis exploration libre depuis plusieurs points de vue ; elle ne remplace pas une étude ergonomique contrôlée.

## Réglages et interprétation

Le [profil courant](interactions.md#sélection-de-sites) est Depth, contact 3 mm, profondeur ±10 mm, attraction 3 mm et stabilisation 0,15. Les rayons sont des **rayons physiques**, pas des diamètres ni des millimètres anatomiques après changement d'échelle. Avec contact et attraction de 3 mm, la recherche latérale peut atteindre 6 mm avant ajout du rayon du site, tandis que le contact prioritaire reste à 3 mm. Le cône Ray dépend de sa longueur et de son demi-angle ; un site exactement derrière un autre peut demander un déplacement du point de vue.

Sphere, Halo, Cage et Stylus changent l'indication visuelle, pas les colliders scientifiques ni les paramètres d'attraction. Le preset visuel est un halo blanc de rayon 4 mm. Le stylet ajoute une tige de 40 mm dont l'extrémité correspond au point de sélection. Les couleurs disponibles sont blanc, ambre et cyan. La boule visible reste indépendante du rayon de contact.

Les offsets X/Y/Z sont physiques dans le repère aim : X à droite, Y vers le haut et Z vers l'avant. Ils déplacent ensemble curseur et origine de sélection ; les rayons des menus restent inchangés. Le défaut est Z=+30 mm. Réglages, styles et offsets sont persistés et rappelés par A/B. Le volume de diagnostic jaune n'inclut pas le rayon du site.

La stabilisation est une hystérésis de **score normalisé**, sans dwell et sans maintien hors volume. `StabilizedFrames` compte les frames où elle retient le candidat précédent ; ce n'est ni le nombre d'erreurs évitées ni un taux de succès.

## Journaux et analyse

[QuestSelectionLab](../../../Assets/Scripts/HBP/Quest/Runtime/QuestSelectionLab.cs) écrit dans `Application.persistentDataPath/selection-lab` : `settings.json` et journaux JSONL. Utiliser [Export-QuestSelectionLab.ps1](../../../Tools/Export-QuestSelectionLab.ps1) pour extraire les données. Conserver version, session, disposition, profil complet, cible, sélection, résultat, temps, transitions et indicateurs de feedback. Un champ absent d'un ancien format reste inconnu, pas zéro.

Distinguer **première acquisition de la cible**, durée jusqu'à terminaison de l'essai et motif de fin. `next-target` peut survenir après une bonne acquisition et n'est pas automatiquement un échec. Les transitions entre sites ne donnent pas un taux de faux positifs sans trajectoire et intention de référence. Une cadence moyenne proche de 72 Hz ne mesure pas la marge GPU ou les pires frames.

La campagne historique du **29 octobre 2026, v2**, comprend **173 essais / 1 093 événements**, un sujet et un ordre non contrebalancé. Les 107 événements de la première version affectés par le placement des cibles sont exclus. Ces observations ont servi aux réglages de profondeur/attraction et à l'instrumentation ; elles ne permettent pas un classement général des profils ou une preuve de supériorité statistique. Les champs de stabilisation ajoutés ensuite ne peuvent pas être reconstruits pour v2. Les données locales sont référencées sous `Logs/SelectionLab/campaign-20261029` ; leur présence dans un autre clone n'est pas garantie.

La performance des 2 000 sites en usage prolongé et l'ergonomie pour plusieurs utilisateurs restent à mesurer ; ni ce guide ni une compilation ne les déclarent qualifiées.
