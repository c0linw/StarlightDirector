using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;
using OpenCGSS.StarlightDirector.DirectorApplication;
using OpenCGSS.StarlightDirector.DirectorApplication.Subsystems.Bvs;
using OpenCGSS.StarlightDirector.Interop;
using OpenCGSS.StarlightDirector.Localization;
using OpenCGSS.StarlightDirector.Models.Beatmap;
using OpenCGSS.StarlightDirector.Previewing.Audio;
using OpenCGSS.StarlightDirector.UI.Controls;

namespace OpenCGSS.StarlightDirector.UI.Forms {
    partial class FMain {

        private void UnregisterEventHandlers() {
            SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
            Load -= FMain_Load;
            sysMaximizeRestore.Click -= SysMaximizeRestore_Click;
            sysMinimize.Click -= SysMinimize_Click;
            picIcon.MouseDown -= PicIcon_MouseDown;
            tsbScoreDifficultySelection.Click -= TsbScoreDifficultySelection_Click;
            visualizer.ContextMenuRequested -= Visualizer_ContextMenuRequested;
            tsbEditMode.Click -= TsbEditMode_Click;
            tsbScoreNoteStartPosition.Click -= TsbScoreNoteStartPosition_Click;
            visualizer.ProjectModified -= Visualizer_ProjectModified;
            Closed -= FMain_Closed;
            visualizer.Previewer.FrameRendered -= Previewer_FrameRendered;
        }

        private void RegisterEventHandlers() {
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
            Load += FMain_Load;
            sysMaximizeRestore.Click += SysMaximizeRestore_Click;
            sysMinimize.Click += SysMinimize_Click;
            picIcon.MouseDown += PicIcon_MouseDown;
            tsbScoreDifficultySelection.Click += TsbScoreDifficultySelection_Click;
            visualizer.ContextMenuRequested += Visualizer_ContextMenuRequested;
            tsbEditMode.Click += TsbEditMode_Click;
            tsbScoreNoteStartPosition.Click += TsbScoreNoteStartPosition_Click;
            visualizer.ProjectModified += Visualizer_ProjectModified;
            Closed += FMain_Closed;
            visualizer.Previewer.FrameRendered += Previewer_FrameRendered;
        }

        private void Previewer_FrameRendered(object sender, EventArgs e) {
            var score = visualizer.Previewer.Score;
            if (score == null) {
                return;
            }

            var rawMusicTime = _liveControl.CurrentTime;

            lock (_liveControl.SfxSyncObject) {
                if (_liveControl.LiveSfxManager == null) {
                    return;
                }
                var now = (rawMusicTime + _liveControl.LiveSfxManager.BufferOffset).TotalSeconds;
                var prev = _liveControl.SfxBufferTime;
                if (now <= prev) {
                    return;
                }
                foreach (var note in score.GetAllNotes()) {
                    if (note.Temporary.HitTiming.TotalSeconds < prev || now <= note.Temporary.HitTiming.TotalSeconds) {
                        continue;
                    }
                    string sfxFileName;
                    if (note.Helper.IsSlide) {
                        sfxFileName = note.Helper.HasNextFlick || note.Helper.IsSlideStart || note.Helper.IsSlideEnd ? FlickAudioFilePath : SlideAudioFilePath;
                    } else {
                        sfxFileName = note.Helper.IsFlick ? FlickAudioFilePath : TapAudioFilePath;
                    }
                    _liveControl.LiveSfxManager.PlayWave(sfxFileName, PreviewingSettings.SfxVolume);
                }
                _liveControl.SfxBufferTime = now;
            }
        }

        private void FMain_Closed(object sender, EventArgs e) {
            this.UnmonitorLocalizationChange();
            //CmdPreviewStop.Execute(this, null);
            _liveControl.Dispose();
            DirectorSettingsManager.SaveSettings();

            _communication?.Client.SendEdExitedNotification().Wait(TimeSpan.FromSeconds(2));

            _communication?.Dispose();
        }

        private void Visualizer_ProjectModified(object sender, EventArgs e) {
            InformProjectModified();
        }

        private void TsbScoreNoteStartPosition_Click(object sender, EventArgs e) {
            var items = new ToolStripItem[mnuScoreNoteStartPosition.DropDownItems.Count];
            mnuScoreNoteStartPosition.DropDownItems.CopyTo(items, 0);
            tsbScoreNoteStartPosition.DropDownItems.AddRange(items);
            tsbScoreNoteStartPosition.DropDownClosed += DropDownClosed;
            tsbScoreNoteStartPosition.ShowDropDown();

            void DropDownClosed(object s, EventArgs ev) {
                mnuScoreNoteStartPosition.DropDownItems.AddRange(items);
                tsbScoreNoteStartPosition.DropDownClosed -= DropDownClosed;
            }
        }

