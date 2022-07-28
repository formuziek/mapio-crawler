namespace Mapio.Dto.Configuration
{
    using Mapio.Shared.Enums;
    using System.Collections.Generic;

    /// <summary>
    /// Configuration for a data set.
    /// </summary>
    public class DataSetConfiguration
    {
        /// <summary>
        /// Dataset version.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Display name in English.
        /// </summary>
        public string TextEng { get; set; }

        /// <summary>
        /// Display name in Latvian.
        /// </summary>
        public string TextLat { get; set; }

        /// <summary>
        /// Data set type.
        /// </summary>
        public DataSetTypes DataSetType { get; set; }

        /// <summary>
        /// Indicates whether the data set supports (read - requires) quarterly searches.
        /// </summary>
        public bool IsQuarterly { get; set; }

        /// <summary>
        /// URI for the dataset.
        /// </summary>
        public string Uri { get; set; }

        /// <summary>
        /// Query parameters for the dataset.
        /// </summary>
        public List<string> QuerySections { get; set; }

        /// <summary>
        /// Default or replacable query values for the dataset.
        /// </summary>
        public List<List<string>> QueryValues { get; set; }

        /// <summary>
        /// List of years the dataset supports.
        /// </summary>
        public List<string> Years { get; set; }

        /// <summary>
        /// List of quarters the dataset supports.
        /// </summary>
        public List<string> Quarters { get; set; }

        /// <summary>
        /// List of quarters the dataset supports in the first available year.
        /// </summary>
        public List<string> FirstYearQuarters { get; set; }

        /// <summary>
        /// List of quarters the dataset supports in the last available year.
        /// </summary>
        public List<string> LastYearQuarters { get; set; }

        /// <summary>
        /// Data set type name for vue.
        /// </summary>
        public string DataSetTypeName { get { return this.TextLat; } }
    }
}
