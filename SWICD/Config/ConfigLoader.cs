﻿using Newtonsoft.Json;
using SWICD.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Environment;

namespace SWICD.Config
{
    public class ConfigLoader
    {

        internal static bool TryMigrateConfiguration(Environment.SpecialFolder specialFolder, string subfolder, string file)
        {
            try
            {
                string folder = Environment.GetFolderPath(specialFolder);
                if (subfolder != null)
                {
                    folder = Path.Combine(folder, subfolder);
                }
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string fileOld = Path.Combine(folder, file);
                string fileNew = Path.Combine(folder, Path.GetFileNameWithoutExtension(file) + ".json");


                if (File.Exists(fileOld) && !File.Exists(fileNew))
                {
                    Configuration config = GetConfiguration(fileOld);
                    SaveConfiguration(config, fileNew);

                    File.Move(fileOld, fileOld + ".bak");
                    LoggingService.LogWarning($"Configuration has been migrated to new format. Check if everything works as expected. The old config has been moved to \"{fileOld}.bak\"");
                    return true;
                }
            }catch(Exception ex)
            {
                LoggingService.LogError($"Could not migrate config: {ex}");
            }

            return false;
        }
        public static Configuration GetConfiguration(Environment.SpecialFolder specialFolder, string subfolder, string file)
        {
            string folder = Environment.GetFolderPath(specialFolder);
            if (subfolder != null)
            {
                folder = Path.Combine(folder, subfolder);
            }
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            file = Path.Combine(folder, file);
            if (!File.Exists(file))
            {
                var config = new Configuration();
                SaveConfiguration(config, file);
                return config;
            }

            string json = File.ReadAllText(file);


            return JsonConvert.DeserializeObject<Configuration>(json);
        }

