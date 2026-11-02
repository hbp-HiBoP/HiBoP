# Interactions Quest

Référence du code courant au 2 novembre 2026. Les observations ergonomiques et performances mesurées restent des résultats datés, décrits dans [validation.md](validation.md) et le [laboratoire de sélection](selection-lab-guide.md).

## Présentation et commandes communes

Le placement et l'échelle physiques des cerveaux, fenêtres et curseurs appartiennent au Quest. Sélection, coupes et paramètres scientifiques passent par les setters et commandes communs. Manipuler un cerveau sélectionne son contexte de scène/colonne ; cela ne transporte pas sa pose dans la pièce vers Desktop. Les modes localement regroupé/séparé et les changements de colonne sont gérés par [QuestAnatomyView.Presentation](../../../Assets/Scripts/HBP/Quest/Runtime/QuestAnatomyView.Presentation.cs) : les captures sont invalidées pendant les transitions, sans réinitialiser les paramètres scientifiques des autres colonnes. Le [contrat de scène](shared-scientific-scenes.md) précise visibilité et sélection.

[QuestPointerInput](../../../Assets/Scripts/HBP/Quest/Runtime/QuestPointerInput.cs), [QuestInteractionInput](../../../Assets/Scripts/HBP/Quest/Runtime/QuestInteractionInput.cs) et [QuestInteractionPolicy](../../../Assets/Scripts/HBP/Quest/Runtime/QuestInteractionPolicy.cs) centralisent l'arbitrage : gâchette d'index pour UI/manipulation anatomique, grip pour coupe. Une pression capture une seule cible par main jusqu'au relâchement. Une perte de tracking ou une invalidation termine le geste ; une nouvelle pression est nécessaire. Le rayon UI par défaut porte à 3 m, avec priorité à la proximité. La sélection de sites utilise son propre profil physique décrit ci-dessous, pas l'ancien rayon de sonde de 0,5 mm des premiers essais.

## Fenêtres et signaux

[QuestWindowsManager](../../../Assets/Scripts/HBP/Quest/Runtime/QuestWindowsManager.cs) instancie les prefabs du catalogue et interdit deux fenêtres de même clé. [QuestWindow](../../../Assets/Scripts/HBP/Quest/Runtime/QuestWindow.cs) sépare ouverture, épinglage, survol et manipulation ; fermer masque contenu/raycast/canvas sans interrompre la connexion. Les fenêtres sont **épinglées par défaut**. Quand le suivi est activé, il respecte le survol et la manipulation, avec hystérésis ; un rappel replace la fenêtre devant la tête.

Déplacement, redimensionnement et zoom du contenu sont des opérations locales. Le redimensionnement modifie le `RectTransform` avec bord opposé conservé ; il ne consiste pas à agrandir les glyphes par l'échelle du GameObject. Prefabs et références sérialisées sont la source de la structure UI. Les éléments/états du thème Quest sont propres à cette présentation ; des paramètres graphiques communs peuvent être partagés sans coupler l'état des contrôles Desktop.

Les panneaux de signaux utilisent les données et présentations communes, avec leur propre ownership de calcul et de ressources. La recette Small du 28 octobre 2026 constitue une observation ciblée du rendu dans le casque ; elle ne qualifie pas tous les protocoles, toutes les densités de courbes ou toutes les durées d'usage. Les anciens plans affirmant que la fenêtre de signaux ou le contexte multi-scène restent à implémenter sont dépassés.

Masquer la surface peut conserver les sites et la prise anatomique ; ce masque de présentation n'est pas le masque scientifique. Sur Desktop, une asymétrie préexistante reste visible dans `Camera3D` : `HorizontalStrafe` multiplie la vitesse par 0,2, `VerticalStrafe` par 1. Ce rapport de cinq ne provient pas de la migration Input System ; aucune correction n'est revendiquée ici.

## Sélection de sites

Les tailles de sélection sont exprimées en **mètres du monde physique**, indépendamment de l'échelle anatomique affichée. [QuestSelectionSettings](../../../Assets/Scripts/HBP/Quest/Runtime/QuestSelectionSettings.cs) définit les profils Contact, Depth et Ray. Le profil courant par défaut est le profil 5 :

| Paramètre | Valeur |
| --- | --- |
| Mode | Depth |
| Rayon de contact / marqueur | 3 mm / 1 mm |
| Profondeur avant et arrière | 10 mm de chaque côté |
| Attraction | Activée, rayon 3 mm |
| Stabilisation | Activée, marge de score 0,15 |
| Curseur | Halo blanc, rayon 4 mm |
| Décalage dans le repère aim | X=0, Y=0, Z=+30 mm |
| Indice visuel | Nom activé, échelle du libellé 0,4 ; autres indices visuels désactivés |

[QuestSiteAssistance](../../../Assets/Scripts/HBP/Quest/Runtime/QuestSiteAssistance.cs) donne priorité au contact direct. Depth utilise une capsule centrée sur la sonde et orientée selon l'axe tête–sonde : le segment va de −10 à +10 mm autour de la sonde avec le profil courant, hors bouts arrondis. Ray utilise le rayon borné et éventuellement son cône d'attraction. Un candidat est classé par distance normalisée, avec un terme de distance et départage par IDs stables. Le site précédent est retenu seulement s'il reste dans le volume et dans la marge de score. **0,15 est une marge de score**, pas une probabilité, une durée de maintien ou un taux d'erreur. Il n'y a pas de sélection balayée entre deux frames ni de conservation indéfinie hors volume. À 50 cm, un demi-angle de 1° ajoute environ 8,7 mm au rayon : le réglage doit être interprété physiquement.

L'éligibilité suit l'apparence commune : les sites masqués/invisibles ne deviennent pas de nouvelles cibles. Une sélection canonique déjà retenue peut rester sélectionnée après devenir invisible. Les noms répétés entre patients ne sont pas des identifiants. Le routage commun et la boundary répliquent une intention de site cohérente avec sa colonne ; la présentation immédiate reste locale, et le driver sérialise les propositions avec une seule dernière intention différée. Les checkpoints et corrections canoniques prévalent sur les réponses anciennes.

## Coupes

[QuestCutCommands](../../../Assets/Scripts/HBP/Quest/Runtime/QuestCutCommands.cs) ne modifie que l'intention du contrôle : chaque opération relit orientation, flip, position et normale courants pour préserver les champs modifiés ailleurs. Une normale custom doit être finie et non nulle ; la position est bornée à [0,1]. Les commandes passent par `SetCutDefinition`, avec identité de coupe stable et portée de scène.

La fenêtre expose les coupes et peut suspendre les aides sans supprimer les données. Fermer la fenêtre arrête le geste et les aides. [QuestCutHandles](../../../Assets/Scripts/HBP/Quest/Runtime/QuestCutHandles.cs) capture la **surface bornée du plan depuis ses deux faces**, au grip ; elle contraint le mouvement à la normale anatomique. La proximité est mesurée depuis grip/aim et un seul candidat reçoit la capture. Seuils du profil : contact 35 mm, apparition 180 mm, disparition 240 mm. Ce ne sont pas des distances anatomiques après agrandissement du cerveau.

Une même coupe n'est pas modifiée par deux mains simultanément. Une coupe saisie verrouille sa représentation anatomique ; une représentation déjà manipulée refuse le démarrage de ce geste. Suppression, fermeture, transition ou changement incompatible invalide la capture. Les guides et leur pose restent locaux ; les dernières définitions valides sont déjà publiées pendant le geste, sans republier au relâchement une copie figée qui écraserait une modification distante.
