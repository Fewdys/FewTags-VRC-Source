using FewTags.FewTags;
using Utf8Json;
using Utf8Json.Formatters;
using Utf8Json.Resolvers;

namespace FewTags.FewTags_Rewrite_V2
{
    public sealed class TagsFormatter : IJsonFormatter<Jsons.Json.Tags>
    {
        public void Serialize(ref JsonWriter writer, Jsons.Json.Tags value, IJsonFormatterResolver formatterResolver)
        {
            if (value == null) { writer.WriteNull(); return; }

            writer.WriteBeginObject();

            writer.WritePropertyName("id"); writer.WriteInt64(value.id);
            writer.WriteValueSeparator();
            writer.WritePropertyName("UserID"); writer.WriteString(value.UserID);
            writer.WriteValueSeparator();
            writer.WritePropertyName("PlateText"); writer.WriteString(value.PlateText);
            writer.WriteValueSeparator();
            writer.WritePropertyName("PlateBigText"); writer.WriteString(value.PlateBigText);
            writer.WriteValueSeparator();
            writer.WritePropertyName("Malicious"); writer.WriteBoolean(value.Malicious);
            writer.WriteValueSeparator();
            writer.WritePropertyName("Active"); writer.WriteBoolean(value.Active);
            writer.WriteValueSeparator();
            writer.WritePropertyName("TextActive"); writer.WriteBoolean(value.TextActive);
            writer.WriteValueSeparator();
            writer.WritePropertyName("BigTextActive"); writer.WriteBoolean(value.BigTextActive);
            writer.WriteValueSeparator();
            writer.WritePropertyName("Size"); writer.WriteString(value.Size);
            writer.WriteValueSeparator();
            writer.WritePropertyName("Tag"); WriteStringArray(ref writer, value.Tag);

            writer.WriteEndObject();
        }

        public Jsons.Json.Tags Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
        {
            if (reader.ReadIsNull()) return null;

            var result = new Jsons.Json.Tags();
            reader.ReadIsBeginObjectWithVerify();
            var count = 0;
            while (!reader.ReadIsEndObjectWithSkipValueSeparator(ref count))
            {
                var key = reader.ReadPropertyName();
                switch (key)
                {
                    case "id": result.id = reader.ReadInt64(); break;
                    case "UserID": result.UserID = reader.ReadString(); break;
                    case "PlateText": result.PlateText = reader.ReadString(); break;
                    case "PlateBigText": result.PlateBigText = reader.ReadString(); break;
                    case "Malicious": result.Malicious = reader.ReadBoolean(); break;
                    case "Active": result.Active = reader.ReadBoolean(); break;
                    case "TextActive": result.TextActive = reader.ReadBoolean(); break;
                    case "BigTextActive": result.BigTextActive = reader.ReadBoolean(); break;
                    case "Size": result.Size = reader.ReadString(); break;
                    case "Tag": result.Tag = ReadStringArray(ref reader); break;
                    default: reader.ReadNextBlock(); break;
                }
            }
            return result;
        }

        private static void WriteStringArray(ref JsonWriter writer, string[] array)
        {
            if (array == null) { writer.WriteNull(); return; }
            writer.WriteBeginArray();
            for (int i = 0; i < array.Length; i++)
            {
                if (i != 0) writer.WriteValueSeparator();
                writer.WriteString(array[i]);
            }
            writer.WriteEndArray();
        }

        private static string[] ReadStringArray(ref JsonReader reader)
        {
            if (reader.ReadIsNull()) return null;

            reader.ReadIsBeginArrayWithVerify();
            var list = new List<string>();
            var count = 0;
            while (!reader.ReadIsEndArrayWithSkipValueSeparator(ref count))
                list.Add(reader.ReadString());

            return list.ToArray();
        }
    }

    public sealed class TagsContainerFormatter : IJsonFormatter<Jsons.Json._Tags>
    {
        public void Serialize(ref JsonWriter writer, Jsons.Json._Tags value, IJsonFormatterResolver formatterResolver)
        {
            if (value == null) { writer.WriteNull(); return; }

            var tagsFormatter = formatterResolver.GetFormatterWithVerify<Jsons.Json.Tags>();

            writer.WriteBeginObject();
            writer.WritePropertyName("records");

            if (value.records == null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteBeginArray();
                for (int i = 0; i < value.records.Count; i++)
                {
                    if (i != 0) writer.WriteValueSeparator();
                    tagsFormatter.Serialize(ref writer, value.records[i], formatterResolver);
                }
                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }

        public Jsons.Json._Tags Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
        {
            if (reader.ReadIsNull()) return null;

            var tagsFormatter = formatterResolver.GetFormatterWithVerify<Jsons.Json.Tags>();
            var result = new Jsons.Json._Tags();

            reader.ReadIsBeginObjectWithVerify();
            var count = 0;
            while (!reader.ReadIsEndObjectWithSkipValueSeparator(ref count))
            {
                var key = reader.ReadPropertyName();
                switch (key)
                {
                    case "records":
                        result.records = ReadRecords(ref reader, tagsFormatter, formatterResolver);
                        break;
                    default:
                        reader.ReadNextBlock();
                        break;
                }
            }
            return result;
        }

        private static List<Jsons.Json.Tags> ReadRecords(ref JsonReader reader, IJsonFormatter<Jsons.Json.Tags> tagsFormatter, IJsonFormatterResolver formatterResolver)
        {
            if (reader.ReadIsNull()) return null;

            reader.ReadIsBeginArrayWithVerify();
            var list = new List<Jsons.Json.Tags>();
            var count = 0;
            while (!reader.ReadIsEndArrayWithSkipValueSeparator(ref count))
                list.Add(tagsFormatter.Deserialize(ref reader, formatterResolver));

            return list;
        }
    }

    /// <summary>
    /// IL2CPP-safe replacement for StandardResolver.AllowPrivate.
    /// No DynamicObjectResolver, no Reflection.Emit — safe to use on background threads too.
    /// </summary>
    public static class FewTagsResolver
    {
        private static bool _registered;
        private static readonly object _lock = new object();

        public static void EnsureRegistered()
        {
            if (_registered) return;
            lock (_lock)
            {
                if (_registered) return;

                CompositeResolver.RegisterAndSetAsDefault(
                    new IJsonFormatter[]
                    {
                        new TagsFormatter(),
                        new TagsContainerFormatter(),
                        new ListFormatter<Jsons.Json.Tags>()
                    },
                    new IJsonFormatterResolver[] { BuiltinResolver.Instance }
                );

                _registered = true;
            }
        }
    }
}