        private void TsbEditMode_Click(object sender, EventArgs e) {
            var items = new ToolStripItem[mnuEditMode.DropDownItems.Count];
            mnuEditMode.DropDownItems.CopyTo(items, 0);
            tsbEditMode.DropDownItems.AddRange(items);
            tsbEditMode.DropDownClosed += DropDownClosed;
            tsbEditMode.ShowDropDown();

            void DropDownClosed(object s, EventArgs ev) {
                mnuEditMode.DropDownItems.AddRange(items);
                tsbEditMode.DropDownClosed -= DropDownClosed;
            }
        }

        private void Visualizer_ContextMenuRequested(object sender, ContextMenuRequestedEventArgs e) {
            var hasSelectedNotes = visualizer.Editor.HasSelectedNotes;
            var hasSelectedBars = visualizer.Editor.HasSelectedBars;
            if ((e.MenuType == VisualizerContextMenu.Note || e.MenuType == VisualizerContextMenu.Bar) && (!hasSelectedNotes && !hasSelectedBars)) {
                return;
            }
            switch (e.MenuType) {
                case VisualizerContextMenu.Note:
                    ctxSep1.Visible = hasSelectedNotes;
                    ctxScoreNoteResetToTap.Visible = hasSelectedNotes;
                    ctxScoreNoteDelete.Visible = hasSelectedNotes;
                    ctxSep2.Visible = false;
                    ctxScoreMeasureDelete.Visible = false;
                    ctxSep3.Visible = false;
                    ctxScoreNoteInsertSpecial.Visible = false;
                    ctxSep4.Visible = false;
                    ctxScoreNoteModifySpecial.Visible = false;
                    ctxScoreNoteDeleteSpecial.Visible = false;
                    ctxScoreNoteInsertSpecial.DeleteCommandParameter();
                    ctxScoreNoteModifySpecial.DeleteCommandParameter();
                    ctxScoreNoteDeleteSpecial.DeleteCommandParameter();
                    break;
                case VisualizerContextMenu.Bar:
                    ctxSep1.Visible = false;
                    ctxScoreNoteResetToTap.Visible = false;
                    ctxScoreNoteDelete.Visible = false;
                    ctxSep2.Visible = hasSelectedBars;
                    ctxScoreMeasureDelete.Visible = hasSelectedBars;
                    ctxSep3.Visible = false;
                    ctxScoreNoteInsertSpecial.Visible = false;
                    ctxSep4.Visible = false;
                    ctxScoreNoteModifySpecial.Visible = false;
                    ctxScoreNoteDeleteSpecial.Visible = false;
                    ctxScoreNoteInsertSpecial.DeleteCommandParameter();
                    ctxScoreNoteModifySpecial.DeleteCommandParameter();
                    ctxScoreNoteDeleteSpecial.DeleteCommandParameter();
                    break;
                case VisualizerContextMenu.SpecialNoteAdd:
                    ctxSep1.Visible = false;
                    ctxScoreNoteResetToTap.Visible = false;
                    ctxScoreNoteDelete.Visible = false;
                    ctxSep2.Visible = false;
                    ctxScoreMeasureDelete.Visible = false;
                    ctxSep3.Visible = true;
                    ctxScoreNoteInsertSpecial.Visible = true;
                    ctxSep4.Visible = false;
                    ctxScoreNoteModifySpecial.Visible = false;
                    ctxScoreNoteDeleteSpecial.Visible = false;
                    ctxScoreNoteInsertSpecial.SetCommandParameter(e.HitTestResult);
                    ctxScoreNoteModifySpecial.DeleteCommandParameter();
                    ctxScoreNoteDeleteSpecial.DeleteCommandParameter();
                    break;
                case VisualizerContextMenu.SpecialNoteModify:
                    ctxSep1.Visible = false;
                    ctxScoreNoteResetToTap.Visible = false;
                    ctxScoreNoteDelete.Visible = false;
                    ctxSep2.Visible = false;
                    ctxScoreMeasureDelete.Visible = false;
                    ctxSep3.Visible = false;
                    ctxScoreNoteInsertSpecial.Visible = false;
                    ctxSep4.Visible = true;
                    ctxScoreNoteModifySpecial.Visible = true;
                    ctxScoreNoteDeleteSpecial.Visible = true;
                    ctxScoreNoteInsertSpecial.DeleteCommandParameter();
                    ctxScoreNoteModifySpecial.SetCommandParameter(e.HitTestResult);
                    ctxScoreNoteDeleteSpecial.SetCommandParameter(e.HitTestResult);
                    break;
                default:
                    break;
            }

            ctxMain.Show(visualizer, e.Location);
        }

