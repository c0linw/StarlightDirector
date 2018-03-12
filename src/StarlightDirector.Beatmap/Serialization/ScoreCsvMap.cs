﻿using System;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace StarlightDirector.Beatmap.Serialization {
    public sealed class ScoreCsvMap : CsvClassMap<CompiledNote> {

        public ScoreCsvMap() {
            Map(m => m.ID).Name("id");
            Map(m => m.HitTiming).Name("sec").TypeConverter<RestrictedDoubleToStringConverter>();
            Map(m => m.Type).Name("type").TypeConverter<StringToIntConverter>();
            // See song_3034 (m063), master level score. These fields are empty, so we need a custom type converter.
            Map(m => m.StartPosition).Name("startPos").TypeConverter<StringToIntConverter>();
            Map(m => m.FinishPosition).Name("finishPos").TypeConverter<StringToIntConverter>();
            Map(m => m.FlickType).Name("status").TypeConverter<StringToIntConverter>();
            Map(m => m.IsSync).Name("sync").TypeConverter<IntToBoolConverter>();
            Map(m => m.GroupID).Name("groupId");
        }

        private sealed class IntToBoolConverter : ITypeConverter {

            public string ConvertToString(TypeConverterOptions options, object value) {
                return (bool)value ? "1" : "0";
            }

            public object ConvertFromString(TypeConverterOptions options, string text) {
                if (string.IsNullOrEmpty(text)) {
                    return false;
                }
                var value = int.Parse(text);
                return value != 0;
            }

            public bool CanConvertFrom(Type type) {
                return true;
            }

            public bool CanConvertTo(Type type) {
                return true;
            }

        }

        private sealed class RestrictedDoubleToStringConverter : ITypeConverter {

            public string ConvertToString(TypeConverterOptions options, object value) {
                return ((double)value).ToString("0.######");
            }

            public object ConvertFromString(TypeConverterOptions options, string text) {
                if (string.IsNullOrEmpty(text)) {
                    return 0d;
                }
                return options.NumberStyle != null ? double.Parse(text, options.NumberStyle.Value) : double.Parse(text);
            }

            public bool CanConvertFrom(Type type) {
                return true;
            }

            public bool CanConvertTo(Type type) {
                return true;
            }

        }

        private sealed class StringToIntConverter : ITypeConverter {

            public string ConvertToString(TypeConverterOptions options, object value) {
                // The conversion is for enums.
                return ((int)value).ToString();
            }

            public object ConvertFromString(TypeConverterOptions options, string text) {
                if (string.IsNullOrEmpty(text)) {
                    return 0;
                }
                var value = int.Parse(text);
                return value;
            }

            public bool CanConvertFrom(Type type) {
                return true;
            }

            public bool CanConvertTo(Type type) {
                return true;
            }

        }

    }
}
