using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SaveRestoreGUI.UI
{
    /// <summary>
    /// Bouton carré multi-sélection de navigateurs.
    /// — Affiche ☐ si aucun navigateur n'est sélectionné.
    /// — Affiche ▣ si une sélection partielle est active.
    /// — Affiche ✔ si tous les navigateurs disponibles sont sélectionnés.
    /// Au clic, un ContextMenuStrip inline liste les navigateurs installés.
    /// </summary>
    public sealed class BrowserPickerButton : Button
    {
        // ── Modèle ────────────────────────────────────────────────────────

        /// <summary>Navigateurs disponibles. Seuls les IsAvailable=true apparaissent.</summary>
        public List<BrowserEntry> Browsers { get; } = [];

        /// <summary>Noms des navigateurs actuellement cochés.</summary>
        public IReadOnlyList<string> SelectedBrowsers => _selected.AsReadOnly();

        private readonly List<string> _selected = [];

        // ── Constructeur ─────────────────────────────────────────────────

        public BrowserPickerButton()
        {
            FlatStyle   = FlatStyle.System;
            AutoSize    = false;
            Size        = new Size(220, 28);
            TextAlign   = ContentAlignment.MiddleLeft;
            Padding     = new Padding(4, 0, 0, 0);
            UpdateLabel();
        }

        // ── Méthodes publiques ───────────────────────────────────────────

        /// <summary>
        /// Reconstruit la liste des navigateurs depuis la source fournie,
        /// puis met à jour l'étiquette du bouton.
        /// </summary>
        public void SetBrowsers(IEnumerable<BrowserEntry> entries)
        {
            Browsers.Clear();
            Browsers.AddRange(entries);

            // Retire de _selected les navigateurs qui ne sont plus disponibles.
            _selected.RemoveAll(n => !Browsers.Any(b => b.Name == n && b.IsAvailable));

            UpdateLabel();
        }

        /// <summary>Coche ou décoche un navigateur par son nom.</summary>
        public void SetSelected(string name, bool selected)
        {
            if (selected && !_selected.Contains(name)) _selected.Add(name);
            else if (!selected) _selected.Remove(name);
            UpdateLabel();
        }

        /// <summary>Retourne true si le navigateur donné est sélectionné.</summary>
        public bool IsSelected(string name) => _selected.Contains(name);

        // ── Clic → dropdown ──────────────────────────────────────────────

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            ShowDropDown();
        }

        private void ShowDropDown()
        {
            var available = Browsers.Where(b => b.IsAvailable).ToList();
            if (available.Count == 0) return;

            var menu = new ContextMenuStrip { ShowImageMargin = false };

            foreach (var entry in available)
            {
                var item = new ToolStripMenuItem($"{entry.Icon}  {entry.Name}")
                {
                    CheckOnClick = true,
                    Checked      = _selected.Contains(entry.Name)
                };

                // Capture locale obligatoire pour le lambda.
                var capturedName = entry.Name;
                item.CheckedChanged += (_, _) =>
                {
                    if (item.Checked)
                    {
                        if (!_selected.Contains(capturedName))
                            _selected.Add(capturedName);
                    }
                    else
                    {
                        _selected.Remove(capturedName);
                    }
                    UpdateLabel();
                };

                menu.Items.Add(item);
            }

            menu.Items.Add(new ToolStripSeparator());

            var checkAll = new ToolStripMenuItem("✔  Tout cocher");
            checkAll.Click += (_, _) =>
            {
                _selected.Clear();
                _selected.AddRange(available.Select(b => b.Name));
                UpdateLabel();
            };
            menu.Items.Add(checkAll);

            var uncheckAll = new ToolStripMenuItem("☐  Tout décocher");
            uncheckAll.Click += (_, _) =>
            {
                _selected.Clear();
                UpdateLabel();
            };
            menu.Items.Add(uncheckAll);

            // Affiche juste sous le bouton.
            menu.Show(this, new Point(0, Height));
        }

        // ── Mise à jour de l'étiquette ───────────────────────────────────

        private void UpdateLabel()
        {
            var available = Browsers.Count(b => b.IsAvailable);

            Text = (_selected.Count, available) switch
            {
                (0, _)            => "\u2610  Navigateurs",          // ☐
                var (n, a) when n == a && a > 0
                                  => $"\u2714  Tous ({n})",          // ✔
                _                 => $"\u25a3  {string.Join(", ", _selected)}" // ▣
            };
        }
    }

    // ── Modèle de données ─────────────────────────────────────────────────

    /// <summary>Description d'un navigateur exposé dans le picker.</summary>
    public sealed record BrowserEntry(
        string Name,
        string Icon,
        bool   IsAvailable,
        string ProfilePath,
        string? ExePath = null);
}
