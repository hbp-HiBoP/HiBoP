# QUEST-030 — Gel Desktop pendant la préparation de Small

Statut : **CLOS — acceptation fonctionnelle du propriétaire le 2026-09-15**.

## Reproduction utilisateur

Le 15 septembre 2026, appairage réussi puis envoi de `visu_full_test / Small`
avec `auto compute activity` désactivé. Le Desktop reste longtemps bloqué sur
la préparation ; le propriétaire arrête le player. Le Quest ne reçoit rien.

## Diagnostic observé

Le répertoire temporaire interrompu contient 22 834 buffers numériques (dont
22 790 de moins de 4 Kio), trois volumes NIfTI, un buffer temporaire et un JSON
incomplet de 15 433 728 octets. Aucune archive finale n’est encore créée.
Les écritures des buffers s’étalent sur 190,76 secondes avant l’arrêt.
Le JSON partiel contient 51 805 références à seulement huit identités globales.

`GlobalReferenceConverter.WriteJson` recalculait chaque empreinte globale à
chaque occurrence : nouveau sérialiseur, arbre JSON, tri, texte et SHA-256.
`NumericBufferConverter` créait ensuite pour chaque tableau un fichier temporaire,
le relisait pour son hash, puis le renommait ou le supprimait. Ces opérations,
ainsi que la compression, étaient exécutées depuis la capture sur le thread Unity.
Le transport n’était appelé qu’une fois cette préparation terminée.

Preuve locale : `.test-results/quest-parity/capture-freeze-initial-evidence.json`.
Les 191 secondes décrivent la portion observée du travail, pas une durée totale
ni une attribution exclusive aux empreintes.

## Correction

- Validation des définitions une fois par **instance** et par sérialisation.
  La comparaison utilise l’identité de référence, car l’égalité de BaseData
  repose sur l’ID. Une instance différente avec le même ID est toujours vérifiée,
  et les définitions sont revalidées lors d’une capture ultérieure.
- Capture synchrone des configurations, métadonnées et copies des buffers en
  mémoire, sous la protection existante de la scène. Aucun graphe mutable ni
  objet Unity n’est parcouru après cette capture.
- Écriture directe des buffers dans le ZIP sur un worker ; aucun fichier
  temporaire n’est créé par tableau. Les volumes sont copiés et vérifiés contre
  leur empreinte de chargement pendant la compression.
- Calcul du hash final et nettoyage hors du thread Unity ; contrôles d’annulation
  pendant l’encodage et attente de sa terminaison avant le nettoyage.
- Écriture groupée des composantes des surfaces, en conservant exactement le
  format binaire little-endian existant.
- Messages de progression bornés à la capture, logs `QUEST_CAPTURE` par phase.

Le format transféré reste la version 3 : contenu et identités de ressources
inchangés, seule leur production est réorganisée. Le protocole d’appairage, les
préférences et `hbp_core` ne sont pas modifiés. Les illustrations facultatives
peuvent encore être copiées pendant la capture ; celles des définitions globales
sont déjà figées lors de l’appairage.

## Validation

Les 43 tests EditMode de `HBP.Transfer.Scene.Tests` passent via MCP Unity,
dont les nouveaux tests de snapshot détaché, mutation de définitions entre
captures, objets distincts partageant un ID, provenance des fichiers modifiés
et annulation avant encodage. L’écriture groupée des vecteurs, couleurs et
indices est comparée octet par octet à l’ancien encodage scalaire.
Preuve : `.test-results/quest-parity/capture-editmode-results-final.json`.

Le test PlayMode `CompleteScene_RestoresNativeModalitiesAndIndependentColumns_ThenRecaptures`
passe également via MCP : six modalités, restauration native, recapture, refus
d’un remplacement incompatible et annulation d’une restauration.
Preuve : `.test-results/quest-parity/capture-playmode-results-final.json`.

### Mesure intermédiaire dans l’éditeur

