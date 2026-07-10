# SaveRestoreGUI

> Outil Windows Forms (.NET 10) de sauvegarde, restauration et migration de profils utilisateurs Windows.

---

## Fonctionnalités

| Onglet | Rôle |
|---|---|
| **Sauvegarde** | Copie sélective de Documents, Bureau, Téléchargements, Outlook, lecteurs réseau, signatures, etc. |
| **Restauration** | Réimporte les données depuis un dossier de sauvegarde existant |
| **Migration USB** | Copie le profil d'un ancien PC/disque externe vers le profil courant |

## Prérequis

- Windows 10/11 64-bit
- .NET 10 Desktop Runtime (ou SDK pour compiler)
- **Droits administrateur recommandés** (import de clés de registre, accès à `C:\Users`)

## Structure d'une sauvegarde

```
<DossierSauvegarde>\<NomUtilisateur>_<Date>\
├── Documents\
├── Desktop\
├── Downloads\
├── Pictures\
├── Music\
├── Videos\
├── Outlook\              ← .pst / .ost + profils .reg
├── Signatures\           ← signatures Outlook
├── StickyNotes\
├── EdgeProfile\
├── Wallpaper\
├── NetworkDrives.txt     ← lecteurs réseau (format: LETTRE|CHEMIN_UNC|LIBELLE)
├── Templates\
├── ExcelMacros\
├── SAP\
├── OneNote\
├── IpDesktopSoftphone\
└── BackupInfo.json       ← métadonnées (date, utilisateur, version)
```

## Compilation

```powershell
dotnet build SaveRestoreGUI.sln -c Release
```

L'exécutable est produit dans `bin\Release\net10.0-windows\`.

## Versionnage

La version est lue automatiquement depuis `<Version>` dans `SaveRestoreGUI.csproj` et affichée dans la barre de titre au démarrage.

## Licence

Usage interne — tous droits réservés.
