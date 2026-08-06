# Roadmap de migration

## Statut

La roadmap détaillée, les travaux de chaque phase et leurs gates sont définis
dans [10-implementation-plan.md](10-implementation-plan.md). Ce document en
donne la vue d'ensemble et les dépendances.

La migration se fait sur la branche courante. Codex n'effectue aucune opération
Git ; le responsable du projet gère les HEAD utilisés pour les comparaisons.

## Principes

- une seule bascule globale vers URP, sans mode runtime double pipeline ;
- baseline Built-in enregistrée avant la bascule ;
- contrat scientifique appliqué directement dans les shaders URP ;
- optimisation structurelle uniquement après mesure ;
- aucun nettoyage destructif avant validation de toutes les plateformes ;
- chaque phase possède un résultat observable et une gate.

## Séquence

```text
Phase 0  Baseline Built-in et fixtures
   ↓
Phase 1  Fondation URP et code partagé
   ↓
Phase 2  Cerveau, coupes et bascule globale
   ↓
Phase 3  ROI, sites et sélection
   ↓
Phase 4  Multi-vues, Edges et exports
   ↓
Phase 5  Validation scientifique et performance intégrée
   ↓
Phase 6  Windows, macOS Apple Silicon et Linux
   ↓
Phase 7  Nettoyage Built-in, PPv2 et assets legacy
```

## Gates résumées

### Gate 0 — Baseline exploitable

- `visu_full_test` / `Small` capturé si disponible ;
- cas anatomie, données, transparence, Edges, ROI, sites et exports couverts ;
- performance Built-in enregistrée ;
- fixture déterministe couleur/alpha rejouable.

### Gate 1 — Fondation prête

- URP 17.5.0 et assets sérialisés ;
- inventaire de tous les matériaux actifs ;
- helpers HLSL et shaders spécialisés préparés ;
- tests couleur et RenderTextureDescriptor passants.

### Gate 2 — Bascule URP fonctionnelle

- cerveau/coupes opaque et transparent ;
- atlas et activités scientifiquement cohérents ;
- extrusion et clipping 0/1/20 ;
- sillons lisibles sans shadow maps ;
- aucun matériau actif magenta ;
- discontinuité surface/coupe non aggravée.

### Gate 3 — Objets spécialisés

- ROI wireframe barycentrique, sans geometry shader ;
- sites fonctionnels avec shader minimal ;
- picking et retour UI de sélection conservés ;
- pas de régression sites soutenue supérieure à 10 %.

### Gate 4 — Vue complète et exports

- Edges complets en opaque et silhouette en transparent ;
- sites/ROI exclus des Edges ;
- RenderTextures réutilisées et détruites correctement ;
- PNG individuel straight alpha sans halo ;
- composite et vidéo fonctionnels ;
- mémoire stable après 100 cycles.

### Gate 5 — Candidat de production

- matrice scientifique et fonctionnelle passante ;
- validation humaine de `Small` ;
- performance du cas courant égale ou meilleure visée ;
- aucune régression P95 soutenue supérieure à 10 %.

### Gate 6 — Desktops

- Windows validé ;
- macOS 12+ Apple Silicon/Metal validé ;
- Linux/Vulkan validé, ou OpenGL Core choisi par la règle de fallback ;
- shaders, palettes, Edges, ROI et exports vérifiés sur chaque plateforme.

### Gate 7 — Nettoyage

- aucune dépendance active au Built-in, PPv2 ou AGM Edge Detection ;
- aucun matériau orphelin ;
- builds propres après suppression ;
- documentation et implémentation alignées.

## Définition de fini

La migration n'est finie qu'après la Gate 7. VR, WebGL, projection directe du
volume 3D et refontes atlas/coupes restent des chantiers séparés et ne retardent
pas cette définition de fini.
