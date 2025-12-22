using Octokit;
using src.common_index;
using System;
using System.IO;
using System.Net.Http;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;


namespace src.utils
{

    internal static class YamlSerializer
    {
        private static readonly IDeserializer Deserializer;
        private static readonly ISerializer Serializer;

        static YamlSerializer()
        {
            Deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            Serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                .Build();
        }
        public static T Deserialize<T>(string filePath)
        {
            using FileStream stream = new FileStream(filePath, System.IO.FileMode.Open, FileAccess.Read, FileShare.Read);
            return Deserialize<T>(stream);
        }

        public static T Deserialize<T>(string owner, string repo, string filePath)
        {
            string url = "https://raw.githubusercontent.com/" + owner + "/" + repo + "/master/" + $"{filePath}";

            using HttpClient client = new HttpClient();
            HttpResponseMessage response = client.GetAsync(url).Result;

            response.EnsureSuccessStatusCode();

            using Stream stream = response.Content.ReadAsStreamAsync().Result;
            return Deserialize<T>(stream);
        }

        public static T Deserialize<T>(string sourcePath, string filePath)
        {
            MemoryStream? compressedStream = ExtractUtils.ReadFile(sourcePath, filePath);
            if (compressedStream == null)
                throw new ArgumentNullException(nameof(compressedStream));
            return Deserialize<T>(compressedStream);
        }

        public static T Deserialize<T>(Stream yamlStream)
        {
            if (yamlStream == null)
            {
                throw new ArgumentNullException(nameof(yamlStream));
            }

            using (StreamReader reader = new StreamReader(yamlStream, leaveOpen: true))
            {
                return Deserializer.Deserialize<T>(reader);
            }
        }

        public static Stream Serialize<T>(T value)
        {
        
            Stream stream = new MemoryStream();
            using (StreamWriter writer = new StreamWriter(stream, leaveOpen: true))
            {
                Serializer.Serialize(writer, value);
                writer.Flush();
                stream.Position = 0;
            }
            return stream;
        }

        public static void Serialize<T>(T value, string filePath)
        {
            filePath =  Path.ChangeExtension(filePath, ".yml");
            PathUtils.EnsureExists(filePath);
            using FileStream fileStream = new FileStream(filePath, System.IO.FileMode.Create, FileAccess.Write, FileShare.None);
            Serialize(value).CopyTo(fileStream);
        }
    }

}
