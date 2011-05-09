// Copyright 2011 Murray Grant
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;

namespace MurrayGrant.BabyGame.Services
{
    public class Configuration
    {
        public string PathToBabyPackage { get; set; }

        public int KeyBashingThreshold { get; set; }
        public bool DisplayOnAllScreens { get; set; }
        public bool MouseButtonRequiredWhenMoving { get; set; }
        public bool AutoPublishException { get; set; }
        public bool InDeveloperMode { get; set; }

        [XmlArray()] public List<Keys> DisabledKeys { get; set; }       // TODO: This needs to capture low level win32 keyboard event codes, not the Keys enum.


        internal Configuration()
        {
            this.DisabledKeys = new List<Keys>();

            this.DisplayOnAllScreens = false;
            this.KeyBashingThreshold = 0;
            this.MouseButtonRequiredWhenMoving = false;
            this.AutoPublishException = true;
            this.InDeveloperMode = false;
        }

        public Configuration Copy()
        {
            var result = new Configuration();
            result.PathToBabyPackage = this.PathToBabyPackage;
            result.KeyBashingThreshold = this.KeyBashingThreshold;
            result.DisplayOnAllScreens = this.DisplayOnAllScreens;
            result.MouseButtonRequiredWhenMoving = this.MouseButtonRequiredWhenMoving;
            result.AutoPublishException = this.AutoPublishException;
            result.InDeveloperMode = this.InDeveloperMode;
            result.DisabledKeys = this.DisabledKeys.ToList();
            return result;
        }
    }



    public class ConfigurationManager
        : IConfigurationService
    {
        public readonly FileInfo ConfigurationFile = new FileInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MurrayGrant", "BabyBashXNA", "config.xml"));
        public readonly FileInfo DefaultBabyPackageSource; 

        public bool Exists { get { return this.ConfigurationFile.Exists; } }
        public ConfigurationManager(Microsoft.Xna.Framework.Game game)
        {
            this.DefaultBabyPackageSource = new FileInfo(Path.Combine(game.Content.RootDirectory, "default.babypackage"));
        }
        public void Load()
        {
            FileStream stream = null;
            Configuration result = null;

            try
            {
                try
                {
                    stream = new FileStream(this.ConfigurationFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
                catch (FileNotFoundException)
                {
                    // Create a default configuration.
                    result = this.CreateDefault();
                }
                catch (DirectoryNotFoundException)
                {
                    // Create a default configuration.
                    result = this.CreateDefault();
                }

                // Deserialise using XPath (because it's faster than XML (de)serialisation).
                if (result == null && stream != null)
                {
                    var doc = new XPathDocument(stream);
                    var nav = doc.CreateNavigator();
                    var anInt = 0;
                    var aBool = false;

                    var root = nav.SelectSingleNode("/Configuration");
                    if (root != null)
                    {
                        result = this.CreateDefault();
                        var node = root.SelectSingleNode("PathToBabyPackage");
                        if (node != null)
                            result.PathToBabyPackage = node.Value;
                        if (!File.Exists(result.PathToBabyPackage) && Path.GetFileName(result.PathToBabyPackage).ToLower().Contains("default"))     // ClickOnce can move paths around somewhat unexpectedly, if that happens, try to recover the default package.
                            result.PathToBabyPackage = this.DefaultBabyPackageSource.FullName;

                        node = root.SelectSingleNode("KeyBashingThreshold");
                        if (node != null)
                        {
                            Int32.TryParse(node.Value, out anInt);
                            result.KeyBashingThreshold = anInt;
                        }

                        node = root.SelectSingleNode("DisplayOnAllScreens");
                        if (node != null)
                        {
                            Boolean.TryParse(node.Value, out aBool);
                            result.DisplayOnAllScreens = aBool;
                        }

                        node = root.SelectSingleNode("MouseButtonRequiredWhenMoving");
                        if (node != null)
                        {
                            Boolean.TryParse(node.Value, out aBool);
                            result.MouseButtonRequiredWhenMoving = aBool;
                        }

                        node = root.SelectSingleNode("AutoPublishException");
                        if (node != null)
                        {
                            Boolean.TryParse(node.Value, out aBool);
                            result.AutoPublishException = aBool;
                        }

                        node = root.SelectSingleNode("InDeveloperMode");
                        if (node != null)
                        {
                            Boolean.TryParse(node.Value, out aBool);
                            result.InDeveloperMode = aBool;
                        }

                        // TODO: DisabledKeys
                    }

                    // Any error in de-serialisation: reset and create a new configuration.
                    if (result == null)
                        result = this.CreateDefault();
                }
            }
            finally
            {
                if (stream != null)
                    ((IDisposable)stream).Dispose();
            }

            if (result == null)
                throw new ApplicationException(string.Format("Unable to load configuration file '{0}'.", this.ConfigurationFile.FullName));

            this.Current = result;

        }
        internal Configuration CreateDefault()
        {
            var result = new Configuration();
            result.PathToBabyPackage = this.DefaultBabyPackageSource.FullName;
            return result;
        }

        public Configuration Current { get; private set; }

        public void Save(Configuration config)
        {
            // Ensure the directory exists and the default baby package is copied.
            if (!this.ConfigurationFile.Directory.Exists)
                this.ConfigurationFile.Directory.Create();

            using (var stream = new FileStream(this.ConfigurationFile.FullName, FileMode.Create))
            {
                // TODO: XmlSerializer is slow (~500ms to generate the serialisation assembly). Consider changing to StringBuilder instead.
                var serialiser = new XmlSerializer(typeof(Configuration));
                serialiser.Serialize(stream, config);
            }

            this.Current = config;
        }
    }

    public interface IConfigurationService
    {
        bool Exists { get; }
        void Load();
        Configuration Current { get; }
        void Save(Configuration config);
    }
}
