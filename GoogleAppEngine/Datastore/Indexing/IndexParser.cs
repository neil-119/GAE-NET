using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using GoogleAppEngine.Datastore;

namespace GoogleAppEngine.Datastore.Indexing
{
    // Very crude YAML index file parser
    internal static class IndexParser
    {
        private const string KindIdentifier = "kind";
        private const string AncestorIdentifier = "ancestor";
        private const string PropertiesIdentifier = "properties";
        private const string NameIdentifier = "name";
        private const string DirectionIdentifier = "direction";

        private static string GetValue(string line)
        {
            return line.Substring(line.IndexOf(":", StringComparison.Ordinal) + 1).Trim();
        }

        /// <summary>
        /// Parses raw YAML into a list of indexes.
        /// </summary>
        /// <param name="lines">YAML lines</param>
        /// <returns></returns>
        public static List<Index> Deserialize(string[] lines)
        {
            var indexes = new List<Index>();

            var isPropertiesRegion = false;
            var index = new Index();

            // Parse lines
            foreach (var line in lines)
            {
                // Crude way to detect kind
                if (line.Contains($"- {KindIdentifier}:"))
                {
                    // Create a new index definition if we reach a new kind definition
                    if (!string.IsNullOrWhiteSpace(index.Kind))
                        indexes.Add(index);
                    
                    index = new Index {Kind = GetValue(line), Properties = new List<Index.IndexProperty>() };
                    isPropertiesRegion = false;
                }
                else if (line.Contains($"{AncestorIdentifier}:"))
                {
                    index.IsAncestor = GetValue(line) == "yes";
                }
                else if (line.Contains($"{PropertiesIdentifier}:"))
                {
                    // Very crude because any line after this is considered a "property"
                    isPropertiesRegion = true;
                }
                else if (isPropertiesRegion && line.Contains($"{NameIdentifier}:"))
                {
                    index.Properties.Add(new Index.IndexProperty { PropertyName = GetValue(line) });
                }
                else if (isPropertiesRegion && line.Contains($"{DirectionIdentifier}:"))
                {
                    index.Properties.Last().OrderingType = GetValue(line) == "asc" ? Index.OrderingType.Ascending : Index.OrderingType.Descending;
                }
            }

            if (!string.IsNullOrWhiteSpace(index.Kind))
                indexes.Add(index);

            return indexes;
        }

        /// <summary>
        /// Serializes index list into YAML
        /// </summary>
        /// <param name="indexes"></param>
        /// <returns></returns>
        public static string Serialize(IEnumerable<Index> indexes)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"indexes:");

            foreach (var index in indexes)
            {
                sb.AppendLine($"- {KindIdentifier}: {index.Kind}");
                sb.AppendLine($"  {AncestorIdentifier}: {(index.IsAncestor ? "yes" : "no")}");
                sb.AppendLine($"  {PropertiesIdentifier}:");

                foreach (var prop in index.Properties)
                {
                    sb.AppendLine($"  - {NameIdentifier}: {prop.PropertyName}");

                    switch (prop.OrderingType)
                    {
                        case Index.OrderingType.Ascending:
                            sb.AppendLine($"    {DirectionIdentifier}: asc");
                            break;

                        case Index.OrderingType.Descending:
                            sb.AppendLine($"    {DirectionIdentifier}: desc");
                            break;
                    }
                }
            }

            return sb.ToString();
        }
    }
}
