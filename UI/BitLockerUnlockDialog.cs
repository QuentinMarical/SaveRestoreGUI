namespace SaveRestoreGUI.UI
{
    /// <summary>
    /// Boîte de dialogue pour saisir la clé de récupération BitLocker.
    /// Affiche le Key ID du volume pour aider l'utilisateur à retrouver
    /// la bonne clé dans Azure AD / Active Directory ou son compte Microsoft.
    /// </summary>
    public sealed class BitLockerUnlockDialog : Form
    {
        private readonly TextBox _txtRecoveryKey;
        private readonly Label   _lblKeyId;
        private readonly Button  _btnOk;
        private readonly Button  _btnCancel;

        /// <summary>Clé de récupération saisie par l'utilisateur.</summary>
        public string RecoveryKey => _txtRecoveryKey.Text.Trim();

        public BitLockerUnlockDialog(string driveLetter, string keyId)
        {
            Text            = $"Déverrouillage BitLocker — {driveLetter}";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            StartPosition   = FormStartPosition.CenterParent;
            ClientSize      = new Size(520, 270);
            Font            = new Font("Segoe UI", 9.5f);

            // ── Icône cadenas ──────────────────────────────────────────
            var lblIcon = new Label
            {
                Text      = "🔒",
                Font      = new Font("Segoe UI", 28f),
                Location  = new Point(24, 20),
                Size      = new Size(52, 52),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // ── Titre ─────────────────────────────────────────────────
            var lblTitle = new Label
            {
                Text      = $"Le disque {driveLetter} est protégé par BitLocker",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                Location  = new Point(84, 22),
                Size      = new Size(420, 26),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // ── Description ───────────────────────────────────────────
            var lblDesc = new Label
            {
                Text     = "Retrouvez la clé de récupération dans votre portail Azure AD,\nActive Directory ou votre compte Microsoft (aka.ms/myrecoverykey).",
                Location = new Point(84, 54),
                Size     = new Size(420, 42),
                Font     = new Font("Segoe UI", 9f)
            };

            // ── Key ID ────────────────────────────────────────────────
            var lblKeyIdCaption = new Label
            {
                Text     = "Identifiant de clé BitLocker (Key ID) :",
                Font     = new Font("Segoe UI", 9f, FontStyle.Bold),
                Location = new Point(24, 112),
                Size     = new Size(300, 20)
            };

            _lblKeyId = new Label
            {
                Text      = string.IsNullOrWhiteSpace(keyId) ? "(non disponible)" : keyId,
                Font      = new Font("Cascadia Code", 9f, FontStyle.Regular),
                Location  = new Point(24, 134),
                Size      = new Size(472, 22),
                ForeColor = Color.DarkBlue,
                Cursor    = Cursors.Hand,
                AutoSize  = false
            };
            // Clic pour copier le Key ID
            _lblKeyId.Click += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(_lblKeyId.Text) && _lblKeyId.Text != "(non disponible)")
                {
                    Clipboard.SetText(_lblKeyId.Text);
                    var orig = _lblKeyId.ForeColor;
                    _lblKeyId.ForeColor = Color.Green;
                    Task.Delay(1000).ContinueWith(_ =>
                        _lblKeyId.Invoke(() => _lblKeyId.ForeColor = orig));
                }
            };

            // ── Séparateur ────────────────────────────────────────────
            var sep = new Panel
            {
                Location  = new Point(24, 164),
                Size      = new Size(472, 1),
                BackColor = Color.LightGray
            };

            // ── Label clé de récupération ─────────────────────────────
            var lblRec = new Label
            {
                Text     = "Clé de récupération (48 chiffres) :",
                Font     = new Font("Segoe UI", 9f, FontStyle.Bold),
                Location = new Point(24, 176),
                Size     = new Size(300, 20)
            };

            // ── Zone de saisie ────────────────────────────────────────
            _txtRecoveryKey = new TextBox
            {
                Location    = new Point(24, 198),
                Size        = new Size(356, 28),
                Font        = new Font("Cascadia Code", 9.5f),
                MaxLength   = 55,
                PlaceholderText = "000000-000000-000000-000000-000000-000000-000000-000000"
            };
            // Nettoie les tirets à la saisie pour ne garder que les chiffres + tirets standards
            _txtRecoveryKey.TextChanged += (s, e) => FormatRecoveryKey();

            // ── Boutons ───────────────────────────────────────────────
            _btnOk = new Button
            {
                Text         = "Déverrouiller",
                DialogResult = DialogResult.OK,
                Location     = new Point(392, 196),
                Size         = new Size(104, 30),
                Font         = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor    = Color.FromArgb(0, 122, 204),
                ForeColor    = Color.White,
                FlatStyle    = FlatStyle.Flat
            };
            _btnOk.FlatAppearance.BorderSize = 0;

            _btnCancel = new Button
            {
                Text         = "Annuler",
                DialogResult = DialogResult.Cancel,
                Location     = new Point(392, 232),
                Size         = new Size(104, 26),
                Font         = new Font("Segoe UI", 9f)
            };

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;

            Controls.AddRange(new Control[]
            {
                lblIcon, lblTitle, lblDesc,
                lblKeyIdCaption, _lblKeyId, sep,
                lblRec, _txtRecoveryKey,
                _btnOk, _btnCancel
            });
        }

        /// <summary>
        /// Formate la saisie en blocs de 6 chiffres séparés par des tirets.
        /// Accepte les espaces et tirets comme séparateurs à l'entrée.
        /// </summary>
        private void FormatRecoveryKey()
        {
            var text   = _txtRecoveryKey.Text;
            var digits = new string(text.Where(char.IsDigit).ToArray());

            if (digits.Length == 0)
                return;

            // Reconstruire avec tirets tous les 6 chiffres
            var parts  = Enumerable.Range(0, (digits.Length + 5) / 6)
                                   .Select(i => digits.Substring(i * 6, Math.Min(6, digits.Length - i * 6)))
                                   .ToArray();
            var formatted = string.Join("-", parts);

            if (formatted == text) return;

            _txtRecoveryKey.TextChanged -= (s, e) => FormatRecoveryKey();
            _txtRecoveryKey.Text         = formatted;
            _txtRecoveryKey.SelectionStart = formatted.Length;
            _txtRecoveryKey.TextChanged += (s, e) => FormatRecoveryKey();
        }
    }
}
