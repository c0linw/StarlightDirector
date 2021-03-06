using OpenCGSS.StarlightDirector.Localization;
using OpenCGSS.StarlightDirector.UI.Extensions;

namespace OpenCGSS.StarlightDirector.UI.Forms {
    partial class FAbout : ILocalizable {

        public void Localize(ILanguageManager manager) {
            if (manager == null) {
                return;
            }

            manager.ApplyText(this, "ui.fabout.text");
            manager.ApplyText(btnClose, "ui.fabout.button.close.text");
        }

    }
}
