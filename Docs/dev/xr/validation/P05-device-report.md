# P05 — rapport de validation Quest 3

- **Date :** 2026-09-02
- **État :** PARTIAL PASS — performance/mémoire/shaders validés, transparence MR non conforme
- **APK :** `.artifacts/xr/p05/HiBoPXR-P05.apk`
- **Taille :** `77 395 361` octets
- **SHA-256 :** `5e604605986d68fea92cf5d0b4bee12c384e44bf4447278f363247203fc12224`

Le player a été exécuté sur un Oculus Quest 3 sous Android 14 / API 34, Adreno 740, Vulkan et espace Linear. Après 360 frames de warmup, 720 frames à 72 Hz ont été mesurées : intervalle p50/p95/max `13,8889/13,8890/13,8890 ms`, main thread `13,8516/14,3800/14,9299 ms`, GC `128/600/29 192 octets` par frame. Unity rapporte `85,98 Mo` alloués, `208,84 Mo` réservés et deux meshes actifs. Le compteur KGSL donne environ `50,34 %` d'utilisation GPU sur 15 échantillons. Le statut thermique est `0`, autour de `49 °C` CPU et `47 °C` GPU. Après `force-stop`, `dumpsys meminfo` répond `No process found`.

Le contrôle visuel valide les surfaces GIFTI anatomical/inflated, l'orientation View3D, le placement côte à côte, l'opaque et la suppression des artefacts de triangles successifs. Il invalide la transparence finale : l'inflated bleu reste visuellement opaque sur le passthrough malgré un alpha `0,25` validé par golden hôte, une prépasse profondeur, une sortie prémultipliée, `preserveFramebufferAlpha` et l'alpha URP activés.

Conclusion : les budgets et la durée de vie sont acceptables pour continuer les paquets indépendants, mais P05-D reste ouverte et le renderer ne doit pas être déclaré gelé. La reprise devra isoler la swapchain/projection OpenXR avec un test alpha discriminant plutôt que modifier encore le matériau à l'aveugle. Le run d'endurance 30 minutes reste également non exécuté.
