# =============================================================================
# Create-CodeSigningCert.ps1
# Crée un certificat auto-signé de signature de code pour SaveRestoreGUI.
# Validité maximale absolue : 31/12/9999 (limite du format X.509 / DateTime)
#
# À exécuter UNE SEULE FOIS en PowerShell Administrateur.
# Le certificat est ensuite utilisé automatiquement à chaque build MSBuild.
# =============================================================================

#Requires -RunAsAdministrator

$ErrorActionPreference = "Stop"
$friendlyName = "SaveRestoreGUI Code Signing"

Write-Host ""
Write-Host "════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "   SaveRestoreGUI — Création du certificat de code  " -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# ── Vérifier si un certificat valide existe déjà ─────────────────────────────
$existing = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert |
    Where-Object { $_.FriendlyName -eq $friendlyName } |
    Sort-Object NotAfter -Descending |
    Select-Object -First 1

if ($existing) {
    Write-Host "⚠️  Un certificat existe déjà :" -ForegroundColor Yellow
    Write-Host "   Subject    : $($existing.Subject)"
    Write-Host "   Thumbprint : $($existing.Thumbprint)"
    Write-Host "   Expire le  : $($existing.NotAfter.ToString('dd/MM/yyyy'))"
    Write-Host ""
    $answer = Read-Host "Voulez-vous le remplacer par un nouveau certificat ? (o/N)"
    if ($answer -notmatch '^[oOyY]') {
        Write-Host "Annulé. Le certificat existant est conservé." -ForegroundColor Green
        exit 0
    }
}

# ── Créer le certificat (validité maximale absolue = 31/12/9999) ─────────────
Write-Host "🔐 Création du certificat..." -ForegroundColor Cyan

$cert = New-SelfSignedCertificate `
    -Type CodeSigningCert `
    -Subject "CN=SaveRestoreGUI, O=QuentinMarical, L=Rouen, C=FR" `
    -FriendlyName $friendlyName `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -NotAfter ([DateTime]::MaxValue) `
    -KeyUsage DigitalSignature `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3")

Write-Host "✅ Certificat créé" -ForegroundColor Green
Write-Host "   Thumbprint : $($cert.Thumbprint)"
Write-Host "   Expire le  : $($cert.NotAfter.ToString('dd/MM/yyyy'))"
Write-Host ""

# ── Faire confiance au certificat sur CE poste ───────────────────────────────
Write-Host "🔒 Ajout aux autorités de confiance..." -ForegroundColor Cyan

# TrustedPublisher (éditeurs de confiance — supprime l'avertissement SmartScreen)
$storePub = New-Object System.Security.Cryptography.X509Certificates.X509Store("TrustedPublisher", "CurrentUser")
$storePub.Open("ReadWrite")
$storePub.Add($cert)
$storePub.Close()

# Root (autorités racines — élimine tout avertissement Windows sur CE poste)
$storeRoot = New-Object System.Security.Cryptography.X509Certificates.X509Store("Root", "LocalMachine")
$storeRoot.Open("ReadWrite")
$storeRoot.Add($cert)
$storeRoot.Close()

Write-Host "✅ Certificat approuvé localement (TrustedPublisher + Root)" -ForegroundColor Green
Write-Host ""

# ── Supprimer l'ancien certificat si remplacement ───────────────────────────
if ($existing -and $existing.Thumbprint -ne $cert.Thumbprint) {
    Remove-Item -Path "Cert:\CurrentUser\My\$($existing.Thumbprint)" -Force -ErrorAction SilentlyContinue
    Write-Host "🗑️  Ancien certificat supprimé : $($existing.Thumbprint)" -ForegroundColor DarkGray
    Write-Host ""
}

# ── Test de signature rapide ─────────────────────────────────────────────────
$testFile = [System.IO.Path]::GetTempFileName() + ".ps1"
"Write-Host 'test'" | Set-Content $testFile
$testResult = Set-AuthenticodeSignature -FilePath $testFile -Certificate $cert -HashAlgorithm SHA256
Remove-Item $testFile -Force -ErrorAction SilentlyContinue

if ($testResult.Status -eq "Valid") {
    Write-Host "✅ Test de signature réussi" -ForegroundColor Green
} else {
    Write-Host "⚠️  Test de signature : $($testResult.Status) — $($testResult.StatusMessage)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Tout est prêt ! Résumé :" -ForegroundColor Cyan
Write-Host "  • FriendlyName : $friendlyName"
Write-Host "  • Thumbprint   : $($cert.Thumbprint)"
Write-Host "  • Valide jusqu'au : 31/12/9999 (maximum absolu X.509)"
Write-Host "  • La prochaine build VS signera automatiquement le .exe"
Write-Host "════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
