using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using OpenCGSS.StarlightDirector.DirectorApplication;
using OpenCGSS.StarlightDirector.Localization;
using OpenCGSS.StarlightDirector.UI.Controls.Previewing;

namespace OpenCGSS.StarlightDirector.UI.Forms {
    public partial class FEditorSettings : Form {

        public static DialogResult ChangeSettings(IWin32Window parent, DirectorSettings settings) {
            using (var f = new FEditorSettings()) {
                f._editorSettings = settings;
                f.MonitorLocalizationChange();
                f.Localize(LanguageManager.Current);
                var r = f.ShowDialog(parent);
                f.UnmonitorLocalizationChange();
                return r;
            }
        }

        ~FEditorSettings() {
            UnregisterEventHandlers();
        }

        private FEditorSettings() {
            InitializeComponent();
            RegisterEventHandlers();
        }

        private void UnregisterEventHandlers() {
            Load -= FEditorSettings_Load;
            btnOK.Click -= BtnOK_Click;
            btnSelectExternalPreviewerFile.Click -= BtnSelectExternalPreviewerFile_Click;
        }

        private void RegisterEventHandlers() {
            Load += FEditorSettings_Load;
            btnOK.Click += BtnOK_Click;
            btnSelectExternalPreviewerFile.Click += BtnSelectExternalPreviewerFile_Click;
        }

        private void BtnSelectExternalPreviewerFile_Click(object sender, EventArgs e) {
            ofd.CheckFileExists = true;
            ofd.ValidateNames = true;
            ofd.ShowReadOnly = false;
            ofd.ReadOnlyChecked = false;
            ofd.Multiselect = false;
            ofd.Filter = "Executable File (*.exe)|*.exe|All Files (*.*)|*.*";

            var r = ofd.ShowDialog(this);

            if (r == DialogResult.Cancel) {
                return;
            }

            txtExternalPreviewerFile.Text = ofd.FileName;
        }

        private void BtnOK_Click(object sender, EventArgs e) {
            var s = _editorSettings;

            s.InvertedScrolling = radInvertedScrollingOn.Checked;
            s.ShowNoteIndicators = chkShowNoteIndicators.Checked;
            s.ScrollingSpeed = (int)txtScrollingSpeed.Value;
            s.PreviewRenderMode = (PreviewerRenderMode)cboPreviewRenderMode.SelectedIndex;
            s.PreviewSpeed = Convert.ToSingle(txtPreviewSpeed.Text);
            s.ExternalPreviewerFile = txtExternalPreviewerFile.Text;
            s.ExternalPreviwerArgs = txtExternalPreviewerArgs.Text;

            if (cboLanguage.SelectedIndex == 0) {
                s.Language = null;
            } else {
                s.Language = _languages[cboLanguage.SelectedIndex - 1].CodeName;
            }

            if (s.Language != _originalLanguage) {
                DirectorSettingsManager.ApplyLanguageSettings();

                var languageManager = LanguageManager.Current;

                if (languageManager == null) {
                    throw new InvalidOperationException();
                }

                LocalizationHelper.Relocalize(languageManager);
            }

            DialogResult = DialogResult.OK;
        }

        private void FEditorSettings_Load(object sender, EventArgs e) {
            var directory = new DirectoryInfo(DirectorSettingsManager.LanguagesPath);
            if (directory.Exists) {
                var fileNames = Directory.EnumerateFiles(directory.FullName, "*" + DirectorSettingsManager.LanguageFileExtension, SearchOption.TopDirectoryOnly);
                foreach (var fileName in fileNames) {
                    // The language name is not important here.
                    var manager = LanguageManager.CreateFromFile(fileName, fileName);
                    _languages.Add((manager.DisplayName, manager.CodeName));
                }
            }

            cboPreviewRenderMode.Items.Clear();
            for (var i = (int)PreviewerRenderMode.Standard; i <= (int)PreviewerRenderMode.EditorLike; ++i) {
                var textKey = $"ui.feditorsettings.dropdown.{i}";
                cboPreviewRenderMode.Items.Add(LanguageManager.Current.GetString(textKey));
            }
            if (cboPreviewRenderMode.Items.Count > 0) {
                cboPreviewRenderMode.SelectedIndex = 0;
            }

            cboLanguage.Items.Clear();
            cboLanguage.Items.Add(LanguageManager.TryGetString("lang.use_auto.text") ?? "Auto");
            cboLanguage.Items.AddRange(_languages.Select(v => v.DisplayName).Cast<object>().ToArray());

            var s = _editorSettings;

            int langIndex;
            _originalLanguage = s.Language;
            if (s.Language == null) {
                langIndex = 0;
            } else {
                if (LanguageManager.Current != null) {
                    langIndex = _languages.FindIndex(v => v.CodeName == LanguageManager.Current.CodeName);
                    if (langIndex < 0) {
                        langIndex = 0;
                    } else {
                        langIndex += 1;
                    }
                } else {
                    langIndex = 0;
                }
            }
            cboLanguage.SelectedIndex = langIndex;

            radInvertedScrollingOn.Checked = s.InvertedScrolling;
            radInvertedScrollingOff.Checked = !s.InvertedScrolling;
            chkShowNoteIndicators.Checked = s.ShowNoteIndicators;
            txtScrollingSpeed.Value = s.ScrollingSpeed;
            if (cboPreviewRenderMode.Items.Count > 0) {
                cboPreviewRenderMode.SelectedIndex = (int)s.PreviewRenderMode;
            }
            txtPreviewSpeed.Text = s.PreviewSpeed.ToString(CultureInfo.InvariantCulture);

            txtExternalPreviewerFile.Text = s.ExternalPreviewerFile;
            txtExternalPreviewerArgs.Text = s.ExternalPreviwerArgs;
        }

        private DirectorSettings _editorSettings;
        private string _originalLanguage;
        private readonly List<(string DisplayName, string CodeName)> _languages = new List<(string, string)>();

    }
}
