using SaveRestoreGUI.UI;

namespace SaveRestoreGUI
{
    /// <summary>
    /// Boîte de dialogue permettant à l'utilisateur de saisir la clé de récupération
    /// BitLocker (48 chiffres, format XXXXXX-XXXXXX-... ou sans tirets).
    /// </summary>
    internal sealed class BitLockerKeyDialog : Form
    {
        // ── Propriété publique ───────────────────────────────────────────
        /// <summary>Clé saisie par l'utilisateur (formatée en XXXXXX-XXXXXX-...).</summary>
        public string RecoveryKey { get; private set; } = string.Empty;

        // ── Contrôles ───────────────────────────────────────────────────
        private readonly Label        _lblInfo;
        private readonly Label        _lblDrive;
        private readonly TextBox      _txtKey;
        private readonly Label        _lblHint;
        private readonly Label        _lblValidation;
        private readonly ModernButton _btnOk;
        private readonly ModernButton _btnCancel;

        // ── Constructeur ─────────────────────────────────────────────────
        public BitLockerKeyDialog(string driveLetter)
        {
            Text            = "Déverrouillage BitLocker";
            Size            = new Size(520, 300);
            MinimumSize     = new Size(480, 280);
            MaximumSize     = new Size(700, 320);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition   = FormStartPosition.CenterParent;
            MaximizeBox     = false;
            MinimizeBox     = false;
            ShowInTaskbar   = false;
            BackColor       = Color.FromArgb(30, 30, 30);
            ForeColor       = Color.FromArgb(220, 220, 220);
            Font            = new Font("Segoe UI", 9f);
            Padding         = new Padding(16);

            _lblInfo = new Label
            {
                Text      = "🔒  Le lecteur est verrouillé par BitLocker",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 180, 60),
                AutoSize  = false,
                Height    = 28,
                Dock      = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _lblDrive = new Label
            {
                Text      = $"Lecteur : {driveLetter}",
                ForeColor = Color.FromArgb(180, 180, 180),
                AutoSize  = true
            };

            _txtKey = new TextBox
            {
                PlaceholderText = "000000-000000-000000-000000-000000-000000-000000-000000",
                Font            = new Font("Consolas", 10f),
                BackColor       = Color.FromArgb(45, 45, 45),
                ForeColor       = Color.White,
                BorderStyle     = BorderStyle.FixedSingle,
                MaxLength       = 100,
                Dock            = DockStyle.Top,
                Height          = 28,
                CharacterCasing = CharacterCasing.Upper
            };
            _txtKey.TextChanged += TxtKey_TextChanged;

            _lblHint = new Label
            {
                Text      = "Saisissez les 48 chiffres avec ou sans tirets (ex : 123456-789012-…)",
                ForeColor = Color.FromArgb(140, 140, 140),
                AutoSize  = false,
                Height    = 20,
                Dock      = DockStyle.Top,
                Font      = new Font("Segoe UI", 8f)
            };

            _lblValidation = new Label
            {
                Text      = "",
                ForeColor = Color.FromArgb(255, 100, 100),
                AutoSize  = false,
                Height    = 20,
                Dock      = DockStyle.Top,
                Font      = new Font("Segoe UI", 8f)
            };

            _btnOk = new ModernButton
            {
                Text    = "Déverrouiller",
                Role    = ButtonRole.Primary,
                Size    = new Size(140, 34),
                Enabled = false
            };
            _btnOk.Click += BtnOk_Click;

            _btnCancel = new ModernButton
            {
                Text = "Annuler",
                Role = ButtonRole.Secondary,
                Size = new Size(100, 34)
            };
            _btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

            var pnlButtons = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock          = DockStyle.Bottom,
                Height        = 46,
                BackColor     = Color.Transparent,
                Padding       = new Padding(0, 6, 0, 0)
            };
            pnlButtons.Controls.Add(_btnCancel);
            pnlButtons.Controls.Add(_btnOk);

            var layout = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 1,
                RowCount    = 5,
                BackColor   = Color.Transparent,
                Padding     = new Padding(4, 8, 4, 4)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));  // titre
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));  // lecteur
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));  // textbox
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));  // hint
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));  // validation
            layout.Controls.Add(_lblInfo,       0, 0);
            layout.Controls.Add(_lblDrive,      0, 1);
            layout.Controls.Add(_txtKey,        0, 2);
            layout.Controls.Add(_lblHint,       0, 3);
            layout.Controls.Add(_lblValidation, 0, 4);

            Controls.Add(pnlButtons);
            Controls.Add(layout);

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;
        }

        // ── Validation en temps réel ────────────────────────────────────────
        private void TxtKey_TextChanged(object? sender, EventArgs e)
        {
            var raw    = _txtKey.Text.Replace("-", "").Replace(" ", "");
            var digits = raw.Count(char.IsDigit);
            var valid  = raw.Length == 48 && raw.All(char.IsDigit);

            if (string.IsNullOrWhiteSpace(_txtKey.Text))
            {
                _lblValidation.Text      = "";
                _lblValidation.ForeColor = Color.FromArgb(255, 100, 100);
            }
            else if (valid)
            {
                _lblValidation.Text      = "✅ Clé valide (48 chiffres)";
                _lblValidation.ForeColor = Color.FromArgb(100, 200, 100);
            }
            else
            {
                _lblValidation.Text      = $"⚠️ {digits}/48 chiffres reconnus";
                _lblValidation.ForeColor = Color.FromArgb(255, 180, 60);
            }

            _btnOk.Enabled = valid;
        }

        // ── Validation finale ───────────────────────────────────────────
        private void BtnOk_Click(object? sender, EventArgs e)
        {
            var clean = _txtKey.Text.Replace("-", "").Replace(" ", "");
            if (clean.Length != 48 || !clean.All(char.IsDigit))
            {
                MessageBox.Show(
                    "La clé doit contenir exactement 48 chiffres.\n" +
                    "Format attendu : 123456-789012-345678-901234-567890-123456-789012-345678",
                    "Clé invalide", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Reformater en blocs de 6 séparés par des tirets (attendu par Unlock-BitLocker)
            RecoveryKey = string.Join("-",
                Enumerable.Range(0, 8).Select(i => clean.Substring(i * 6, 6)));

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
