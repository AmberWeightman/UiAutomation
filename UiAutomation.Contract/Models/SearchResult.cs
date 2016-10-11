using System;
using System.Runtime.Serialization;

namespace UiAutomation.Contract.Models
{
    [DataContract]
    public class SearchResult
    {
        [DataMember]
        public string CT { get; set; }

        [DataMember]
        public string Owner { get; set; }

        [DataMember]
        public string Status { get; set; }

        [DataMember]
        public string PotentiallyMaoriLand { get; set; }

        [DataMember]
        public string LegalDescription { get; set; }

        [DataMember]
        public string IndicativeArea { get; set; }

        [DataMember]
        public string LandDistrict { get; set; }

        [DataMember]
        public string TimeshareWeek { get; set; }

        public SearchResult(string tabSeparatedData)
        {
            if (string.IsNullOrEmpty(tabSeparatedData)) return;

            var values = tabSeparatedData.Split(new char[] { '\t' }, StringSplitOptions.None); // do not remove empty entries
            if(values.Length > 0) CT = values[0];
            if (values.Length > 1) Owner = values[1];
            if (values.Length > 2) Status = values[2];
            if (values.Length > 3) PotentiallyMaoriLand = values[3];
            if (values.Length > 4) LegalDescription = values[4];
            if (values.Length > 5) IndicativeArea = values[5];
            if (values.Length > 6) LandDistrict = values[6];
            if (values.Length > 7) TimeshareWeek = values[7];
        }
    }

}
