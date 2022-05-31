using SharpConfig;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using WilsonUtils.Properties;

namespace WilsonUtils
{
    public class Settings
    {
        #region Properties

        public Configuration Config
        {
            get; internal set;
        } = new();

        public ApplicationSettingsBase ConfigDefaults
        {
            get; set;
        }

        public string ConfigFile
        {
            get; set;
        } = Defaults.Default.DefaultConfigFile;

        public string ProgramName
        {
            get; set;
        } = Assembly.GetExecutingAssembly().GetName().Name;

        #endregion Properties

        #region Constructors

        public Settings(string configFile, ApplicationSettingsBase defaults = null, string programName = null)
        {
            ConfigDefaults = defaults;
            ProgramName = programName ?? Defaults.Default.DefaultProgramName ?? Assembly.GetExecutingAssembly().GetName().Name;
            ConfigFile = configFile ?? (string)ConfigDefaults?["DefaultConfigFile"] ?? Defaults.Default.DefaultConfigFile;
            _ = Load();
        }

        #endregion Constructors

        #region Instance Methods

        public T Get<T>(string key, string section = null)
        {
            try
            {
                if (!Config.Contains(section ??= (string)ConfigDefaults?["DefaultSection"]) || !Config[section].Contains(key ??= ""))
                {
                    if (ConfigDefaults[$"{section}_{key}"] is not null)
                    {
                        Set(key, ConfigDefaults?[$"{section}_{key}"], section);
                    }
                    if (!Config.Contains(section) || !Config[section].Contains(key))
                    {
                        throw new KeyNotFoundException(string.Format(Resources.SettingsKeyNotFound, section, key));
                    }
                }
                return Config[section][key].GetValue<T>();
            }
            catch (Exception e)
            {
                DialogResult resetConfig = MsgBox.Show(
                    e.Message,
                    Resources.ResetConfigFile,
                    ProgramName,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Exclamation,
                    MessageBoxDefaultButton.Button2
                    );
                if (resetConfig == DialogResult.Yes)
                {
                    File.Delete(ConfigFile);
                    return Get<T>(key, section);
                }
                return default;
            }
        }

        public static Configuration Load(string configFile)
        {
            return File.Exists(configFile) ? Configuration.LoadFromFile(configFile) : default;
        }

        public bool Load()
        {
            if (!File.Exists(ConfigFile))
            {
                return false;
            }
            Config = Load(ConfigFile);
            return true;
        }

        public void Save()
        {
            Config.SaveToFile(ConfigFile);
        }

        public void Set<T>(string key, T value, string section = null, bool save = true)
        {
            try
            {
                if (key is null || key.Trim().Length == 0)
                {
                    throw new ArgumentException(Resources.InvalidSettingName);
                }
                Config[section ?? (string)ConfigDefaults?["DefaultSection"]][key].SetValue(value);
                if (save)
                {
                    Save();
                }
            }
            catch (Exception e)
            {
                _ = MsgBox.Show(
                    e.Message,
                    ProgramName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                    );
            }
        }

        public bool Toggle(string key, string section = null, bool save = true)
        {
            section ??= (string)ConfigDefaults?["DefaultSection"];
            Set(key, !Get<bool>(key, section), section, save);
            return Get<bool>(key, section);
        }

        #endregion Instance Methods
    }
}