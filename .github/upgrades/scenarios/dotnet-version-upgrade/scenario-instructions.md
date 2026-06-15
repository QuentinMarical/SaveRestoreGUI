# Mise à niveau de version .NET

## Preferences
- **Flow Mode**: Automatic
- **Target Framework**: net10.0 (.NET 10, LTS)
- **Langue de communication**: Français

## Upgrade Options
**Source**: .github/upgrades/scenarios/dotnet-version-upgrade/upgrade-options.md

### Strategy
- Upgrade Strategy: All-at-Once

## Strategy
**Selected**: All-At-Once
**Rationale**: Solution à projet unique (SaveRestoreGUI.csproj, WinForms, net8.0-windows, SDK-style), aucun graphe de dépendances à gérer, 1 seul package à mettre à jour, aucun package incompatible.

### Execution Constraints
- Mise à niveau atomique unique — TFM, packages et corrections de code dans une même passe
- Séquence : mise à jour du fichier projet (TFM) → mise à jour des packages → restore → build et correction de toutes les erreurs de compilation (une seule passe bornée)
- Validation complète de la solution (build sans erreurs ni warnings) après la mise à niveau
- Les tests viennent APRÈS la réussite de la mise à niveau atomique

