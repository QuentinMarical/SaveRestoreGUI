# 02-final-validation: Validation finale de la solution

Effectuer la validation complète de la solution mise à niveau : build complet en configuration Release, vérification de l'absence d'erreurs et de warnings, exécution des tests s'il en existe, et vérification que la publication autonome (SelfContained/PublishSingleFile) fonctionne toujours correctement avec net10.0-windows. Documenter les recommandations différées éventuelles.

## Résultats de la recherche

- La solution ne contient qu'un seul projet (SaveRestoreGUI.csproj, WinForms) — **aucun projet de test** n'existe, l'étape tests est donc sans objet
- La tâche 01 a déjà validé le build Debug : 0 erreur, 0 warning
- Le projet est WinForms sans .resx avec images embarquées (seuls MainForm.cs, MainForm.Designer.cs, Program.cs) — `dotnet build`/`dotnet publish` suffisent
- Propriétés de publication définies dans le csproj : SelfContained=true, RuntimeIdentifier=win-x64, PublishSingleFile=true, EnableCompressionInSingleFile=true, IncludeNativeLibrariesForSelfExtract=true

**Plan d'exécution** :
1. `dotnet build -c Release` — vérifier 0 erreur / 0 warning
2. `dotnet publish -c Release` — vérifier que la publication autonome en fichier unique fonctionne avec net10.0-windows
3. Vérifier la présence de l'exécutable publié

**Done when**: Build complet sans erreurs ni warnings, tests passants (le cas échéant), publication autonome validée.
