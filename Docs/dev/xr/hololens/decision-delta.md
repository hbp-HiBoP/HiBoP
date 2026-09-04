# REVIEW BEFORE COMMIT — derived from closed-source prototype

# Audit HoloLens — delta vers la spécification 0.2

> **Mise à jour produit du 4 septembre 2026 :** ce document retrace le passage historique vers la spécification 0.2. D21 conserve le rendu local Quest comme baseline, mais le qualifie désormais de `PROVISIONAL` jusqu'à la revue end-to-end E11. Les formulations absolues ci-dessous ne doivent pas être interprétées comme une interdiction de réouvrir l'architecture après ce gate.

| Sujet | Position 0.1 | Preuve du prototype/courant | Correction 0.2 |
| --- | --- | --- | --- |
| architecture historique | supposée client/protocole à auditer | App Remoting vidéo/entrée, processus unique PC | déclarer qu'aucun protocole n'est réutilisable |
| projets Unity | choix encore ouvert | isolation utile mais copie divergente | deux projets Unity |
| dépôts | ouvert | repo HoloLens copié et obsolète | monorepo applicatif + repo `hbp_core` |
| partage | packages ou assemblies envisagés | aucun asmdef partagé dans le prototype | packages UPM embarqués, aucune copie |
| dépendance Quest à Core/Data | acceptable sous conditions | dépendances UI/IO/native/globals trop larges | interdire la dépendance complète, extraire contrats |
| `hbp_core` Android | faisabilité abstraite | build ARM64 réussi, runtime Quest non prouvé | baseline Desktop, spike ciblé seulement |
| protocole physique | candidats non décidés | aucun historique utilisable | baseline HTTPS + WSS, bibliothèque à spiker |
| timeline | bundle conceptuel | payload exact identifiable ; interpolation surface douteuse | contrat détaillé et test de parité obligatoire |
| coupes | résultat distant général | géométrie/base/overlay séparables | `CutRenderResult` atomique et dédupliqué |
| sites | optimisation recommandée | ancien et courant sont O(N)/GameObjects | architecture bufferisée obligatoire |
| multi-cerveaux | comportement requis | mesh cloné par colonne | asset immuable partagé + buffers mutables |
| interactions | migration Input avant intégration | OpenXR exige Input System, mais second projet isole | XR démarre Input System ; migration Desktop parallèle |
| Meta/XRI | comparaison ouverte | MRTK historique non portable ; OpenXR Meta actuel viable | XRI baseline, Meta derrière adaptateur |
| vie privée | règles générales | prototype ne persistait que préférences réseau ; Core écrit sur disque | zéro cache patient Quest explicite |
| distribution | ouverte | règles Meta évolutives et organisation à vérifier | D19 et spike de canal pilote |
| seuils | plusieurs critères vagues | aucune mesure Quest | D20 avec p50/p95/max et seuils initiaux |

## Décisions non modifiées

- desktop autoritaire ;
- rendu XR local comme baseline ; aucun second renderer vidéo avant la revue E11 ;
- passthrough par défaut avec VR de repli ;
- calculs scientifiques canoniques côté Desktop ;
- transformations spatiales locales ;
- mains et contrôleurs ;
- plusieurs visualisations ;
- tous les sites et colonnes fonctionnelles conservés ;
- aucune limite arbitraire introduite pour la performance.

## Deltas de roadmap

1. Ne pas attendre une migration Desktop complète de l'input pour démarrer les spikes du projet XR.
2. Fermer tôt les contrats de rendu et le transport IL2CPP/3 OS.
3. Valider sites et timeline avant de construire l'UX complète.
4. Traiter `hbp_core` Android comme contingency, non comme fondation.
5. Ajouter un test explicite de l'alpha temporel de surface.
