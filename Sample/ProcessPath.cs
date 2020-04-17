using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace Sample
{
    public class ProcessPath
    {
        public List<FilePath> lstPaths { get; set; }
    }
    public class FilePath
    {
        public string Deletepath { get; set; }
        public string PublishPath { get; set; }
        public string CSprojPath { get; set; }
        public string nuspecPath { get; set; }
    }
}
