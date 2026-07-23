# Checklist d'implémentation pour Codex

## 1. Avant chaque lot

- [ ] Lire `AGENTS.md` et tous les documents de ce dossier.
- [ ] Vérifier le statut Git et ne pas écraser les changements utilisateur.
- [ ] Identifier le jalon et la gate visés.
- [ ] Définir un résultat observable et un test de régression.
- [ ] Limiter le lot à pipeline, correction scientifique ou optimisation — pas
      les trois simultanément.
- [ ] Lorsque Unity est ouvert, lire d'abord les ressources Unity MCP :
      instances, editor state, project info et ressources pertinentes.
- [ ] Vérifier la console avant modification.
- [ ] Capturer ou référencer la baseline applicable.

## 2. Modification des assets Unity

- [ ] Respecter le workflow prefab-first.
- [ ] Sérialiser les références dans le prefab ou asset propriétaire.
- [ ] Ne jamais utiliser `new GameObject(...)` comme réparation d'une référence
      prefab absente.
- [ ] Vérifier les `.meta` et GUID.
- [ ] Inspecter le diff YAML pour références perdues.
- [ ] Si le converter URP est utilisé, commit préalable et scope limité.
- [ ] Vérifier tous les niveaux de qualité, pas seulement le niveau actif.

## 3. Pipeline URP

- [ ] Package compatible avec Unity 6000.5.2f1.
- [ ] URP Asset et Renderer Data explicites.
- [ ] Universal Renderer en Forward.
- [ ] Linear conservé.
- [ ] HDR/tone mapping/color grading désactivés au jalon initial.
- [ ] Main light et ombres documentées.
- [ ] Additional lights désactivées si inutiles.
- [ ] Opaque texture désactivée si inutilisée.
- [ ] Depth/depth normals activées uniquement pour les consommateurs réels.
- [ ] Références SRP obsolètes supprimées.

## 4. Shader cerveau

- [ ] Aucun Surface Shader Built-in restant sur un matériau actif.
- [ ] Extrusion reproduite.
- [ ] Trois flux UV vérifiés.
- [ ] Vertex colors vérifiés.
- [ ] Atlas vérifié.
- [ ] iEEG/fMRI/MEG vérifiés.
- [ ] 0, 1 et 20 plans de coupe vérifiés.
- [ ] Même clipping dans Forward, DepthOnly, DepthNormals et ShadowCaster.
- [ ] Variante opaque.
- [ ] Variante transparente et ordre de rendu.
- [ ] `UnityPerMaterial` CBUFFER.
- [ ] Compatibilité SRP Batcher inspectée.
- [ ] Variantes shader recensées.
- [ ] Aucune influence lumineuse sur l'overlay scientifique cible.

## 5. Couleur scientifique

- [ ] Espace source de chaque palette documenté.
- [ ] Texture couleur marquée sRGB ; texture de données marquée Linear.
- [ ] Conversion sRGB -> Linear exactement une fois.
- [ ] Patch uniforme, vertex color et texture donnent la même couleur.
- [ ] Mesh et coupe donnent la même couleur.
- [ ] Lumière/normale/caméra ne changent pas le RGB scientifique.
- [ ] Alpha testé séparément.
- [ ] Légende et shader partagent le mapping ou les mêmes tests.
- [ ] Export conserve le RGB.
- [ ] Validation humaine obtenue avant de remplacer la baseline.

## 6. Coupes

- [ ] Anatomie correcte.
- [ ] Atlas correct.
- [ ] fMRI et activité corrects.
- [ ] Filtrage texture adapté au contenu.
- [ ] Transparence correcte.
- [ ] Pas de halo lié aux passes depth/normals.
- [ ] Dirty flags caractérisés.
- [ ] Génération native non modifiée sans test de référence.
- [ ] Copies CPU/GPU instrumentées avant optimisation.

## 7. Caméras et RenderTextures

