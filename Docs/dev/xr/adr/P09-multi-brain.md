# ADR P09 — instances multiples, bindings et layout local

- **Statut :** ACCEPTED — GATE P09-A–E RESOLVED
- **Date :** 2026-09-03
- **Accepté par :** propriétaire du dépôt HiBoP via l’ordre d’exécution de P09
- **Baseline inspectée :** branche `feature/xr`, commit `83b187757`
- **Décisions héritées :** D12, D14, ADR P02, P07 et P08
- **Périmètre :** lifecycle, bindings et transformations locales des `BrainInstance`; aucune interaction P13, donnée de site, timeline ou coupe

## Contexte et réouverture ciblée de P02

L’inventaire fonctionnel impose deux liaisons : une instance demandée pour une visualisation et une instance demandée pour une colonne. P02 distingue volontairement l’ID opaque de l’entité et l’ID de son scope, mais le snapshot V1 ne portait pas encore le lien entre eux. Un consommateur ne pouvait donc pas résoudre de façon déterministe une entrée de membership vers le scope qui décrit sa surface.

P09 rouvre P02 uniquement pour ajouter trois clés requises dans la paire d’applications coordonnée : `VisualizationEntity`, `ColumnEntity` et `ColumnVisualization`. Elles relient respectivement un scope de visualisation à son `visualizationId`, un scope de colonne à son `columnId`, et cette colonne à son `visualizationId`. Elles ne changent ni les propriétaires, ni les IDs, ni les révisions, ni le format de `ContractValue`. Toute absence, duplication ou contradiction rend le snapshot impropre aux bindings P09 ; le client conserve alors son dernier état cohérent.

## P09-A — bindings V1 et changement de colonne

V1 possède exactement deux bindings locaux, sans valeur « libre » ou implicite :

- `VisualizationBound(visualizationId)` suit la colonne dont `ColumnSelected == true` parmi les membres ordonnés de cette visualisation. Zéro colonne sélectionnée donne une instance valide sans buffers de colonne ; plusieurs colonnes sélectionnées rendent l’état canonique invalide. Un changement de sélection Desktop conserve l’instance, son ID et son layout, et remplace seulement les buffers mutables de colonne lorsqu’ils seront raccordés par P10/P11.
- `ColumnBound(visualizationId, columnId)` reste épinglé à cette colonne tant que la visualisation existe et que son membership contient cette colonne. Un changement de sélection Desktop ne le rebinde pas.

Un rebind demandé sur le Quest remplace localement le binding de la même instance, sans commande Desktop et sans modifier sa transformation. Le nouveau binding est validé contre un snapshot canonique complet. Le changement n’est publié qu’après acquisition de la nouvelle surface ; un asset absent laisse le binding et le rendu précédents intacts.

La surface et sa représentation viennent toujours du scope de visualisation. Le scope de colonne ne sélectionne jamais une autre topologie par convention.

## P09-B — création explicite

Aucune instance n’est créée automatiquement. Le snapshot expose les visualisations et colonnes disponibles, puis une action XR explicite crée une instance avec un nouvel `brainInstanceId` propre à l’epoch. Cette règle permet plusieurs instances du même binding et évite qu’un snapshot ou une reconnexion change le layout par effet de bord.

## P09-C — fermeture de l’autorité Desktop

La disparition de la visualisation ferme toutes ses instances `VisualizationBound` et `ColumnBound`. La disparition d’une colonne ou son retrait du membership ferme seulement ses instances `ColumnBound`; une instance `VisualizationBound` reste ouverte et suit la nouvelle sélection valide ou l’état sans colonne sélectionnée.

La fermeture est atomique du point de vue du registry : l’instance est retirée, son renderer libère explicitement le lease P08, puis son layout est supprimé. Le résultat de réconciliation retourne les IDs fermés et la cause ; une perte de layout n’est donc jamais silencieuse. Une réouverture Desktop nécessite une nouvelle demande XR et produit un nouvel ID/layout, sans résurrection fantôme.

## P09-D — propriétés locales et partagées

- locales à l’instance et en mémoire Quest : position, rotation, échelle uniforme, visibilité, focus et disposition ;
- canoniques au scope Visualization : asset/représentation anatomical ou inflated, hémisphères, edges, transparence, alpha cerveau et colormap ; toutes les instances de la visualisation observent la même valeur canonique ;
- canoniques au scope Column : sélection, paramètres scientifiques et futurs buffers mutables ; ils ne sont jamais écrits dans le `SurfaceAsset` ou le `Mesh` partagé ;
- partagés physiquement par hash : payload P08, objet `SurfaceAsset` décodé et `Mesh` P05 ;
- isolés par renderer : `Transform` et `MaterialPropertyBlock`. Aucun `Mesh`, matériau ou tableau de topologie n’est cloné pour une instance.

Anatomical et inflated restent deux assets/hash P08 distincts. Un changement canonique de représentation acquiert l’asset complet correspondant puis commute les renderers ; il n’existe ni morph local, ni override de représentation par instance en V1.

## P09-E — reprise et nouvel epoch

Une interruption ou une reprise dans le même epoch conserve registry et layout. Après commit atomique des deltas ou d’un snapshot complet du même epoch, le registry réconcilie les IDs : layouts et instances valides sont conservés, les cibles invalides sont fermées selon P09-C, et les changements d’asset sont appliqués sans modifier les poses.

Un nouvel epoch invalide tous les pseudonymes P02. Le registry ferme toutes les instances, libère tous les leases P08 et purge tous les layouts avant d’accepter le nouveau snapshot. P09 ne tente aucun remapping nominal et ne recrée rien automatiquement. La fermeture de session applique la même purge. Il n’existe aucune persistance durable du layout.

## Invariants d’implémentation

- aucune constante de cardinalité métier ;
- échelle uniforme, finie et strictement positive ; position et quaternion finis, quaternion non nul puis normalisé ;
- mutation du layout uniquement sur le Quest ; aucune commande Desktop pour pose/recentrage/scale/visibilité ;
- validation du snapshot et acquisition de la nouvelle surface avant toute mutation visible ;
- un seul owner par instance et `Dispose` idempotent ;
- métriques : instances actives, renderers, hashes distincts, meshes distincts, octets résidents P08 et draw calls attendus.

## Réouverture

Réouvrir cet ADR pour un troisième type de binding, création automatique, conservation d’un layout après invalidation de sa cible, remapping inter-epoch, représentation locale par instance, topologie dépendante d’une colonne, persistance du layout ou plafond fonctionnel d’instances.
