# Progression — 01-upgrade-saverestoregui

## Modifications effectuées

### SaveRestoreGUI.csproj
- `TargetFramework` : `net8.0-windows` → `net10.0-windows`
- `System.Management` : `8.0.0` → `10.0.9`
- Les propriétés de publication autonome (SelfContained, PublishSingleFile, win-x64, compression) sont conservées telles quelles — compatibles .NET 10

### MainForm.cs
- Ligne 9 : suppression du modificateur `readonly` sur le champ `_cancellationTokenSource` (type `CancellationTokenSource?`)
  - **Cause** : le champ était assigné dans 3 gestionnaires d'événements (lignes 557, 920, 1403 — BtnStartBackup_Click, BtnStartRestore_Click, démarrage migration), ce qui est interdit pour un champ `readonly` (erreur CS0191 levée par le compilateur C# 14 de .NET 10)
  - **Effet secondaire positif** : les 3 warnings CS8602 (déréférencement de référence potentiellement null) ont également disparu, le flux de nullabilité étant désormais correctement analysé après l'assignation

## Résultats de build

| Étape | Résultat |
|-------|----------|
| Restore | ✅ Succès (System.Management 10.0.9 restauré) |
| Build (Debug, net10.0-windows, win-x64) | ✅ **0 erreur, 0 warning** |

Sortie : `bin\Debug\net10.0-windows\win-x64\SaveRestoreGUI.dll`

## Problèmes rencontrés et résolus

1. **CS0191 (×3)** : champ `readonly` assigné hors constructeur — résolu en retirant `readonly` (le champ est réassigné légitimement à chaque démarrage d'opération de sauvegarde/restauration/migration)
2. **CS8602 (×3)** : warnings de nullabilité consécutifs — résolus automatiquement par la correction ci-dessus

## Vérification des critères « Done when »

- [x] Le projet cible net10.0-windows — vérifié dans le csproj
- [x] System.Management est en 10.0.9 — vérifié dans le csproj + restore réussi
- [x] La solution compile sans erreurs ni warnings — build vérifié : 0/0

## Écarts par rapport au plan

Aucun écart : les incompatibilités binaires (Api.0001) signalées par l'évaluation ont bien été résolues par la recompilation, comme prévu. Les seules corrections de code nécessaires (readonly/CS0191) n'étaient pas signalées par l'évaluation mais relèvent du durcissement du compilateur C# 14.
