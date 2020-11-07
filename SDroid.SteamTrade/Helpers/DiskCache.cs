using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SDroid.SteamTrade.Helpers
{
    public class DiskCache
    {
        private static DiskCache _default = new DiskCache();
        private readonly DirectoryInfo _directory;
        private readonly object _localLock = new object();

        public DiskCache() : this(new DirectoryInfo(Path.GetTempPath()))
        {
        }

        public DiskCache(DirectoryInfo directory)
        {
            _directory = directory;
        }

        public static DiskCache Default
        {
            get => _default;
            set => _default = value ?? new DiskCache();
        }

        public bool DeleteCached(string name)
        {
            try
            {
                lock (_localLock)
                {
                    var fileName = GetFileName(name);

                    if (File.Exists(fileName))
                    {
                        File.Delete(fileName);
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <exception cref="Exception">A delegate callback throws an exception.</exception>
        // ReSharper disable once TooManyArguments
        public T Ensure<T>(string name, TimeSpan maxAge, Func<T> action, Func<T, bool> validityChecker = null)
        {
            if (IsValid(name, maxAge))
            {
                return GetCached<T>(name);
            }

            validityChecker = validityChecker ?? (r => !EqualityComparer<T>.Default.Equals(r, default));

            var result = action();

            if (!validityChecker(result))
            {
                PutCached(name, result);
            }

            return result;
        }

        /// <exception cref="Exception">A delegate callback throws an exception.</exception>
        // ReSharper disable once TooManyArguments
        public async Task<T> EnsureAsync<T>(
            string name,
            TimeSpan maxAge,
            Func<Task<T>> action,
            Func<T, Task<bool>> validityChecker = null)
        {
            validityChecker = validityChecker ??
                              (r => Task.FromResult(!EqualityComparer<T>.Default.Equals(r, default)));

            if (IsValid(name, maxAge))
            {
                return GetCached<T>(name);
            }

            var result = await action().ConfigureAwait(false);

            if (!await validityChecker(result).ConfigureAwait(false))
            {
                PutCached(name, result);
            }

            return result;
        }

        public bool Extend(string name)
        {
            try
            {
                lock (_localLock)
                {
                    var fileName = GetFileName(name);

                    if (!File.Exists(fileName))
                    {
                        return false;
                    }

                    File.SetCreationTimeUtc(fileName, DateTime.UtcNow);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public TimeSpan? GetAge(string name)
        {
            try
            {
                lock (_localLock)
                {
                    var fileName = GetFileName(name);

                    if (!File.Exists(fileName))
                    {
                        return null;
                    }

                    return DateTime.UtcNow - File.GetCreationTimeUtc(fileName);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public T GetCached<T>(string name)
        {
            try
            {
                lock (_localLock)
                {
                    var fileName = GetFileName(name);
                    var json = File.ReadAllText(fileName);

                    return JsonConvert.DeserializeObject<T>(json);
                }
            }
            catch (Exception)
            {
                return default;
            }
        }

        public bool IsValid(string name, TimeSpan maxAge = default)
        {
            try
            {
                var age = GetAge(name);

                if (age == null)
                {
                    return false;
                }

                if (maxAge == default)
                {
                    return true;
                }

                return age <= maxAge;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool PutCached<T>(string name, T obj)
        {
            try
            {
                lock (_localLock)
                {
                    var fileName = GetFileName(name);
                    var json = JsonConvert.SerializeObject(obj);
                    File.WriteAllText(fileName, json);
                    File.SetCreationTimeUtc(fileName, DateTime.UtcNow);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private string GetFileName(string name)
        {
            try
            {
                if (!_directory.Exists)
                {
                    _directory.Create();
                }
            }
            catch (Exception)
            {
                // ignored
            }

            return Path.Combine(_directory.FullName,
                string.Join("_", name.Split(Path.GetInvalidFileNameChars())) + ".cached");
        }
    }
}