- [ ] Descripteur explicite.
- [ ] Format couleur et alpha documentés.
- [ ] Depth/stencil documenté.
- [ ] MSAA explicite.
- [ ] Pas de reallocation si taille/format inchangés.
- [ ] Ancienne texture libérée et détruite selon son ownership.
- [ ] Vue minimisée/invisible testée.
- [ ] 100 redimensionnements sans croissance persistante.
- [ ] 24 vues testées.
- [ ] 60 vues testées ou limitation documentée.
- [ ] Culling masks et layers inchangés.

## 8. Contours

- [ ] Aucune dépendance active PPv2 dans les vues migrées.
- [ ] Renderer Feature indépendante par caméra.
- [ ] Depth/normals demandées explicitement.
- [ ] Opaque, transparent et clipping testés.
- [ ] Activation/désactivation testée.
- [ ] Épaisseur à plusieurs résolutions.
- [ ] Coût 1, 24 et 60 vues.
- [ ] Export conforme.

## 9. Sites

- [ ] Shader URP initial aussi simple que le shader historique.
- [ ] Aucun URP/Lit ou URP/Unlit générique substitué sans benchmark.
- [ ] Nombre réel d'instances/renderers/colliders mesuré.
- [ ] Nombre de matériaux dynamiques mesuré.
- [ ] 30 000 sites testés.
- [ ] Sélection, survol, filtre, activité et blacklist testés.
- [ ] 8×3 testé avec sites.
- [ ] Picking préservé si instancing.
- [ ] Fallback plateforme défini si indirect draw/buffers avancés.
- [ ] Avant/après sur Intel iGPU.

## 10. ROI et transparence

- [ ] Wireframe normal/sélectionné.
- [ ] Geometry shader non supposé portable.
- [ ] Fallback défini si WebGL confirmé.
- [ ] Tri cerveau/coupes/sites/ROI testé sous plusieurs angles.
- [ ] VR testée avant validation finale de la technique transparente.

## 11. Export

- [ ] Même renderer et mêmes shaders que l'écran.
- [ ] Export individuel 2048×2048 ou résolution demandée.
- [ ] Fond `(0,0,0,0)`.
- [ ] Alpha de fond nul.
- [ ] RGB comparable à la vue équivalente.
- [ ] Export composite sur `#282828`.
- [ ] État caméra restauré via chemin sûr.
- [ ] Ressources temporaires détruites.
- [ ] PNG relu et testé, pas seulement créé.

## 12. Validation après modification

- [ ] Attendre la fin de compilation Unity.
- [ ] Lire les erreurs console avec stack trace.
- [ ] Exécuter les tests ciblés.
- [ ] Exécuter les tests de sérialisation si prefab/material/asset modifié.
- [ ] Capturer les cas A/B.
- [ ] Exécuter le benchmark proportionné au risque.
- [ ] Vérifier les plateformes concernées ou consigner ce qui reste.
- [ ] Mettre à jour le document affecté.
- [ ] Relire le diff complet.
- [ ] Consigner toute différence volontaire.

## 13. Interdictions

- [ ] Ne pas supprimer les assets Built-in avant validation du rollback.
- [ ] Ne pas modifier les palettes pour « compenser à l'œil » une mauvaise
      conversion colorimétrique.
- [ ] Ne pas activer HDR/tone mapping pour retrouver artificiellement le
      contraste.
- [ ] Ne pas conclure à un gain à partir des FPS sous VSync.
- [ ] Ne pas optimiser sans baseline.
- [ ] Ne pas accepter une moyenne d'image si une petite région scientifique est
      fausse.
- [ ] Ne pas lancer Unity CLI dans la sandbox.
- [ ] Ne jamais bloquer le main thread dans les tests async Unity.

## 14. Rapport de fin de lot

Le handoff doit contenir :

```text
Jalon / lot :
Fichiers et assets modifiés :
Comportement préservé :
Différences volontaires :
Tests exécutés et résultats :
Captures :
Mesures avant/après :
Console :
Plateformes vérifiées :
Risques ouverts :
Validation humaine requise :
Prochaine gate :
```

