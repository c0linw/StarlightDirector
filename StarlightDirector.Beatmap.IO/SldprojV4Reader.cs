﻿using System;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using StarlightDirector.Beatmap.Extensions;
using StarlightDirector.Core;

namespace StarlightDirector.Beatmap.IO {
    public sealed partial class SldprojV4Reader : ProjectReader {

        public override Project ReadProject(string fileName) {
            var fileInfo = new FileInfo(fileName);
            if (!fileInfo.Exists) {
                throw new IOException($"File '{fileName}' does not exist.");
            }
            if (fileInfo.Length == 0) {
                throw new IOException($"File '{fileName}' is empty.");
            }
            var builder = new SQLiteConnectionStringBuilder {
                DataSource = fileName
            };
            Project project;
            using (var db = new SQLiteConnection(builder.ToString())) {
                db.Open();
                project = ReadProject(db);
                db.Close();
            }
            project.SaveFileName = fileInfo.FullName;
            project.IsChanged = false;
            project.Version = ProjectVersion.Current;
            return project;
        }

        private static Project ReadProject(SQLiteConnection db) {
            var project = new Project();
            SQLiteCommand getValues = null;

            // Main
            var mainValues = SQLiteHelper.GetValues(db, Names.Table_Main, ref getValues);
            project.MusicFileName = mainValues[Names.Field_MusicFileName];
            var projectVersionString = mainValues[Names.Field_Version];
            float.TryParse(projectVersionString, out var fProjectVersion);
            if (fProjectVersion <= 0) {
                Debug.Print("WARNING: incorrect project version: {0}", projectVersionString);
                fProjectVersion = ProjectVersion.Current;
            }
            // 400 (v0.4)
            var projectVersion = (int)fProjectVersion;
            // Keep project.Version property being the latest project version.

            // Scores
            foreach (var difficulty in Difficulties) {
                var score = new Score(project, difficulty);
                ReadScore(db, score, projectVersion);
                ResolveReferences(score);
                FixSyncNotes(score);
                project.SetScore(difficulty, score);
            }

            // Score settings
            var scoreSettingsValues = SQLiteHelper.GetValues(db, Names.Table_ScoreSettings, ref getValues);
            var settings = project.Settings;
            settings.BeatPerMinute = double.Parse(scoreSettingsValues[Names.Field_GlobalBpm]);
            settings.StartTimeOffset = double.Parse(scoreSettingsValues[Names.Field_StartTimeOffset]);
            settings.GridPerSignature = int.Parse(scoreSettingsValues[Names.Field_GlobalGridPerSignature]);
            settings.Signature = int.Parse(scoreSettingsValues[Names.Field_GlobalSignature]);

            // Bar params
            if (SQLiteHelper.DoesTableExist(db, Names.Table_BarParams)) {
                foreach (var difficulty in Difficulties) {
                    var score = project.GetScore(difficulty);
                    ReadBarParams(db, score);
                }
            }

            // Special notes
            if (SQLiteHelper.DoesTableExist(db, Names.Table_SpecialNotes)) {
                foreach (var difficulty in Difficulties) {
                    var score = project.GetScore(difficulty);
                    ReadSpecialNotes(db, score);
                }
            }

            // Metadata (none for now)

            // Cleanup
            getValues.Dispose();

            return project;
        }

        private static void ReadScore(SQLiteConnection connection, Score score, int projectVersion) {
            using (var table = new DataTable()) {
                SQLiteHelper.ReadNotesTable(connection, score.Difficulty, table);
                // v0.3.1: "note_type"
                // Only flick existed when there is a flick-alike relation. Now, both flick and slide are possible.
                foreach (DataRow row in table.Rows) {
                    var id = new Guid((byte[])row[Names.Column_ID]);
                    var barIndex = (int)(long)row[Names.Column_BarIndex];
                    var grid = (int)(long)row[Names.Column_IndexInGrid];
                    var start = (NotePosition)(long)row[Names.Column_StartPosition];
                    var finish = (NotePosition)(long)row[Names.Column_FinishPosition];
                    var flick = (NoteFlickType)(long)row[Names.Column_FlickType];
                    var prevFlick = new Guid((byte[])row[Names.Column_PrevFlickNoteID]);
                    var nextFlick = new Guid((byte[])row[Names.Column_NextFlickNoteID]);
                    var prevSlide = new Guid((byte[])row[Names.Column_PrevSlideNoteID]);
                    var nextSlide = new Guid((byte[])row[Names.Column_NextSlideNoteID]);
                    var hold = new Guid((byte[])row[Names.Column_HoldTargetID]);
                    var noteType = (NoteType)(long)row[Names.Column_NoteType];

                    EnsureBarIndex(score, barIndex);
                    var bar = score.Bars[barIndex];
                    var note = bar.AddNote(id, grid, finish);
                    if (note != null) {
                        note.Basic.StartPosition = start;
                        note.Basic.Type = noteType;
                        note.Basic.FlickType = flick;
                        note.Temporary.PrevFlickNoteID = prevFlick;
                        note.Temporary.NextFlickNoteID = nextFlick;
                        note.Temporary.PrevSlideNoteID = prevSlide;
                        note.Temporary.NextSlideNoteID = nextSlide;
                        note.Temporary.HoldTargetID = hold;
                    } else {
                        Debug.Print("Note with ID '{0}' already exists.", id);
                    }
                }
            }
        }

