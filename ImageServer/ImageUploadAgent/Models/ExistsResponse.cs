using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageUploadAgent.Models
{
    public class ExistsResponse
    {
        public int Result { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool Exists { get; set; }
        public string FileName { get; set; } = string.Empty;
    }
}
