# Plan de mise à niveau .NET — SaveRestoreGUI

## Overview

**Target**: Mise à niveau de la solution SaveRestoreGUI de net8.0-windows vers net10.0-windows (LTS)
**Scope**: 1 projet WinForms (SDK-style), 1 package NuGet à mettre à jour, ~2 400 problèmes signalés (majoritairement des incompatibilités binaires résolues par recompilation)

### Selected Strategy
**All-At-Once** — Tous les projets mis à niveau simultanément en une seule opération.
**Rationale**: 1 projet, déjà sur .NET 8 moderne, structure de dépendances triviale.

## Tasks

### 01-upgrade-saverestoregui: Mise à niveau du projet SaveRestoreGUI vers net10.0-windows

Vérifier que le SDK .NET 10 est installé (déjà validé en pré-initialisation), puis mettre à niveau le projet SaveRestoreGUI.csproj : changer le TargetFramework de `net8.0-windows` vers `net10.0-windows`, mettre à jour le package System.Management de 8.0.0 vers 10.0.9, restaurer les dépendances, compiler et corriger toutes les erreurs de compilation en une seule passe bornée.

L'évaluation signale 2 186 incompatibilités binaires (Api.0001) qui se résolvent par recompilation avec le nouveau framework, et 214 incompatibilités source potentielles (Api.0002) concentrées sur Windows Forms, GDI+/System.Drawing et WMI (System.Management). Le projet utilise la publication autonome (SelfContained, PublishSingleFile, win-x64) — vérifier que ces propriétés restent compatibles avec .NET 10.

Points de départ pour la recherche : examiner les 5 fichiers affectés signalés par l'évaluation, vérifier les changements cassants WinForms entre .NET 8 et .NET 10, contrôler les API System.Management utilisées.

**Done when**: Le projet cible net10.0-windows, System.Management est en 10.0.9, la solution compile sans erreurs ni warnings.

---

### 02-final-validation: Validation finale de la solution

Effectuer la validation complète de la solution mise à niveau : build complet en configuration Release, vérification de l'absence d'erreurs et de warnings, exécution des tests s'il en existe, et vérification que la publication autonome (SelfContained/PublishSingleFile) fonctionne toujours correctement avec net10.0-windows. Documenter les recommandations différées éventuelles.

**Done when**: Build complet sans erreurs ni warnings, tests passants (le cas échéant), publication autonome validée.