        private static void ReadBarParams(SQLiteConnection connection, Score score) {
            using (var table = new DataTable()) {
                SQLiteHelper.ReadBarParamsTable(connection, score.Difficulty, table);
                foreach (DataRow row in table.Rows) {
                    var index = (int)(long)row[Names.Column_BarIndex];
                    var grid = (int?)(long?)row[Names.Column_GridPerSignature];
                    var signature = (int?)(long?)row[Names.Column_Signature];
                    if (index < score.Bars.Count) {
                        score.Bars[index].Params = new BarParams {
                            UserDefinedGridPerSignature = grid,
                            UserDefinedSignature = signature
                        };
                    }
                }
            }
        }

        private static void ReadSpecialNotes(SQLiteConnection connection, Score score) {
            using (var table = new DataTable()) {
                SQLiteHelper.ReadSpecialNotesTable(connection, score.Difficulty, table);
                foreach (DataRow row in table.Rows) {
                    var id = new Guid((byte[])row[Names.Column_ID]);
                    var barIndex = (int)(long)row[Names.Column_BarIndex];
                    var grid = (int)(long)row[Names.Column_IndexInGrid];
                    var type = (int)(long)row[Names.Column_NoteType];
                    var paramsString = (string)row[Names.Column_ParamValues];
                    if (barIndex >= score.Bars.Count) {
                        continue;
                    }
                    var bar = score.Bars[barIndex];
                    // Special notes are not added during the ReadScores() process, so we call AddNote() rather than AddNoteWithoutUpdatingGlobalNotes().
                    var note = bar.Notes.FirstOrDefault(n => n.Basic.Type == (NoteType)type && n.Basic.IndexInGrid == grid);
                    if (note == null) {
                        note = bar.AddSpecialNote(id, (NoteType)type);
                        note.Basic.IndexInGrid = grid;
                        note.Params = NoteExtraParams.FromDataString(paramsString, note);
                    } else {
                        note.Params.UpdateByDataString(paramsString);
                    }
                }
            }
        }

        private static void EnsureBarIndex(Score score, int index) {
            if (score.Bars.Count > index) {
                return;
            }
            for (var i = score.Bars.Count; i <= index; ++i) {
                var bar = new Bar(score, i);
                score.Bars.Add(bar);
            }
        }

        private static void ResolveReferences(Score score) {
            if (score.Bars == null) {
                return;
            }
            var allNotes = score.Bars.SelectMany(bar => bar.Notes).ToArray();
            foreach (var note in allNotes) {
                if (!note.Helper.IsGaming) {
                    continue;
                }
                if (note.Temporary.NextFlickNoteID != Guid.Empty) {
                    note.Editor.NextFlick = score.FindNoteByID(note.Temporary.NextFlickNoteID);
                }
                if (note.Temporary.PrevFlickNoteID != Guid.Empty) {
                    note.Editor.PrevFlick = score.FindNoteByID(note.Temporary.PrevFlickNoteID);
                }
                if (note.Temporary.NextSlideNoteID != Guid.Empty) {
                    note.Editor.NextSlide = score.FindNoteByID(note.Temporary.NextSlideNoteID);
                }
                if (note.Temporary.PrevSlideNoteID != Guid.Empty) {
                    note.Editor.PrevSlide = score.FindNoteByID(note.Temporary.PrevSlideNoteID);
                }
                if (note.Temporary.HoldTargetID != Guid.Empty) {
                    note.Editor.HoldPair = score.FindNoteByID(note.Temporary.HoldTargetID);
                }
            }
        }

        private static void FixSyncNotes(Score score) {
            foreach (var bar in score.Bars) {
                var gridIndexGroups =
                    from n in bar.Notes
                    where n.Helper.IsGaming
                    group n by n.Basic.IndexInGrid
                    into g
                    select g;
                foreach (var group in gridIndexGroups) {
                    var sortedNotesInGroup =
                        from n in @group
                        orderby n.Basic.FinishPosition
                        select n;
                    Note prev = null;
                    foreach (var note in sortedNotesInGroup) {
                        NoteUtilities.MakeSync(prev, note);
                        prev = note;
                    }
                    NoteUtilities.MakeSync(prev, null);
                }
            }
        }

        private static readonly Difficulty[] Difficulties = { Difficulty.Debut, Difficulty.Regular, Difficulty.Pro, Difficulty.Master, Difficulty.MasterPlus };

    }
}