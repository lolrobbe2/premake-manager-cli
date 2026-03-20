using EasyCaching.Core;
using EasyCaching.Core.Serialization;
using EasyCaching.Disk;
using EasyCaching.Serialization.SystemTextJson;
using Newtonsoft.Json;
using Octokit.Caching;
using Octokit.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace src.utils
{


    public class NewtonsoftEasyCachingSerializer : IEasyCachingSerializer
    {
        private readonly JsonSerializerSettings _settings;

        public string Name { get; }

        public NewtonsoftEasyCachingSerializer(string name = "newtonsoft_json", JsonSerializerSettings settings = null)
        {
            Name = name;
            _settings = settings ?? new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto, // supports polymorphic types
                NullValueHandling = NullValueHandling.Ignore
            };
        }

        // Generic serialization
        public byte[] Serialize<T>(T value)
        {
            if (value == null) return Array.Empty<byte>();
            string json = JsonConvert.SerializeObject(value, _settings);
            return Encoding.UTF8.GetBytes(json);
        }

        // Generic deserialization
        public T Deserialize<T>(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return default(T);
            string json = Encoding.UTF8.GetString(bytes);
            return JsonConvert.DeserializeObject<T>(json, _settings);
        }

        // Non-generic deserialization
        public object Deserialize(byte[] bytes, Type type)
        {
            if (bytes == null || bytes.Length == 0) return null;
            string json = Encoding.UTF8.GetString(bytes);
            return JsonConvert.DeserializeObject(json, type, _settings);
        }

        // ArraySegment serialization (for EasyCaching newer versions)
        public ArraySegment<byte> SerializeObject(object obj)
        {
            if (obj == null) return new ArraySegment<byte>(Array.Empty<byte>());
            string json = JsonConvert.SerializeObject(obj, _settings);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            return new ArraySegment<byte>(bytes);
        }

        // ArraySegment deserialization
        public object DeserializeObject(ArraySegment<byte> value)
        {
            if (value.Array == null || value.Count == 0) return null;
            string json = Encoding.UTF8.GetString(value.Array, value.Offset, value.Count);
            return JsonConvert.DeserializeObject(json, _settings);
        }
    }


    class CacheProvider : Octokit.Caching.IResponseCache
    {
        private readonly DefaultDiskCachingProvider _provider;

        public CacheProvider()
        {
            var serializer = new NewtonsoftEasyCachingSerializer();
            var serializers = new List<IEasyCachingSerializer> { serializer };

            var options = new DiskOptions
            {
                DBConfig = new DiskDbOptions
                {
                    BasePath = PathUtils.GetCachePath()
                },
                SerializerName = "newtonsoft_json"
            };

            _provider = new DefaultDiskCachingProvider("default_disk", serializers, options, null);
        }

        public async Task<CachedResponse.V1> GetAsync(IRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            string key = GenerateCacheKey(request);
            var cached = await _provider.GetAsync<CachedResponse.V1>(key);

            if (cached.HasValue)
            {
                return cached.Value;
            }

            return null;
        }

        public async Task SetAsync(IRequest request, CachedResponse.V1 cachedResponse)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (cachedResponse == null) throw new ArgumentNullException(nameof(cachedResponse));

            string key = GenerateCacheKey(request);
            // Octokit cache doesn't provide expiration info, but you can set one if you want
            await _provider.SetAsync(key, cachedResponse, TimeSpan.FromHours(1));
        }

        /// <summary>
        /// Generates a unique cache key for the request.
        /// You can include URL, method, and parameters to avoid collisions.
        /// </summary>
        private string GenerateCacheKey(IRequest request)
        {
            string endpointString = request.Endpoint?.ToString() ?? string.Empty;
            string method = request.Method?.ToString() ?? string.Empty;

            // Include query parameters if present
            string query = request.Parameters != null && request.Parameters.Count > 0
                ? string.Join("&", request.Parameters.Select(p => $"{p.Key}={p.Value}"))
                : string.Empty;

            return $"octokit_cache:{method}:{endpointString}?{query}";
        }
    }
}