        public static Configuration GetConfiguration(string file)
        {
            Configuration configuration = new Configuration();

            string[] lines = File.ReadAllLines(file);

            string section = "general";

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (!line.StartsWith("#") && line.Length > 0) // Ignore comments
                {
                    if (line.StartsWith("["))
                    {
                        if (!line.Contains("]")) throw new Exception($"Malformed config. Expected \"]\" in line {i}");
                        section = line.Substring(1, line.IndexOf("]") - 1);
                        continue;
                    }

                    string[] parts = line.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length > 2)
                    {
                        parts = new string[]
                        {
                            line.Substring(0, line.IndexOf("=")),
                            line.Substring(line.IndexOf("=") +1)
                        };
                    }

                    if (parts.Length != 2)
                        throw new Exception($"Malformed config. Expected \"=\" in line {i}");

                    if (section == "general")
                    {
                        ProcessGeneralLine(parts[0].Trim(), parts[1].Trim(), ref configuration);
                    }

                    if (section == "buttons")
                    {
                        configuration.DefaultControllerConfig = ProcessButtonsLine(parts[0].Trim(), parts[1].Trim(), configuration.DefaultControllerConfig);
                        continue;
                    }

                    if (section == "keyboardkeys")
                    {
                        configuration.DefaultControllerConfig = ProcessKeyboardLine(parts[0].Trim(), parts[1].Trim(), configuration.DefaultControllerConfig);
                        continue;
                    }

                    if (section == "mousebuttons")
                    {
                        configuration.DefaultControllerConfig = ProcessMouseLine(parts[0].Trim(), parts[1].Trim(), configuration.DefaultControllerConfig);
                        continue;
                    }

                    if (section == "axes")
                    {
                        configuration.DefaultControllerConfig = ProcessAxesLine(parts[0].Trim(), parts[1].Trim(), configuration.DefaultControllerConfig);
                        continue;
                    }

                    if (section == "profile")
                    {
                        configuration.DefaultControllerConfig = ProcessProfileLine(parts[0].Trim(), parts[1].Trim(), configuration.DefaultControllerConfig);
                        continue;
                    }

                    string executable = section.Substring(section.IndexOf(",") + 1);

                    if (section.StartsWith("buttons"))
                    {

                        configuration.PerProcessControllerConfig[executable] = ProcessButtonsLine(parts[0].Trim(), parts[1].Trim(),
                                                                    configuration.PerProcessControllerConfig.ContainsKey(executable) ?
                                                                    configuration.PerProcessControllerConfig[executable] : GetControllerConfigFromDefault(configuration, executable));
                    }

                    if (section.StartsWith("keyboardkeys"))
                    {

                        configuration.PerProcessControllerConfig[executable] = ProcessKeyboardLine(parts[0].Trim(), parts[1].Trim(),
                                                                    configuration.PerProcessControllerConfig.ContainsKey(executable) ?
                                                                    configuration.PerProcessControllerConfig[executable] : GetControllerConfigFromDefault(configuration, executable));
                    }

                    if (section.StartsWith("mousebuttons"))
                    {

                        configuration.PerProcessControllerConfig[executable] = ProcessMouseLine(parts[0].Trim(), parts[1].Trim(),
                                                                    configuration.PerProcessControllerConfig.ContainsKey(executable) ?
                                                                    configuration.PerProcessControllerConfig[executable] : GetControllerConfigFromDefault(configuration, executable));
                    }

                    if (section.StartsWith("axes"))
                    {

                        configuration.PerProcessControllerConfig[executable] = ProcessAxesLine(parts[0].Trim(), parts[1].Trim(),
                                                                    configuration.PerProcessControllerConfig.ContainsKey(executable) ?
                                                                    configuration.PerProcessControllerConfig[executable] : GetControllerConfigFromDefault(configuration, executable));
                    }

                    if (section.StartsWith("profile"))
                    {

                        configuration.PerProcessControllerConfig[executable] = ProcessProfileLine(parts[0].Trim(), parts[1].Trim(),
                                                                    configuration.PerProcessControllerConfig.ContainsKey(executable) ?
                                                                    configuration.PerProcessControllerConfig[executable] : GetControllerConfigFromDefault(configuration, executable));
                    }
                }
            }
            configuration.CreateSnapshot();
            return configuration;
        }

        private static ControllerConfig GetControllerConfigFromDefault(Configuration configuration, string executable)
        {
            var config = (ControllerConfig)configuration.DefaultControllerConfig.Clone();
            config.Executable = executable;
            return config;
        }

        public static void SaveConfiguration(Configuration config, Environment.SpecialFolder specialFolder, string subfolder, string file)
        {
            string folder = Environment.GetFolderPath(specialFolder);
            if (subfolder != null)
            {
                folder = Path.Combine(folder, subfolder);
            }
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            file = Path.Combine(folder, file);
            SaveConfiguration(config, file);
        }

        public static void SaveConfiguration(Configuration config, string file)
        {
            File.WriteAllText(file, JsonConvert.SerializeObject(config, Formatting.Indented));
        }

        private static ControllerConfig ProcessAxesLine(string v1, string v2, ControllerConfig configuration)
        {
            HardwareAxis axis = (HardwareAxis)Enum.Parse(typeof(HardwareAxis), v1);
            configuration.AxisMapping[axis] = new EmulatedAxisConfig(v2);
            return configuration;
        }

        private static ControllerConfig ProcessProfileLine(string v1, string v2, ControllerConfig configuration)
        {
            if (v1 == "DisableLizardMode")
            {
                configuration.ProfileSettings.DisableLizardMouse = v2.ToLower() == "true";
                configuration.ProfileSettings.DisableLizardButtons = v2.ToLower() == "true";
            }
            if (v1 == "DisableLizardMouse")
            {
                configuration.ProfileSettings.DisableLizardMouse = v2.ToLower() == "true";
            }
            if (v1 == "DisableLizardButtons")
            {
                configuration.ProfileSettings.DisableLizardButtons = v2.ToLower() == "true";
            }
            return configuration;
        }

        private static ControllerConfig ProcessButtonsLine(string v1, string v2, ControllerConfig configuration)
        {
            HardwareButton hardwareButton = HardwareButton.None;
            EmulatedButton emulatedButton = EmulatedButton.None;

            Enum.TryParse(v1, out hardwareButton);
            Enum.TryParse(v2, out emulatedButton);

            configuration.ButtonMapping[hardwareButton] = emulatedButton;

            return configuration;
        }

        private static ControllerConfig ProcessKeyboardLine(string v1, string v2, ControllerConfig configuration)
        {
            HardwareButton hardwareButton = HardwareButton.None;

            Enum.TryParse(v1, out hardwareButton);

            configuration.KeyboardMapping[hardwareButton] = v2;

            return configuration;
        }

        private static ControllerConfig ProcessMouseLine(string v1, string v2, ControllerConfig configuration)
        {
            HardwareButton hardwareButton = HardwareButton.None;
            VirtualMouseKey mouseKey = VirtualMouseKey.NONE;

            Enum.TryParse(v1, out hardwareButton);
            Enum.TryParse(v2, out mouseKey);

            configuration.MouseMapping[hardwareButton] = mouseKey;

            return configuration;
        }

        private static void ProcessGeneralLine(string key, string value, ref Configuration config)
        {
            switch (key.ToLower())
            {
                case "blacklist":
                    config.GenericSettings.BlacklistedProcesses.Add(value);
                    break;
                case "whitelist":
                    config.GenericSettings.WhitelistedProcesses.Add(value);
                    break;
                case "mode":
                    OperationMode mode = OperationMode.Combined;
                    Enum.TryParse(value, out mode);
                    config.GenericSettings.OperationMode = mode;
                    break;
                case "startwithwindows":
                    bool _autostart = config.GenericSettings.StartWithWindows;
                    bool.TryParse(value, out _autostart);
                    config.GenericSettings.StartWithWindows = _autostart;
                    break;
                case "startminimized":
                    bool _minimized = config.GenericSettings.StartMinimized;
                    bool.TryParse(value, out _minimized);
                    config.GenericSettings.StartMinimized = _minimized;
                    break;
            }
        }
    }
}
