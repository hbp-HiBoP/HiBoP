# Registre des risques et conditions de révision

| ID | Risque / preuve | Conséquence | Réponse ciblée | Point de décision |
| --- | --- | --- | --- | --- |
| R01 | Copie des classes HoloLens ou de l'orchestration scientifique | Deux HiBoP divergents malgré DLL commune | Extraire la tranche dans Core, Desktop l'utilise en premier | J4/J5 : bloquer une deuxième règle de calcul |
| R02 | Transport embarqué pas encore qualifié dans Unity | Retard J2, APIs TLS/serveur indisponibles | Essai borné, métriques et cycle de vie ; P06 auxiliaire en repli argumenté | Fin QUEST-009 |
| R03 | hbp_core Android compilé historiquement mais runtime non prouvé | Crash/ABI ou écart scientifique | Build reproductible, smoke IL2CPP puis parité | QUEST-015/016 puis QUEST-019 |
| R04 | Entrées scientifiques incomplètes : volume, masque ou normalisation omis | Résultat différent sans erreur visible | Contrat préparé explicite, référence Desktop et cas limites | Avant acceptation J4/J5 |
| R05 | Repères différents entre Surface.SetBuffers et RawSiteList.AddSite | Réflexion anatomique ou distances fausses | Adaptateur commun et points asymétriques, mm scientifiques séparés de mètres VR | J2/J3 puis reconstruction J4 |
| R06 | Un instant iEEG traité comme une nouvelle plage de données | Couleurs/amplitudes réinterprétées | Transférer Middle/SpanMin/SpanMax et provenance, premier sample exact | J5 |
| R07 | UI/Resources/plugins Desktop embarqués ou activés Quest | Échec build, mémoire inutile ou appels natifs invalides | Build Report et exclusions ciblées, composition Quest propre | J1 |
| R08 | Réglage global d'entrée modifié | Régression souris ou contrôleurs | Profil effectif testé sur deux Players | J1 |
| R09 | Reprise des purges/leases de l'ancien cache | Session perdue à la déconnexion | Données détenues par session, réseau indépendant | J2 |
| R10 | Double chargement staging + natif + GPU et pool de workers | Pic mémoire, blocage rendu ou throttling | Mesurer propriété/copies, séquencer calcul et publication, régler concurrence commune | J4 |
| R11 | Annulation libérant les handles avant fin native | Crash ou corruption mémoire | Propriétaire de calcul garde les ressources jusqu'à terminaison | J4 |
| R12 | Snapshots mélangés pendant édition Desktop | Données incohérentes reçues | Capture cohérente/versionnée, copie avant travail réseau | J2/J4 |
| R13 | Transparence passthrough P05 non résolue | Contacts internes difficiles à voir | Bouton afficher/masquer validé ; différer la transparence | Réouvrir seulement à son jalon |
| R14 | Supposer Mac validé car bundle/cross-build existe | Découverte tardive problème de transport/chemins | J6 physique Apple Silicon, OS et artefacts consignés | Après Windows complet |
| R15 | Vieilles métriques promues en preuve du nouveau produit | Mauvaise décision architecture/performance | Rapports avec scope ; refaire la chaîne réelle | Chaque jalon |
| R16 | Réutilisation de formats réseau/assembly historiques trop larges | Anciennes politiques distribuées réintroduites | Sélection de fichiers/types et tests de dépendances | Revue des reprises |
| R17 | Modification d'un paramètre de grille pour accélérer Quest | Différence scientifique silencieuse | Comparer paramètres scientifiques identiques ; optimiser scheduling/rendu séparément | J4/J5 |
| R18 | Rendu Quest refait ses règles de couleur/masque | Divergence malgré calcul natif correct | Apparence scientifique commune, adaptateurs GPU seulement | J3/J5 |

Un défaut qui bloque un jalon scientifique ne rend pas automatiquement invalide
la preuve anatomique déjà acquise. Inversement, un cerveau manipulable fluide
ne valide pas le calcul scientifique local.

Les questions de conflits futurs ne sont pas un risque à résoudre par une
architecture distribuée générique maintenant. Leur point de décision est
l'introduction de commandes scientifiques modifiables depuis les deux côtés.
