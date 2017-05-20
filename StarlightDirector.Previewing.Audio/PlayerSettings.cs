﻿using System;
using StarlightDirector.Core;

namespace StarlightDirector.Previewing.Audio {
    internal static class PlayerSettings {

        public static float MusicVolume {
            get => _musicVolume;
            set {
                value = value.Clamp(0f, 1f);
                var b = !value.Equals(_musicVolume);
                if (b) {
                    _musicVolume = value;
                    MusicVolumeChanged?.Invoke(null, EventArgs.Empty);
                }
            }
        }

        public static event EventHandler<EventArgs> MusicVolumeChanged;

        public static float SfxVolume {
            get => _sfxVolume;
            set {
                value = value.Clamp(0f, 1f);
                _sfxVolume = value;
            }
        }

        // Compensates for systematic offset of official scores, which turns out to be very close to zero
        public static TimeSpan GlobalOffset { get; set; } = TimeSpan.Zero;

        // Compensates for a future ~3ms (128 samples) HCA encoder delay on WAV music files
        public static TimeSpan MusicFileOffset { get; set; } = TimeSpan.Zero;

        // Total offset
        public static TimeSpan SfxOffset => GlobalOffset + MusicFileOffset;

        private static float _musicVolume = 0.7f;
        private static float _sfxVolume = 0.5f;

    }
}