        private void TsbScoreDifficultySelection_Click(object sender, EventArgs e) {
            var items = new ToolStripItem[mnuScoreDifficulty.DropDownItems.Count];
            mnuScoreDifficulty.DropDownItems.CopyTo(items, 0);
            tsbScoreDifficultySelection.DropDownItems.AddRange(items);
            tsbScoreDifficultySelection.DropDownClosed += DropDownClosed;
            tsbScoreDifficultySelection.ShowDropDown();

            void DropDownClosed(object s, EventArgs ev) {
                mnuScoreDifficulty.DropDownItems.AddRange(items);
                tsbScoreDifficultySelection.DropDownClosed -= DropDownClosed;
            }
        }

        private void PicIcon_MouseDown(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                if (e.Clicks == 1) {
                    var pt = picIcon.Location;
                    pt.Y = picIcon.Bottom;
                    pt = PointToScreen(pt);
                    var dispRect = RECT.FromRectangle(ClientRectangle);
                    var hMenu = NativeMethods.GetSystemMenu(Handle, false);
                    NativeMethods.TrackPopupMenu(hMenu, NativeConstants.TPM_LEFTBUTTON, pt.X, pt.Y, 0, Handle, ref dispRect);
                } else {
                    Close();
                }
            } else if (e.Button == MouseButtons.Right) {
                var mouseScreen = MousePosition;
                var dispRect = RECT.FromRectangle(ClientRectangle);
                var hMenu = NativeMethods.GetSystemMenu(Handle, false);
                NativeMethods.TrackPopupMenu(hMenu, NativeConstants.TPM_LEFTBUTTON, mouseScreen.X, mouseScreen.Y, 0, Handle, ref dispRect);
            }
        }

        private void SysMinimize_Click(object sender, EventArgs eventArgs) {
            WindowState = FormWindowState.Minimized;
        }

        private void SysMaximizeRestore_Click(object sender, EventArgs eventArgs) {
            WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
        }

        private void FMain_Load(object sender, EventArgs eventArgs) {
            MaximumSize = Screen.PrimaryScreen.WorkingArea.Size;
            using (var smallIcon = new Icon(Icon, new Size(16, 16))) {
                picIcon.InitialImage = picIcon.Image = smallIcon.ToBitmap();
                picIcon.ErrorImage = ToolStripRenderer.CreateDisabledImage(picIcon.InitialImage);
            }

            ApplyColorScheme(ColorScheme.Default);
            CursorFixup(this);

            mnuEditSelectClearAll.ShortcutKeyDisplayString = "Esc";
            mnuScoreNoteStartPositionAt0.ShortcutKeyDisplayString = "0";
            mnuScoreNoteStartPositionAt1.ShortcutKeyDisplayString = "1";
            mnuScoreNoteStartPositionAt2.ShortcutKeyDisplayString = "2";
            mnuScoreNoteStartPositionAt3.ShortcutKeyDisplayString = "3";
            mnuScoreNoteStartPositionAt4.ShortcutKeyDisplayString = "4";
            mnuScoreNoteStartPositionAt5.ShortcutKeyDisplayString = "5";
            mnuScoreNoteStartPositionTo0.ShortcutKeyDisplayString = "P";
            mnuScoreNoteStartPositionTo1.ShortcutKeyDisplayString = "Q";
            mnuScoreNoteStartPositionTo2.ShortcutKeyDisplayString = "W";
            mnuScoreNoteStartPositionTo3.ShortcutKeyDisplayString = "E";
            mnuScoreNoteStartPositionTo4.ShortcutKeyDisplayString = "R";
            mnuScoreNoteStartPositionTo5.ShortcutKeyDisplayString = "T";

            mnuEditModeSelect.ShortcutKeyDisplayString = "F";
            mnuEditModeTap.ShortcutKeyDisplayString = "A";
            mnuEditModeHoldFlick.ShortcutKeyDisplayString = "S";
            mnuEditModeSlide.ShortcutKeyDisplayString = "D";

            DirectorSettingsManager.LoadSettings();
            ApplySettings(DirectorSettingsManager.CurrentSettings);

            _liveControl = new LiveControl();
            _liveControl.PreloadWave(TapAudioFilePath);
            _liveControl.PreloadWave(FlickAudioFilePath);
            _liveControl.PreloadWave(SlideAudioFilePath);

            // Localize before setting command shortcut display strings.
            Localize(LanguageManager.Current);
            this.MonitorLocalizationChange();

            RegisterCommands();

            CmdProjectNew.Command.Execute(null);
            CmdScoreNoteStartPositionSetAt.Command.Execute(NotePosition.Default);

            if (Program.StartupOptions.BvspCommunicationEnabled) {
                _communication = new SDCommunication(this);
                _communication.Server.Start(0);
            }
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs eventArgs) {
            MaximumSize = Screen.PrimaryScreen.WorkingArea.Size;
        }

        private void LiveControl_Tick(object sender, EventArgs e) {
            var previewer = visualizer.Previewer;

            previewer.Now = _liveControl.CurrentTime;
        }

    }
}
