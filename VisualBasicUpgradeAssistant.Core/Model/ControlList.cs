using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using VisualBasicUpgradeAssistant.Core.DataClasses;

namespace VisualBasicUpgradeAssistant.Core.Model
{
    /// <summary>
    /// This controls the read / write of the json file
    /// </summary>
    [DataContract]
    public class ControlList
    {
        /// <summary>
        /// List of controls for conversion
        /// </summary>
        [DataMember]
        public IList<Controltem> Controls { get; set; } = new List<Controltem>();

        /// <summary>
        /// Read data from given file
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static ControlList ReadData(FileInfo file)
        {
            // Sanity check
            if (file is null)
                throw new ArgumentNullException(nameof(file));
            if (!file.Exists)
                throw new FileNotFoundException(nameof(file), file.FullName);

            String json = File.ReadAllText(file.FullName);
            return JsonConvert.DeserializeObject<ControlList>(json);
        }

        /// <summary>
        /// Write the data to given file
        /// </summary>
        /// <param name="fileInfo"></param>
        public void WriteData(FileInfo fileInfo)
        {
            String json = JsonConvert.SerializeObject(this);
            File.WriteAllText(fileInfo.FullName, json);
            fileInfo.Refresh();
        }
    }
}
