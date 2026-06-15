# 01-upgrade-saverestoregui: Mise à niveau du projet SaveRestoreGUI vers net10.0-windows

Vérifier que le SDK .NET 10 est installé (déjà validé en pré-initialisation), puis mettre à niveau le projet SaveRestoreGUI.csproj : changer le TargetFramework de `net8.0-windows` vers `net10.0-windows`, mettre à jour le package System.Management de 8.0.0 vers 10.0.9, restaurer les dépendances, compiler et corriger toutes les erreurs de compilation en une seule passe bornée.

L'évaluation signale 2 186 incompatibilités binaires (Api.0001) qui se résolvent par recompilation avec le nouveau framework, et 214 incompatibilités source potentielles (Api.0002) concentrées sur Windows Forms, GDI+/System.Drawing et WMI (System.Management). Le projet utilise la publication autonome (SelfContained, PublishSingleFile, win-x64) — vérifier que ces propriétés restent compatibles avec .NET 10.

Points de départ pour la recherche : examiner les 5 fichiers affectés signalés par l'évaluation, vérifier les changements cassants WinForms entre .NET 8 et .NET 10, contrôler les API System.Management utilisées.

## Résultats de la recherche

**Structure du projet** (confirmée) :
- Projet unique SDK-style, `net8.0-windows`, WinForms (`UseWindowsForms=true`), Nullable + ImplicitUsings activés
- Publication autonome : `SelfContained=true`, `RuntimeIdentifier=win-x64`, `PublishSingleFile=true`, `EnableCompressionInSingleFile=true`, `IncludeNativeLibrariesForSelfExtract=true` — toutes ces propriétés restent prises en charge en .NET 10
- 3 fichiers source : Program.cs (12 lignes), MainForm.cs (~1 600+ lignes), MainForm.Designer.cs (~720+ lignes)

**Analyse des problèmes signalés** :
- La quasi-totalité des 2 402 problèmes sont des `Api.0001` (incompatibilités binaires) sur des types WinForms standard (Button, CheckBox, RichTextBox, MessageBox, etc.) — résolus automatiquement par la recompilation vers net10.0-windows
- Le fichier `obj\...\ApplicationConfiguration.g.cs` est généré automatiquement — sera régénéré au build
- WMI : `using System.Management` ligne 1, `ManagementObjectSearcher` lignes 848-849 de MainForm.cs (requête Win32_MappedLogicalDisk) — API inchangée entre System.Management 8.0.0 et 10.0.9, simple bump de version
- Program.cs utilise `ApplicationConfiguration.Initialize()` — modèle standard .NET 6+, compatible .NET 10

**Actions à exécuter** :
1. SaveRestoreGUI.csproj : `<TargetFramework>net8.0-windows</TargetFramework>` → `net10.0-windows`
2. SaveRestoreGUI.csproj : `System.Management` 8.0.0 → 10.0.9
3. Restore + build, corriger les erreurs/warnings éventuels (une seule passe)

**Décisions** : Pas de décomposition nécessaire — tâche atomique (1 projet, 2 modifications de fichier projet, corrections de code attendues minimes).

**Done when**: Le projet cible net10.0-windows, System.Management est en 10.0.9, la solution compile sans erreurs ni warnings.
