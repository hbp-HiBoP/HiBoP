# SCENE-004 — Préparer toutes les ressources et le Data Quest

Édition du 10 septembre 2026. Lot B.
Lire le [workflow](../TASK-WORKFLOW.md) et la
[cadence de développement](../04-migration-and-validation.md).
La fiche est un repère d’implémentation, pas une porte de qualification.

## Résultat visé

Rendre disponibles les ressources d’une visualisation complète et installer les
références standard localement sur Quest.

## Travail

- Rendre la fin de LoadMissingAnatomy attendable, avec résultat/erreur explicites.
  Une scène affichable ne doit pas être considérée automatiquement prête à exporter.
- Préparer tous les meshes, représentations et volumes disponibles dans la
  visualisation, selon l’inventaire ; inclure les autres dépendances nécessaires.
- Réutiliser les chargeurs et MNIObjects. Adapter le packaging Data et la résolution
  de chemins Android pour un chargement local utilisable par le runtime natif.
- Livrer le MNI de référence avec l’installation Quest ; couvrir les ressources
  standard nécessaires aux autres fonctionnalités, notamment les atlas concernés.
- Définir une identification vérifiable des ressources standard. Une ressource
  personnalisée non identique ne peut pas être remplacée par le MNI local.
- Prévoir partage entre colonnes/scènes et fermeture sans double libération.
  Éviter les copies inutiles de ressources immuables.
- Faire attendre l’export jusqu’à disponibilité complète. Ne pas développer
  l’envoi progressif ni un transfert spécial à la première connexion.

## Passage à la suite

Le code de préparation et le packaging sont prêts à être utilisés par le
transfert/restauration. Le test réel du chargement Android est regroupé en 008 ;
un build intermédiaire n’est justifié que par un blocage concret empêchant la suite.

Le périmètre autorisé détermine la poursuite ; aucune nouvelle permission n’est
requise à cette frontière lorsque le lot ou le chantier entier a été demandé.
