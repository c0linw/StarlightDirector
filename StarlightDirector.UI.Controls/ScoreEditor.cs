﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using StarlightDirector.Beatmap;
using StarlightDirector.Beatmap.Extensions;
using StarlightDirector.UI.Rendering.Direct2D;
using JetBrains.Annotations;

namespace StarlightDirector.UI.Controls {
    public sealed partial class ScoreEditor : Direct2DCanvas {

        [Browsable(false)]
        public ScrollBar ScrollBar { get; internal set; }

        [Browsable(false)]
        public Project Project {
            get => _project;
            set {
                var b = value != _project;
                if (b) {
                    _project = value;
                    RecalcLayout();
                    Invalidate();
                }
            }
        }

        [Browsable(false)]
        public Difficulty Difficulty {
            get => _difficulty;
            set {
                var b = value != _difficulty;
                if (b) {
                    _difficulty = value;
                    RecalcLayout();
                    Invalidate();
                }
            }
        }

        [Browsable(false)]
        [CanBeNull]
        public Score CurrentScore {
            [DebuggerStepThrough]
            get { return Project?.GetScore(Difficulty); }
        }

        [Browsable(false)]
        public int ScrollOffsetX { get; set; }

        [Browsable(false)]
        public int ScrollOffsetY {
            get => _scrollOffsetY;
            set {
                var b = value != _scrollOffsetY;
                if (b) {
                    _scrollOffsetY = value;
                    Invalidate();
                }
            }
        }

        [Browsable(false)]
        public float BarLineSpaceUnit {
            get => _barLineSpaceUnit;
            set {
                var b = !value.Equals(_barLineSpaceUnit);
                if (b) {
                    _barLineSpaceUnit = value;
                    RecalcLayout();
                    Invalidate();
                }
            }
        }

        [Browsable(false)]
        public bool IndicatorsVisible { get; set; } = true;

        [Browsable(false)]
        public PrimaryBeatMode PrimaryBeatMode { get; set; } = PrimaryBeatMode.EveryFourBeats;

        [Browsable(false)]
        public ScoreEditorConfig Config { get; } = new ScoreEditorConfig();

        public float GetFullHeight() {
            var score = CurrentScore;
            if (score == null) {
                return 0;
            }
            var height = score.Bars.Sum(bar => BarLineSpaceUnit * bar.GetNumberOfGrids());
            return height;
        }

        internal ScoreEditor() {
        }

        protected override void OnClientSizeChanged(EventArgs e) {
            base.OnClientSizeChanged(e);
            RecalcLayout();
        }

        protected override void OnRender(D2DRenderContext context) {
            var score = CurrentScore;
            var hasAnyBar = score?.HasAnyBar ?? false;
            if (hasAnyBar) {
                RenderBars(context, score);
            }
            var hasAnyNote = score?.HasAnyNote ?? false;
            if (hasAnyNote) {
                RenderNotes(context, score);
            }
        }

        // This is used for scaling. It can be different with signature*gps.
        private static readonly int MaxNumberOfGrids = 96;

        private int _scrollOffsetY;
        private Project _project;
        private Difficulty _difficulty = Difficulty.Debut;
        private static readonly float DefaultBarLineSpaceUnit = 7;
        private float _barLineSpaceUnit = DefaultBarLineSpaceUnit;

    }
}
