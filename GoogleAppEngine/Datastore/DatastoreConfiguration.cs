using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleAppEngine.Datastore
{
    public class DatastoreConfiguration
    {
        /// <summary>
        /// Whether to generate indexes automatically or not. See the project wiki for more details.
        /// </summary>
        public bool GenerateIndexYAMLFile { get; set; } = false;

        /// <summary>
        /// If generating indexes automatically, where to save the file. By default, it's saved as "index.yaml" in the current working directory.
        /// </summary>
        public string IndexFileLocation { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "index.yaml");

        /// <summary>
        /// Whether to convert advanced queries (queries which Datastore cannot handle) to in-memory queries
        /// </summary>
        public bool AllowQueriesInMemory { get; set; } = false; // Does nothing yet. TODO work on this this!

        /// <summary>
        /// If true, any auto-generated ID is checked to ensure that the id does not already exist in the database.
        /// </summary>
        public bool DoubleCheckGeneratedIds { get; set; } = false;
    }
}
