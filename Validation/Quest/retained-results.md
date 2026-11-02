# Résultats de référence Quest

La [synthèse de validation](../../Docs/dev/quest-autonomous/validation.md) explique
les résultats, les choix techniques et leurs limites. Ce dossier contient les
comparaisons finales qui permettent d'en vérifier les chiffres. Ces campagnes de
septembre 2026 ne qualifient pas le code courant.

## Lire les résultats par sujet

| Sujet | Contexte et résultats principaux | Portée |
| --- | --- | --- |
| Densité | [Contexte](scientific-parity/density/measurement-context.json), [comparaisons](scientific-parity/density/comparison-results.json), [acceptation numérique](scientific-parity/density/numerical-acceptance.json), [validation casque](scientific-parity/density/device-validation.json) | Neuf variantes, deux répétitions par plateforme ; 18 comparaisons Windows/Quest. Acceptation du 9 septembre 2026. |
| iEEG | [Contexte](scientific-parity/ieeg/measurement-context.json), [comparaisons](scientific-parity/ieeg/comparison-results.json), [acceptation numérique](scientific-parity/ieeg/numerical-acceptance.json), [provenance des entrées](scientific-parity/ieeg/fixture-provenance.json), [validation hors ligne](scientific-parity/ieeg/offline-device-validation.json) | 18 fixtures, deux répétitions par plateforme ; 36 comparaisons Windows/Quest. Acceptation du 10 septembre 2026. |
| Scènes multimodales | [Contexte](scientific-parity/multimodal-scenes/measurement-context.json), [comparaison MNI](scientific-parity/multimodal-scenes/mni-comparison-results.json), [comparaison patient synthétique](scientific-parity/multimodal-scenes/synthetic-patient-comparison-results.json), [acceptation numérique](scientific-parity/multimodal-scenes/numerical-acceptance.json), [validation casque](scientific-parity/multimodal-scenes/device-validation.json) | Six modalités, 72/78 comparaisons, 12 textures de coupe par fixture. Binaires révision 2, USB uniquement ; acceptation du 14 septembre 2026. |

Pour les scènes multimodales, les [binaires](scientific-parity/multimodal-scenes/player-provenance.json)
et la [recette du patient synthétique](scientific-parity/multimodal-scenes/synthetic-patient-provenance.json)
définissent le périmètre exact. Les répétitions finales sont conservées parce
qu'elles établissent des propriétés distinctes de la première comparaison :

| Propriété | MNI | Patient synthétique |
| --- | --- | --- |
| Répétition à chaud sur Quest | [Comparaison](scientific-parity/multimodal-scenes/mni-warm-repeat-comparison.json) | [Comparaison](scientific-parity/multimodal-scenes/synthetic-patient-warm-repeat-comparison.json) |
| Répétition après coupure | [Comparaison](scientific-parity/multimodal-scenes/mni-offline-repeat-comparison.json) | [Comparaison](scientific-parity/multimodal-scenes/synthetic-patient-offline-repeat-comparison.json) |
| Comptages de ressources et mémoire | [Mesures MNI et Desktop](scientific-parity/multimodal-scenes/mni-and-desktop-resource-measurements.json) | [Mesures patient corrigé](scientific-parity/multimodal-scenes/synthetic-patient-resource-measurements.json) |

Les mesures de ressources conservent aussi le reliquat de meshes Desktop. Trois
passages stables sur Quest ne démontrent pas une stabilité en endurance. Les
observations manuelles et les acceptations restent séparées des écarts numériques.
Le relevé MNI et Desktop contient également la première cohorte patient Windows ;
pour la recette patient corrigée, utiliser le relevé patient dédié. Ces cohortes
ne doivent pas être fusionnées.

## Fixtures reproductibles

Les entrées réutilisables sont conservées à côté des résultats de validation :

| Fixture | Contenu et génération |
| --- | --- |
| [Anatomie MNI](fixtures/mni-anatomy/anatomy-fixture.md) | Projet minimal et visualisation anatomique ; génération d'une archive déterministe à partir des références MNI du dépôt. |
| [Contacts synthétiques MNI](fixtures/mni-contacts/contacts-fixture.md) | Deux patients synthétiques et huit contacts aux identités et coordonnées connues ; entrées utilisées par les tests et le générateur iEEG. |
| [iEEG synthétique](fixtures/synthetic-ieeg-fixture.md) | Recette de génération des signaux BrainVision, du protocole et de l'archive ; valeurs attendues et conditions de chargement. |

Les manifestes des fixtures décrivent les sources, leurs empreintes et les entrées
des archives. Ils sont lus par le générateur et restent des données opérationnelles.
Les identifiants sérialisés et noms d'artefacts contenant d'anciens numéros de tâche
restent stables pour les tests et lanceurs existants ; les guides décrivent leur
fonction sans nécessiter la connaissance de ces tâches. Les fichiers générés sont
locaux et ne sont pas ajoutés aux résultats historiques qualifiés.

## Chargement natif Android

