using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace src.common_index
{
    
    internal class CommonIndex
    {
        #region STREAM
        public static IndexView ReadStreamIndex(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            

            IDeserializer deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            using StreamReader reader = new StreamReader(stream, leaveOpen: true);

            IndexView indexView = deserializer.Deserialize<IndexView>(reader);

            if (indexView == null)
                throw new InvalidDataException("Failed to deserialize IndexView from stream.");
            
            return indexView;
        }
        public static Stream WriteStreamIndex(IndexView index)
        {
            ISerializer serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);

            serializer.Serialize(writer, index);
            writer.Flush();
            stream.Position = 0;

            return stream;
        }
        #endregion

        #region FILE
        public static IndexView ReadFileIndex(string filePath)
        {
            using FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return ReadStreamIndex(stream);
        }

        public static void WriteFileIndex(IndexView index, string filePath)
        {
            using Stream stream = WriteStreamIndex(index);
            using FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            stream.CopyTo(fileStream);
        }

        #endregion
        #region REMOTE
        public static IndexView ReadRemoteIndex(string owner, string repo)
        {
            string url = "https://raw.githubusercontent.com/" + owner + "/" + repo + "/master/premakeIndex.yml";

            using HttpClient client = new HttpClient();
            HttpResponseMessage response = client.GetAsync(url).Result;

            response.EnsureSuccessStatusCode();

            using Stream stream = response.Content.ReadAsStreamAsync().Result;
            return ReadStreamIndex(stream);
        }

        #endregion
        public static IndexView CreateNew(string remoteName)
        {
            return new IndexView()
            {
                remote = remoteName,
                libraries = new Dictionary<string, IList<IndexLibrary>>()
            };
        }
    }
}
