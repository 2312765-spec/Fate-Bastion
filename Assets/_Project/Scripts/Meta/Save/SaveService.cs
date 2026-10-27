using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace FateBastion.Meta
{
    /// <summary>Where a Load() got its data from (S9).</summary>
    public enum SaveLoadKind
    {
        /// <summary>save.json was valid.</summary>
        Loaded,

        /// <summary>save.json was missing or unreadable and save.bak was used.</summary>
        RecoveredFromBackup,

        /// <summary>No usable file: a brand-new save was created from the starter factory.</summary>
        CreatedNew
    }

    /// <summary>Result of SaveService.Load(): the data, how it was obtained and the error that caused a fallback.</summary>
    public sealed class SaveLoadResult
    {
        public SaveData Data { get; }
        public SaveLoadKind Kind { get; }

        /// <summary>Message of the failure that forced a fallback; null when save.json loaded cleanly.</summary>
        public string Error { get; }

        public SaveLoadResult(SaveData data, SaveLoadKind kind, string error = null)
        {
            Data = data;
            Kind = kind;
            Error = error;
        }

        public bool Recovered => Kind != SaveLoadKind.Loaded;
    }

    /// <summary>
    /// Persistent storage of SaveData (S9). Plain class so EditMode tests can point it at a temp folder.
    /// Writes safely (save.tmp -> File.Replace/Move -> save.bak) and never overwrites save.json directly.
    /// Other services work on SaveData in memory and call Save() after every change (S8).
    /// </summary>
    public sealed class SaveService
    {
        public const string SaveFileName = "save.json";
        public const string BackupFileName = "save.bak";
        public const string TempFileName = "save.tmp";

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        private readonly IStarterSaveFactory _starterFactory;

        /// <param name="directory">Folder that holds save.json; Application.persistentDataPath in the game, a temp folder in tests.</param>
        /// <param name="starterFactory">Builds a new player's data when no usable file exists; null makes an empty SaveData.</param>
        public SaveService(string directory, IStarterSaveFactory starterFactory = null)
        {
            if (string.IsNullOrEmpty(directory)) throw new ArgumentException("directory is required", nameof(directory));
            Directory = directory;
            _starterFactory = starterFactory;
        }

        public string Directory { get; }
        public string SaveFilePath => Path.Combine(Directory, SaveFileName);
        public string BackupFilePath => Path.Combine(Directory, BackupFileName);
        public string TempFilePath => Path.Combine(Directory, TempFileName);

        public bool Exists => File.Exists(SaveFilePath);

        /// <summary>The data currently held in memory; set by Load() or Save().</summary>
        public SaveData Data { get; private set; }

        /// <summary>
        /// Reads save.json, falls back to save.bak, and finally creates a new save from the starter factory.
        /// Never throws for a missing or corrupt file: the caller inspects SaveLoadResult.Kind/Error.
        /// </summary>
        public SaveLoadResult Load()
        {
            System.IO.Directory.CreateDirectory(Directory);
            string error = null;

            if (File.Exists(SaveFilePath) && TryRead(SaveFilePath, out SaveData loaded, out error))
            {
                Data = loaded;
                return new SaveLoadResult(loaded, SaveLoadKind.Loaded);
            }

            if (File.Exists(BackupFilePath))
            {
                if (TryRead(BackupFilePath, out SaveData backup, out string backupError))
                {
                    Data = backup;
                    return new SaveLoadResult(backup, SaveLoadKind.RecoveredFromBackup, error);
                }
                if (string.IsNullOrEmpty(error)) error = backupError;
            }

            Data = CreateNew();
            return new SaveLoadResult(Data, SaveLoadKind.CreatedNew, error);
        }

        /// <summary>Writes the in-memory Data; same as Save(Data).</summary>
        public void Save()
        {
            if (Data == null) throw new InvalidOperationException("SaveService chưa có dữ liệu; gọi Load() trước.");
            Save(Data);
        }

        /// <summary>Writes data safely and adopts it as the in-memory data.</summary>
        public void Save(SaveData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            System.IO.Directory.CreateDirectory(Directory);
            string json = JsonConvert.SerializeObject(data, SerializerSettings);
            File.WriteAllText(TempFilePath, json, new UTF8Encoding(false));

            if (File.Exists(SaveFilePath)) File.Replace(TempFilePath, SaveFilePath, BackupFilePath);
            else File.Move(TempFilePath, SaveFilePath);

            Data = data;
        }

        /// <summary>Deletes save.json, save.bak and save.tmp. Used by the F1 debug panel (B-16).</summary>
        public void Delete()
        {
            DeleteIfExists(SaveFilePath);
            DeleteIfExists(BackupFilePath);
            DeleteIfExists(TempFilePath);
            Data = null;
        }

        private SaveData CreateNew()
        {
            SaveData data = _starterFactory != null ? _starterFactory.CreateNew() : new SaveData();
            if (data == null) data = new SaveData();
            data.version = SaveData.CurrentVersion;
            return data;
        }

        private static bool TryRead(string path, out SaveData data, out string error)
        {
            data = null;
            error = null;
            try
            {
                string text = File.ReadAllText(path, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(text))
                {
                    error = $"File save rỗng: {path}";
                    return false;
                }

                data = JsonConvert.DeserializeObject<SaveData>(text, SerializerSettings);
                if (data == null)
                {
                    error = $"File save không có dữ liệu: {path}";
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                data = null;
                error = ex.Message;
                return false;
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
