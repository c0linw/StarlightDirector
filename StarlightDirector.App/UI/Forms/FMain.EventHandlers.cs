﻿using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;
using StarlightDirector.Core.Interop;
using StarlightDirector.UI.Controls;

namespace StarlightDirector.App.UI.Forms {
    partial class FMain {

        private void UnregisterEventHandlers() {
            SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
            Load -= FMain_Load;
            sysMaximizeRestore.Click -= SysMaximizeRestore_Click;
            sysMinimize.Click -= SysMinimize_Click;
            picIcon.MouseDown -= PicIcon_MouseDown;
            tsbDifficultySelection.Click -= TsbDifficultySelection_Click;
            visualizer.ContextMenuRequested -= Visualizer_ContextMenuRequested;
            tsbEditMode.Click -= TsbEditMode_Click;
            tsbEditNoteStartPosition.Click -= TsbEditNoteStartPosition_Click;
        }

        private void RegisterEventHandlers() {
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
            Load += FMain_Load;
            sysMaximizeRestore.Click += SysMaximizeRestore_Click;
            sysMinimize.Click += SysMinimize_Click;
            picIcon.MouseDown += PicIcon_MouseDown;
            tsbDifficultySelection.Click += TsbDifficultySelection_Click;
            visualizer.ContextMenuRequested += Visualizer_ContextMenuRequested;
            tsbEditMode.Click += TsbEditMode_Click;
            tsbEditNoteStartPosition.Click += TsbEditNoteStartPosition_Click;
        }

        private void TsbEditNoteStartPosition_Click(object sender, EventArgs e) {
            var items = new ToolStripItem[mnuEditNoteStartPosition.DropDownItems.Count];
            mnuEditNoteStartPosition.DropDownItems.CopyTo(items, 0);
            tsbEditNoteStartPosition.DropDownItems.AddRange(items);
            tsbEditNoteStartPosition.DropDownClosed += DropDownClosed;
            tsbEditNoteStartPosition.ShowDropDown();

            void DropDownClosed(object s, EventArgs ev)
            {
                mnuEditNoteStartPosition.DropDownItems.AddRange(items);
                tsbEditNoteStartPosition.DropDownClosed -= DropDownClosed;
            }
        }

        private void TsbEditMode_Click(object sender, EventArgs e) {
            var items = new ToolStripItem[mnuEditMode.DropDownItems.Count];
            mnuEditMode.DropDownItems.CopyTo(items, 0);
            tsbEditMode.DropDownItems.AddRange(items);
            tsbEditMode.DropDownClosed += DropDownClosed;
            tsbEditMode.ShowDropDown();

            void DropDownClosed(object s, EventArgs ev)
            {
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
                    ctxEditDeleteNotes.Visible = hasSelectedNotes;
                    ctxSep2.Visible = false;
                    ctxEditDeleteMeasures.Visible = false;
                    ctxSep3.Visible = false;
                    ctxEditAddSpecialNote.Visible = false;
                    ctxSep4.Visible = false;
                    ctxEditModifySpecialNote.Visible = false;
                    break;
                case VisualizerContextMenu.Bar:
                    ctxSep1.Visible = false;
                    ctxEditDeleteNotes.Visible = false;
                    ctxSep2.Visible = hasSelectedBars;
                    ctxEditDeleteMeasures.Visible = hasSelectedBars;
                    ctxSep3.Visible = false;
                    ctxEditAddSpecialNote.Visible = false;
                    ctxSep4.Visible = false;
                    ctxEditModifySpecialNote.Visible = false;
                    break;
                case VisualizerContextMenu.SpecialNoteAdd:
                    ctxSep1.Visible = false;
                    ctxEditDeleteNotes.Visible = false;
                    ctxSep2.Visible = false;
                    ctxEditDeleteMeasures.Visible = false;
                    ctxSep3.Visible = true;
                    ctxEditAddSpecialNote.Visible = true;
                    ctxSep4.Visible = false;
                    ctxEditModifySpecialNote.Visible = false;
                    break;
                case VisualizerContextMenu.SpecialNoteModify:
                    ctxSep1.Visible = false;
                    ctxEditDeleteNotes.Visible = false;
                    ctxSep2.Visible = false;
                    ctxEditDeleteMeasures.Visible = false;
                    ctxSep3.Visible = false;
                    ctxEditAddSpecialNote.Visible = false;
                    ctxSep4.Visible = true;
                    ctxEditModifySpecialNote.Visible = true;
                    break;
                default:
                    break;
            }

            ctxMain.Show(visualizer, e.Location);
        }

        private void TsbDifficultySelection_Click(object sender, EventArgs e) {
            var items = new ToolStripItem[mnuEditDifficulty.DropDownItems.Count];
            mnuEditDifficulty.DropDownItems.CopyTo(items, 0);
            tsbDifficultySelection.DropDownItems.AddRange(items);
            tsbDifficultySelection.DropDownClosed += DropDownClosed;
            tsbDifficultySelection.ShowDropDown();

            void DropDownClosed(object s, EventArgs ev)
            {
                mnuEditDifficulty.DropDownItems.AddRange(items);
                tsbDifficultySelection.DropDownClosed -= DropDownClosed;
            }
        }

        private void PicIcon_MouseDown(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                if (e.Clicks == 1) {
                    var pt = picIcon.Location;
                    pt.Y = picIcon.Bottom;
                    pt = PointToScreen(pt);
                    var dispRect = NativeStructures.RECT.FromRectangle(ClientRectangle);
                    var hMenu = NativeMethods.GetSystemMenu(Handle, false);
                    NativeMethods.TrackPopupMenu(hMenu, NativeConstants.TPM_LEFTBUTTON, pt.X, pt.Y, 0, Handle, ref dispRect);
                } else {
                    Close();
                }
            } else if (e.Button == MouseButtons.Right) {
                var mouseScreen = MousePosition;
                var dispRect = NativeStructures.RECT.FromRectangle(ClientRectangle);
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
            mnuEditNoteStartPositionAt0.ShortcutKeyDisplayString = "0";
            mnuEditNoteStartPositionAt1.ShortcutKeyDisplayString = "1";
            mnuEditNoteStartPositionAt2.ShortcutKeyDisplayString = "2";
            mnuEditNoteStartPositionAt3.ShortcutKeyDisplayString = "3";
            mnuEditNoteStartPositionAt4.ShortcutKeyDisplayString = "4";
            mnuEditNoteStartPositionAt5.ShortcutKeyDisplayString = "5";

            CmdFileNew.Execute(null, null);
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs eventArgs) {
            MaximumSize = Screen.PrimaryScreen.WorkingArea.Size;
        }

    }
}
