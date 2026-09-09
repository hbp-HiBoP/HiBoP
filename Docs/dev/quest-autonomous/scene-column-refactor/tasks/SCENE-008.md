# SCENE-008 — Vérifier l'intégration et préparer QUEST-024

Type : vérification finale du refactoring et transmission à la qualification.
Dépendance : [SCENE-007](SCENE-007.md) et preuves utiles de 001–006.
Suivi : [registre](../TASK-STATUS.md#scene-008).

## Reprise

Lire le [contrat local](../TASK-WORKFLOW.md), [la migration](../04-migration-and-validation.md),
les rapports locaux et [QUEST-024](../../tasks/QUEST-024.md). Le propriétaire a
approuvé l'insertion de ce chantier entre 023 et 024 ; le document historique
reste inchangé et doit être lu avec ce contexte complémentaire.

## Points d'entrée à inspecter

Deux chemins de production, format final, Players, fixtures et preuves réelles
du refactoring ; recettes de référence du prototype après 023. Les chemins et
hashes d'anciens binaires ne valent pas preuve des nouveaux.

## À réaliser

- Assembler une recette reproductible couvrant anatomie/sites/densité/iEEG dans
  les Players refactorisés, sources applicatives et natives identifiées.
- Exécuter les preuves d'intégration nécessaires pour relier Desktop réel,
  export, transport, restauration du même modèle, calcul autonome et rendu Quest.
- Consolider I01–I10 avec liens vers preuves valides, code et limites. Vérifier
  explicitement modèle/accès/opérations communs, pas seulement la parité numérique.
- Vérifier les ressources sur remplacement, déconnexion, nouvelle tentative et
  fermeture ; reprendre les mesures utiles de temps et mémoire sur la même fixture.
- Corriger seulement les défauts empêchant cette preuve ; si un problème remet
  en cause une étape, identifier la tâche et les preuves affectées.
- Fournir dans le rapport un bloc de reprise pour 024 : versions, binaires,
  fixtures, commandes, différences par rapport à 023 et contrôles encore requis.

## Hors périmètre

Pas d'exécution implicite de 024, acceptation globale du prototype, qualification
Mac/Linux ou nouvel appairage 030. Pas de nouvelle fonctionnalité ou optimisation
finale. Aucun verdict manuel ou support de plateforme déduit du seul build.

## Vérifications par l'agent

- Comparer les résultats scientifiques à la baseline et entre Windows/Quest,
  avec tolérances justifiées et preuve de calcul local après déconnexion.
- Exécuter les scénarios d'opération commune de scène et de colonne sans UI,
  puis vérifier les vrais consommateurs ; couvrir l'invalidation entre colonnes
  et les invariants de partage de ressources via les tests du modèle.
- Rejouer les scénarios d'intégration affectés : contenu invalide conservant
  l'ancien, retry idempotent, fermeture/calcul en vol, nettoyage et présentation indépendante.
- Contrôler absence de dépendance de la science aux vues/caméras, payload et
  transport ; recouper les conclusions de SCENE-007 avec le code final.
- Vérifier les preuves de non-régression Desktop et autres modalités touchées ;
  ne réutiliser une preuve ancienne que si son périmètre reste applicable.
- Identifier APK/Player/fixtures, sources et résultats réellement testés ; récupérer
  les preuves et arrêter HiBoP sur Quest conformément au contrat d'essai.

## Validation manuelle

L'acceptation globale visuelle et de confort reste portée par QUEST-024. Aucune
nouvelle validation manuelle interne n'est obligatoire ici si les retours utiles
de 003/005 sont acquis et restent applicables. Répertorier les retours encore
en attente et leur impact ; ne pas les convertir en VALIDE par agrégation.

Si une correction finale invalide un retour, fournir la recette ciblée à refaire.
Un défaut visuel connu n'est pas caché derrière l'absence d'exigence manuelle de
cette fiche.

## Rapport et critère de fin

Produire `reports/SCENE-008.md`, `evidence/SCENE-008/manifest.json` et sa ligne
locale. Inclure une conclusion explicite « prêt pour exécuter QUEST-024 » ou
« écarts à lever avant QUEST-024 », fondée sur les preuves et retours requis.

Terminé techniquement lorsque les invariants obligatoires sont établis dans les
Players et tests pertinents, Desktop est préservé et la recette de qualification
est concrète. Une preuve obligatoire absente conserve un état partiel.

Proposer ensuite QUEST-024 en joignant ce rapport comme contexte de reprise.
Ne pas la lancer, mettre à jour son statut ou modifier sa fiche automatiquement.
