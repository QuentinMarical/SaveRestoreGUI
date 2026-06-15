# Upgrade Options — SaveRestoreGUI

Assessment: 1 projet (WinForms, net8.0-windows, SDK-style), cible net10.0-windows, 1 package à mettre à jour (System.Management 8.0.0 → 10.0.9), aucun package incompatible.

## Strategy

### Upgrade Strategy
Projet unique sans graphe de dépendances — la mise à niveau atomique en une passe est l'approche la plus simple et la plus rapide.

| Value | Description |
|-------|-------------|
| **All-at-Once** (selected) | Mise à niveau de tous les projets simultanément en une seule passe atomique : changement de TFM, mise à jour des packages, corrections de code, validation complète. |