Le [contrôle élémentaire de `hbp_math`](native-runtime/math-abi-comparison.json),
son [contexte de build](native-runtime/math-android-build-context.json) et sa
[provenance](native-runtime/measurement-context.json) complètent les comparaisons
de scènes. Conservés de la campagne du 11 septembre 2026, ils portent sur
**17 exports publics** et quatre résultats élémentaires : erreur standard de la
moyenne, corrélation de Pearson et deux tests de Wilcoxon. Les écarts observés
Windows/Android sont tous nuls sur ces entrées. Cela ne définit ni une tolérance
universelle, ni une qualification Unity, ni une qualification scientifique complète.
Le hash de la bibliothèque comparée correspond à celui du contexte de build.

## Connectivité USB et réseau

La [qualification Windows 11 / Quest 3 du 27 octobre 2026](connectivity/windows-quest3-qualification-2026-10-27.md)
conserve les conditions d'essai, les délais observés de repli/reprise et les limites
de la recette NCM. macOS, Linux et Quest 3S ne sont pas qualifiés par cette campagne.
Le refus du consentement NCM n'a pas été exercé. Ce rapport est une référence
documentaire, sans contrôle bloquant de publication dans la CI.

## Provenance et fichiers historiques

Les 21 fichiers de résultats, d'acceptation et de provenance ont été déplacés et
renommés **sans modifier leurs octets**. Chaque `measurement-context.json` associe
leur nom actuel à leur ancien chemin et à leur SHA-256 dans `retainedFiles`.
Pour les campagnes de parité scientifique, il extrait aussi le contexte utile du manifeste d'origine : versions, état source
(y compris les fichiers non commités), identité des binaires et portée. Pour les
scènes multimodales, il conserve le rapprochement entre source finale et source
compilée, avec la seule différence de fin de ligne documentée.
Pour le contrôle natif, la source et la chaîne de compilation sont directement
décrites dans le fichier de contexte de build d'origine.

Les chemins et commandes à l'intérieur des enregistrements d'origine restent
historiques. Un ancien nom de fichier se résout par `retainedFiles` ; il ne désigne
pas nécessairement un fichier voisin portant encore ce nom. Les autres chemins
vers `.artifacts`, `.test-results` ou une machine locale ne garantissent pas la
disponibilité des gros buffers, APK et logs. Les comparaisons conservées permettent
de vérifier les agrégats par cas, pas de recalculer tous les écarts sans les buffers
originaux. Aucune mesure n'a été recréée pendant ce classement.

Les identifiants de tâches restent uniquement des clés de provenance : densité
`QUEST-019`, iEEG `QUEST-023`, scènes multimodales `QUEST-024`. Les essais remplacés,
rapports de compilation, captures redondantes et journaux intermédiaires ont été
retirés du checkout. Leurs conclusions encore utiles sont dans la documentation.

### Retrouver une ancienne preuve

Le commit `003edbcf53ac2697525dee688b2a9be082159794` contient tous les artefacts
versionnés avant cette consolidation. Ce repère d'archivage n'est **pas** le commit
d'exécution de chaque campagne : celui-ci est enregistré dans son contexte.
Depuis la racine du dépôt, par exemple :

```powershell
git show 003edbcf53ac2697525dee688b2a9be082159794:Docs/dev/quest-autonomous/evidence/QUEST-009/manifest.json
```

La même commande permet de consulter les manifestes des premières fixtures
(`QUEST-001`, `QUEST-013`) et de la livraison TLS (`QUEST-010`), ou un rapport sous
`Docs/dev/quest-autonomous/reports/`. Les traces qui n'étaient jamais versionnées
ne deviennent pas disponibles par cette méthode.

Les anciens artefacts sous `Docs/dev/quest-autonomous/scene-column-refactor/evidence/`
et les deux CSV de `Docs/dev/quest-autonomous/sync/M2-Small-inputs/` sont également
consultables à ce commit. Les comparaisons Editor et la première publication de
scène des 10–11 septembre étaient partielles ; la qualification multimodale du
14 septembre conservée ici fournit la référence matérielle plus aboutie. Les
états de pause/reprise, sauvegardes de déploiement, builds intermédiaires et modèle
de manifeste n'ajoutent pas de qualification à ces résultats.

Les mesures de synchronisation et leurs limites sont dans la
[référence de validation](../../Docs/dev/quest-autonomous/sync/synchronization-validation.md).
Les anciens rapports et listes de tâches sous `Docs/dev/quest-autonomous/sync/`
restent consultables par la même commande ; les noms historiques servent à
retrouver une source, pas à organiser la documentation courante.

## Ajouter une nouvelle référence

Conserver une mesure lorsqu'elle justifie une conclusion, un choix ou une limite
encore utile. Documenter contenu, paramètres, matériel, versions, transport,
conditions de cache, répétitions, unités et critères d'acceptation. Distinguer
la mesure, son interprétation et la validation manuelle ; garder aussi les échecs
qui expliquent une limite. Remplacer une référence dépassée plutôt qu'accumuler
toutes les étapes de développement.

Les mesures de transfert et de Display sont déjà documentées dans la
[synthèse](../../Docs/dev/quest-autonomous/validation.md#transfert-small--septembre-2026).
Cette sélection ne crée pas de fichiers de mesures brutes à partir de leurs
résumés. Les prochaines campagnes pourront ajouter ces sujets avec leurs
véritables résultats et leur contexte.