`visu_full_test / Small`, trois colonnes iEEG, calcul automatique désactivé :
deux préparations aboutissent en 32,4 s puis 21,8 s, archives de 143,2 Mo.
La compression s’effectue pendant que la boucle de rendu continue de tourner.
Une pause maximale de 5,9 s puis 5,0 s subsiste dans cette version intermédiaire.
Un sondage séparé attribue 3,06 s à la capture des ressources et 1,90 s au JSON
(34,4 Mo). Ces mesures précèdent l’écriture groupée des surfaces.

L’éditeur émet une erreur URP répétée après le changement de profil Quest vers
Desktop (`_AdditionalShadowParams`, 256 vs 32). Une mesure dans un nouveau
player Windows a été tentée pour isoler cette erreur d’état de l’éditeur.

Preuves : `.test-results/quest-parity/small-capture-editor-20260915a/result.json`
et `.test-results/quest-parity/small-stage-probe.json`.

### Mesure finale dans l’éditeur

Même projet, même visualisation, `AutomaticEEGUpdate=false`, après optimisation :

| Capture | Durée totale, frame finale comprise | Plus grand intervalle entre frames | Archive |
| --- | ---: | ---: | ---: |
| Première | 33,60 s | 4,69 s | 143 195 875 octets |
| Seconde | 24,20 s | 5,20 s | 143 195 875 octets |

Les phases de capture synchrone durent respectivement 4,68 s et 4,19 s.
Le premier passage charge également des ressources préalables pendant environ
10 s. L’encodage et le hash continuent hors du thread Unity. Ces mesures
restent sensibles à la limitation de cadence de l’éditeur non focalisé et à
l’erreur URP répétée ; elles ne constituent pas une mesure du player Windows.
L’optimisation des surfaces réduit modestement le travail synchrone, sans
supprimer la pause de snapshot. La disparition du long travail bloquant est
principalement due au cache des validations et à l’encodage sur worker.

Preuve : `.test-results/quest-parity/small-capture-editor-final-20260915/result.json`.

### Build Windows

La build IL2CPP de développement réussit (248,97 s, aucune erreur), dans
`.artifacts/quest-030/Windows/HiBoP.6.1.0.win64/HiBoP.exe`.
Preuves : `DesktopWindows.build-report.json` dans le dossier Windows et
`.test-results/quest-parity/capture-desktop-build-result.txt`.

Deux lancements de mesure sans interface visible n’ont pas atteint la capture :
le premier en batchmode (le lecteur CLI attend `WaitForEndOfFrame` après chaque
argument), le second avec une fenêtre masquée. Ils ont été arrêtés en ne ciblant
que les processus créés pour ces diagnostics. Le second blocage de lancement
n’est pas diagnostiqué. Aucun de ces essais ne valide la performance du player.
La mesure finale utilise donc l’éditeur via MCP ; une recette visible du player
et la réception sur le Quest restent à faire. Le Quest ne répondait plus à son
ancienne adresse Wi-Fi lors de cette session.

## Diagnostic réutilisable

Dans une build de développement, ouvrir normalement un projet et sa visualisation
avec `-pf <projet.hibop> -v Small`, puis ajouter
`-sceneCaptureEvidence <dossier-de-preuves> -sceneCaptureOnce`.
Utiliser un player normal, avec une fenêtre visible ; ne pas passer `-batchmode`.
Deux captures locales sont effectuées, avec durées, plus grand intervalle entre
frames et taille de chaque archive. Aucun envoi réseau n’est effectué.

Dans l’éditeur en Play Mode, le même diagnostic est accessible via MCP
`execute_code` en lançant sans attente bloquante :

```csharp
Cysharp.Threading.Tasks.UniTaskExtensions.Forget(
    HBP.Dev.SceneCaptureDiagnostic.RunAsync("C:/HBP/Software/HiBoP/.test-results/quest-parity/small-capture"),
    UnityEngine.Debug.LogException);
return "Capture diagnostic started";
```

Le dossier doit être nouveau pour distinguer les résultats de chaque essai.


## Acceptation utilisateur

Le propriétaire accepte les fonctionnalités développées et demande la clôture
le 2026-09-15. Les limites des mesures ci-dessus restent documentées. Il signale
une lenteur distincte après réception sur Quest, consignée dans le
[bilan de clôture](QUEST-030-state-transfer-implementation.md#clôture-fonctionnelle-et-point-de-performance-restant).
