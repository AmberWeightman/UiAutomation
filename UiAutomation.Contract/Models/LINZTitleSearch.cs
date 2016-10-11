using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace UiAutomation.Contract.Models
{
    [DataContract]
    [Flags]
    public enum LINZTitleSearchType
    {
        [EnumMember]
        TitleSearchWithDiagram = 0,
        [EnumMember]
        TitleSearchNoDiagram = 1,
        [EnumMember]
        Historical = 2,
        [EnumMember]
        Guaranteed = 3,
    }

    [DataContract]
    public class LINZTitleSearch
    {
        [DataMember]
        public string TitleReference { get; set; }

        [DataMember]
        public LINZTitleSearchType Type { get; set; }

        [DataMember]
        public string OrderId { get; set; }

        [DataMember]
        public string OutputDirectory { get; set; }

        //[DataMember]
        //public string ScreenshotDirectoryPath { get; set; }

        #region properties set on response

        [DataMember]
        public bool Success { get; set; }

        [DataMember]
        public List<string> OutputFilePaths { get; private set; }
        
        [DataMember]
        public List<SearchResult> SearchResults { get; set; }

        [DataMember]
        public List<string> Errors { get; set; }

        [DataMember]
        public List<string> Warnings { get; set; }

        //[DataMember]
        //public Exception Exception { get; set; }

        [DataMember]
        public string ExceptionType { get; set; }

        [DataMember]
        public string ExceptionMsg { get; set; }

        #endregion

        public void AddOutputFilePath(string filePath)
        {
            if(OutputFilePaths == null)
            {
                OutputFilePaths = new List<string>();
            }
            OutputFilePaths.Add(filePath);
        }
    }

    [DataContract]
    public class LINZTitleSearchBatch
    {
        [DataMember]
        public string AutoOrderBulkId { get; set; }

        [DataMember]
        public string ScreenshotDirectoryPath { get; set; }

        [DataMember]
        public LINZTitleSearch[] TitleSearchRequests { get; set; }

        //[DataMember]
        //public Exception Exception { get; set; }

        [DataMember]
        public string ExceptionType { get; set; }

        [DataMember]
        public string ExceptionMsg { get; set; }
    }
}
