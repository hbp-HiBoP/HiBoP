# Questions et arbitrages ouverts

## Usage

Ce document distingue les questions qui peuvent attendre de celles qui bloquent
un jalon. Tant qu'une réponse n'est pas obtenue, la valeur temporaire indiquée
doit être utilisée et marquée comme hypothèse dans l'implémentation.

## Q1 — Espace source des palettes

**Question :** les couleurs d'atlas, fMRI, iEEG et préférences sont-elles
historiquement définies comme valeurs sRGB destinées à un écran ?

**Valeur temporaire recommandée :** oui, sRGB.

**Pourquoi :** les couleurs éditées ou publiées sous forme de triplets RGB sont
généralement des couleurs d'affichage. Le projet Linear doit les convertir une
fois avant composition.

**Bloque :** Gate 4, contrat scientifique.  
**Preuve attendue :** patchs connus et validation d'une palette de référence.

## Q2 — GPU Intel minimal concret

**Question :** quelle génération de chipset Intel intégré, quelle RAM, quelle
résolution et quel OS représentent le minimum supporté ?

**Valeur temporaire :** machine Intel intégrée la plus faible disponible dans
le parc de test, précisément documentée.

**Bloque :** validation finale de performance, pas le prototype.

## Q3 — Définition des 30 000 sites

**Question :** s'agit-il de 30 000 sites source avant clonage par colonne, de
30 000 instances visibles totales, ou les deux selon le projet ?

**Valeur temporaire :** tester 30 000 sites source et rapporter le nombre réel
d'instances après colonnes.

**Bloque :** choix d'une refonte structurelle du renderer de sites.

## Q4 — VR

**Questions :**

- quels casques ?
- OpenXR ou autre runtime ?
- fréquence cible ?
- Windows uniquement ou casque autonome ?
- mode single-pass instanced requis ?
- contrôleurs et picking 3D concernés par cette migration ?

**Valeur temporaire :** garder l'architecture compatible OpenXR et éviter les
techniques connues comme hostiles au stéréo ; ne pas certifier la VR.

**Bloque :** Gate plateforme VR et choix final de transparence/contours.

## Q5 — WebGL

**Question :** WebGL est-il une cible de release supportée, un prototype ou une
piste sans engagement ?

**Valeur temporaire :** piste exploratoire. Documenter les incompatibilités,
mais ne pas dégrader le desktop/VR pour elle.

**Bloque :** choix définitif du renderer de sites et du wireframe ROI si WebGL
devient requis.

## Q6 — macOS et Linux de référence

**Questions :**

- macOS Intel, Apple Silicon ou les deux ?
- version macOS minimale ?
- distribution Linux, serveur d'affichage et API graphique ?
- GPUs réellement présents chez les utilisateurs ?

**Valeur temporaire :** Metal sur Apple Silicon pour le prototype macOS et API
actuelle du projet pour Linux, sans certification.

**Bloque :** Gate plateforme desktop.

## Q7 — Budget de fluidité

**Question :** quel niveau est exigé pour chaque scénario : 30, 60, 72/90 FPS
VR, ou seulement interaction sans blocage ?

**Valeur temporaire :** ne pas régresser de plus de 10 % sur l'usage courant et
calibrer ensuite un budget absolu.

**Bloque :** acceptation finale des performances.

## Q8 — Transparence cible

**Question :** faut-il reproduire exactement les artefacts de tri actuels au
premier jalon, ou peut-on adopter directement une solution plus stable si les
informations visibles restent identiques ?

**Valeur temporaire :** parité du comportement, puis modernisation séparée.

**Bloque :** uniquement si le port transparent classique produit une
régression fonctionnelle.

## Q9 — Contours

**Questions :**

- quelle épaisseur/couleur est considérée comme référence ?
- doivent-ils affecter les transparents, les coupes, les sites et les ROI ?
- doivent-ils apparaître dans tous les exports ?

**Valeur temporaire :** reproduire le scope AGM actuel, puis proposer une
modernisation A/B.

**Bloque :** Gate 3.

## Q10 — Cas scientifiques de référence

**Question :** quels projets/datasets peuvent être figés et partagés localement
pour atlas, fMRI, iEEG, coupes et gros volume de sites ?

**Valeur temporaire :** construire des fixtures synthétiques pour les tests
automatiques et utiliser les projets réels au cas par cas pour la validation
humaine.

**Bloque :** Gate 0 pour au moins un cas de chaque famille ; extension du corpus
possible pendant le portage.

## Q11 — Gestion de couleur des captures

**Question :** les moniteurs ou workflows de publication emploient-ils des
profils ICC particuliers, du HDR système ou des écrans wide gamut ?

**Valeur temporaire :** mesures sur PNG sRGB brut, écran SDR, HDR système
désactivé. La validation humaine peut utiliser les écrans habituels mais doit
indiquer leur contexte.

**Bloque :** seulement si les écarts sont visibles entre postes malgré des PNG
identiques.

## Q12 — Ombres

**Question :** les ombres du cerveau ont-elles une valeur fonctionnelle ou
seulement esthétique ?

**Valeur temporaire :** les conserver pour la parité anatomique, mais les
interdire sur les overlays scientifiques et mesurer leur coût multi-vues.

**Bloque :** choix du profil low-end, pas le prototype.

## Ordre recommandé des réponses

Pour démarrer la baseline : Q1, Q3 et Q10.  
Avant de choisir les optimisations : Q2 et Q7.  
Avant la validation multi-plateforme : Q4, Q5 et Q6.  
Avant de figer le rendu modernisé : Q8, Q9 et Q12.

