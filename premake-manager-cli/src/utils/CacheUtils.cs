using EasyCaching.Core.Serialization;
using EasyCaching.Disk;
using EasyCaching.Serialization.Json;
using EasyCaching.Serialization.SystemTextJson; // Add this
using System;
using System.Collections.Generic;
using System.Text;

namespace src.utils
{
    internal class CacheUtils
    {
        private static readonly DefaultDiskCachingProvider _provider;
        static CacheUtils()
        {
           
            var serializer = new DefaultJsonSerializer("json", new SystemTextJsonSerializerOptions());
            var serializers = new List<IEasyCachingSerializer> { serializer };

            var options =  new DiskDbOptions { BasePath = "app_cache" };
            _provider = new DefaultDiskCachingProvider("default_disk", options);
        }
    }
}
