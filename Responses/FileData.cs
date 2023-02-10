using Penguin.Remote.Attributes;
using System.IO;

namespace Penguin.Remote.Responses
{
    public class FileData
    {
        public FileData()
        {
        }

        public FileData(string path)
        {
            FileInfo fi = new(path);
            this.FileName = fi.FullName;
            this.Length = fi.Length;
        }

        [SerializationData(size: 255)]
        public string FileName { get; set; }

        public long Length { get; set; }
    }
}