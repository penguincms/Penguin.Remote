using Penguin.Remote.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Penguin.Remote.Responses
{
    public class EnumerateFilesResponse : ServerResponse
    {
        [DontSerialize]
        public IReadOnlyList<FileData> Files
        {
            get
            {
                List<FileData> toReturn = new();

                if (string.IsNullOrWhiteSpace(this.Text) || !this.Success)
                {
                    return toReturn;
                }

                foreach(string fileMeta in this.Text.Split(System.Environment.NewLine))
                {
                    string path = fileMeta.Split('\t')[0];

                    long length = long.Parse(fileMeta.Split('\t')[1]);

                    toReturn.Add(new FileData()
                    {
                        FileName = path,
                        Length = length
                    });
                }

                return toReturn;
            }
            set
            {
                StringBuilder sb = new();

                if(value is null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                for(int i = 0; i < value.Count; i++)
                {
                    if(i != 0)
                    {
                        _ = sb.Append(System.Environment.NewLine);
                    }

                    _ = sb.Append(value[i].FileName);
                    _ = sb.Append('\t');
                    _ = sb.Append(value[i].Length);
                }

                this.Text = sb.ToString();
            }
        }
    }
}
