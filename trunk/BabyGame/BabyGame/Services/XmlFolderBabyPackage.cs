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
using System.Xml.XPath;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Ionic.Zip;
using MurrayGrant.BabyGame.Helpers;
using MurrayGrant.BabyGame.Entities;

namespace MurrayGrant.BabyGame.Services
{
    public class XmlFolderBabyPackage 
        : IBabyPackageProvider
    {
        private readonly IEnumerable<EventType> KeyedEvents = new EventType[] { EventType.KeyPress, EventType.MouseButtonPress, EventType.MouseWheel, EventType.ControllerButtonPress };

        public GameMain Game { get; set; }

        public String Author { get; set; }
        public String Title { get; set; }
        public String Description { get; set; }
        public Uri Website { get; set; }

        private Dictionary<string, object> _LoadedObjects;       // ZipFile Path -> Texture/SoundEffect/Color (only used during loading).
        private Dictionary<string, List<Color>> _ColourPools = new Dictionary<string, List<Color>>();
        private Dictionary<string, List<Texture2D>> _TexturePools = new Dictionary<string, List<Texture2D>>();
        private Dictionary<string, List<SoundEffect>> _SoundPools = new Dictionary<string, List<SoundEffect>>();
        private Dictionary<string, BabyShape> _ShapeTemplates = new Dictionary<string, BabyShape>();

        // I suppose these could all be in one big dictionary.
        // But it's worth logically separating them out.
        private Dictionary<EventKey, EventAction> _KeyActionIndex = new Dictionary<EventKey, EventAction>();
        private Dictionary<EventKey, EventAction> _ControllerButtonActionIndex = new Dictionary<EventKey, EventAction>();
        private Dictionary<EventKey, EventAction> _MouseButtonActionIndex = new Dictionary<EventKey, EventAction>();
        private Dictionary<EventKey, EventAction> _MouseWheelActionIndex = new Dictionary<EventKey, EventAction>();

        private String _BackgroundPoolName;
        private int _BackgroundIndex = -1;
        private Color _BackgroundColour = Color.White;

        private EventAction _DefaultKeyAction;
        private EventAction _DefaultControllerButtonAction;
        private EventAction _DefaultMouseButtonAction;

        private EventAction _IdleAction;
        public TimeSpan IdleTimeout { get; private set; }
        private EventAction _AnalogueControllerAction;
        private EventAction _StartAction;
        private EventAction _KeyBashingAction;
        private EventAction _MouseMoveAction;
        
        private BabyShape _AnalogueControllerThing;

        private FileInfo _PackageFileForExceptionHandling;

        private CancellationToken _CancelMarker = CancellationToken.None;

        public XmlFolderBabyPackage(GameMain game)
        {
            this.Game = game;
        }

        public bool Loaded { get; private set; }
        public GameTime CurrentGameTime { get; set; }

        #region Dispose()
        private bool _Disposed = false;
        public void Dispose()
        {
            this.Dispose(true);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!this._Disposed)
            {
                if (disposing)
                {
                    foreach (var pool in this._TexturePools.Values)
                    {
                        foreach (var texture in pool)
                        {
                            texture.Dispose();
                        }
                    }
                    this._TexturePools.Clear();

                    foreach (var pool in this._SoundPools.Values)
                    {
                        foreach (var sound in pool)
                        {
                            sound.Dispose();
                        }
                    }
                    this._SoundPools.Clear();
                }
                this._ShapeTemplates.Clear();
                this._ColourPools.Clear();

                this._KeyActionIndex.Clear();
                this._MouseButtonActionIndex.Clear();
                this._MouseWheelActionIndex.Clear();
                this._ControllerButtonActionIndex.Clear();

                this._DefaultKeyAction = null;
                this._DefaultControllerButtonAction = null;
                this._DefaultMouseButtonAction = null;

                this._IdleAction = null;
                this._AnalogueControllerAction = null;
                this._StartAction = null;
                this._KeyBashingAction = null;
                this._MouseMoveAction = null;

                this._Disposed = true;
            }
        }
        #endregion

        #region Load
        public void Load(string packagePathAndFile, CancellationToken cancelMarker)
        {
            this.Load(new FileInfo(packagePathAndFile), cancelMarker);
        }
        public void Load(System.IO.FileInfo packageFile, CancellationToken cancelMarker)
        {
            // These are effectivly closure variables.
            this._LoadedObjects = new Dictionary<string, object>();
            this._PackageFileForExceptionHandling = packageFile;
            this._CancelMarker = cancelMarker;

            try
            {
                using (var stream = packageFile.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                using (var packageData = this.OpenBabyPackageAndVerifyRootNodeAndVersion(packageFile, stream))
                {
                    if (this._CancelMarker.IsCancellationRequested)
                        this._CancelMarker.ThrowIfCancellationRequested();

                    // Each version gets a go at loading stuff.
                    if (packageData.Version >= new Version(1, 0))
                        this.LoadVersion10(packageData.ZipFile, packageData.XmlDoc);
                    // TODO: think about what is involved in handling new data for future versions.
                }
            }
            catch (System.Security.SecurityException ex)
            {
                throw this.CreateLoadException("Unable to access BabyPackage", ex);
            }
            catch (FileNotFoundException ex)
            {
                throw this.CreateLoadException("Unable to find BabyPackage", ex);
            }
            catch (DirectoryNotFoundException ex)
            {
                throw this.CreateLoadException("Unable to find BabyPackage (may be on a network drive which is unavailable)", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw this.CreateLoadException("Unable to access BabyPackage", ex);
            }
            catch (IOException ex)
            {
                throw this.CreateLoadException("Unable to access BabyPackage, it may already be open in another program", ex);
            }
            catch (OperationCanceledException)
            {
                // Load was cancelled: rethrow.
                throw;
            }
            catch (Exception ex)
            {
                throw this.CreateLoadException("Unexpected error while loading BabyPackage", ex);
            }
            finally
            {
                // These aren't needed after load.
                if (this._LoadedObjects != null)
                    this._LoadedObjects = null;
                if (this._PackageFileForExceptionHandling != null)
                    this._PackageFileForExceptionHandling = null;
                this._CancelMarker = CancellationToken.None;
            }

            this.Loaded = true;
        }

        private FirstOpenData OpenBabyPackageAndVerifyRootNodeAndVersion(System.IO.FileInfo packageFile, System.IO.Stream packageStream)
        {
            // Open the zip file and check the header.
            var zipFile = Ionic.Zip.ZipFile.Read(packageStream);
            if (!zipFile.ContainsEntry("BabyPackage.xml"))
                throw this.CreateLoadException("Could not find 'BabyPackage.xml' in BabyPackage");

            using (var xmlStream = zipFile["BabyPackage.xml"].OpenReader())
            {
                // Use XPath to load the document elements.
                var packageDoc = new XPathDocument(xmlStream);
                var nav = packageDoc.CreateNavigator();

                var root = nav.SelectSingleNode("/BabyPackage");
                if (root == null)
                    throw this.CreateLoadException(root, "Could not find root node 'BabyPackage'");
                var verStr = root.GetAttribute("Version", String.Empty);
                if (String.IsNullOrWhiteSpace(verStr))
                    throw this.CreateLoadException(root, "Could not find 'Version' attribute on root node 'BabyPackage'");
                var ver = Version.Parse(verStr);
                if (ver < new Version(1, 0))
                    throw this.CreateLoadException(root, String.Format("Found unexpected Version '{0}'", ver));

                // This returns the XPathDocument and associated stream as a performance optimisation.
                // Otherwise closing, reopening and recreating the XPathDoc takes ~80-120ms (and is otherwise a free performance gain).
                return new FirstOpenData { ZipFile = zipFile, Version = ver, XmlDoc = packageDoc, XmlStream = xmlStream };
            }
        }
        private sealed class FirstOpenData
            : IDisposable
        {
            public ZipFile ZipFile { get; set; }
            public Version Version { get; set; }
            public XPathDocument XmlDoc { get; set; }
            public Stream XmlStream { get; set; }

            public void Dispose()
            {
                if (this.XmlStream != null)
                {
                    this.XmlStream.Dispose();
                    this.XmlStream = null;
                }
            }
        }
        private void LoadVersion10(ZipFile zipFile, XPathDocument packageDoc)
        {
            // Use XPath to load the document elements.
            var nav = packageDoc.CreateNavigator();
            var root = nav.SelectSingleNode("/BabyPackage");

            // Load the basic properties of the package.
            this.Author = root.GetAttribute("Author", String.Empty);
            this.Title = root.GetAttribute("Title", String.Empty);
            this.Description = root.GetAttribute("Description", String.Empty);
            var website = root.GetAttribute("Website", String.Empty);
            if (!String.IsNullOrWhiteSpace(website))
            {
                try {
                    this.Website = new Uri(website);
                } catch (UriFormatException ex) {
                    throw this.CreateLoadException(root, "Invalid website", ex);
                }
            }
            if (this._CancelMarker.IsCancellationRequested)
                this._CancelMarker.ThrowIfCancellationRequested();

            // Load pools.
            this.LoadPool<Color>(root, zipFile, new string[] { "ColourPools/ColourPool", "ColorPools/ColourPool", "ColourPools/ColorPool", "ColorPools/ColorPool" }, new string[] { "Color", "Colour" }, this._ColourPools, ColourFromNode);
            this.LoadPool<Texture2D>(root, zipFile, new string[] { "TexturePools/TexturePool" }, new string[] { "Texture" }, this._TexturePools, TextureFromNode);
            this.LoadPool<SoundEffect>(root, zipFile, new string[] { "SoundPools/SoundPool" }, new string[] { "Sound" }, this._SoundPools, SoundFromNode);
            this.LoadBackground(root, zipFile);
            this.LoadShapeTemplates(root);


            // Load events!!
            var evtRoot = root.SelectSingleNode("Events");
            // Misc.
            this._StartAction = this.LoadEventAction(evtRoot.SelectSingleNode("Start"), zipFile, EventType.Start, false).Value;
            this._KeyBashingAction = this.LoadEventAction(evtRoot.SelectSingleNode("KeyBashing"), zipFile, EventType.KeyBashing, false).Value;
            this._IdleAction = this.LoadEventAction(evtRoot.SelectSingleNode("Idle"), zipFile, EventType.Idle, false).Value;
            this.IdleTimeout = this.LoadIdleTimeout(evtRoot.SelectSingleNode("Idle"));


            // Keyboard.
            this.LoadEventActions(evtRoot.Select("KeyPress/Action").Cast<XPathNavigator>(), zipFile, EventType.KeyPress, this._KeyActionIndex);
            foreach (var node in evtRoot.Select("KeyPress/LoadFont").Cast<XPathNavigator>())
                this.LoadFontFromZipFolder(node, zipFile, this._KeyActionIndex);
            this._DefaultKeyAction = this.LoadEventAction(evtRoot.SelectSingleNode("KeyPress/DefaultAction"), zipFile, EventType.KeyPress, true).Value;

            // Mouse.
            this._MouseMoveAction = this.LoadEventAction(evtRoot.SelectSingleNode("MouseMove"), zipFile, EventType.MouseMove, false).Value;
            this.LoadEventActions(evtRoot.Select("MouseButtonPress/Action").Cast<XPathNavigator>(), zipFile, EventType.MouseButtonPress, this._MouseButtonActionIndex);
            this.LoadEventActions(evtRoot.Select("MouseWheel/Action").Cast<XPathNavigator>(), zipFile, EventType.MouseWheel, this._MouseWheelActionIndex);
            this._DefaultMouseButtonAction = this.LoadEventAction(evtRoot.SelectSingleNode("MouseButtonPress/DefaultAction"), zipFile, EventType.MouseButtonPress, true).Value;

            // Controller.
            this.LoadEventActions(evtRoot.Select("ControllerButtonPress/Action").Cast<XPathNavigator>(), zipFile, EventType.ControllerButtonPress, this._ControllerButtonActionIndex);
            this._DefaultControllerButtonAction = this.LoadEventAction(evtRoot.SelectSingleNode("ControllerButtonPress/DefaultAction"), zipFile, EventType.ControllerButtonPress, true).Value;
            this._AnalogueControllerAction = this.LoadEventAction(evtRoot.SelectSingleNode("ControllerAnalogueMove"), zipFile, EventType.ControllerAnalogueMove, false).Value;
        }

        private void LoadPool<T>(XPathNavigator root, ZipFile zipFile, IEnumerable<String> xQueries, IEnumerable<String> subNodes, Dictionary<string, List<T>> pool, Func<XPathNavigator, ZipFile, T> nodeFactory)
        {
            foreach (var p in xQueries.SelectMany(q => root.Select(q).Cast<XPathNavigator>()))
            {
                if (this._CancelMarker.IsCancellationRequested)
                    this._CancelMarker.ThrowIfCancellationRequested();

                var name = p.GetAttribute("Name", String.Empty).Trim();
                if (String.IsNullOrEmpty(name))
                    throw this.CreateLoadException(p, "Unable to find 'Name' attribute (required for any pool).");
                if (!pool.ContainsKey(name))
                    pool[name] = new List<T>();

                foreach (var c in subNodes.SelectMany(node => p.SelectChildren(node, String.Empty).Cast<XPathNavigator>()))
                    pool[name].Add(nodeFactory(c, zipFile));
            }
        }
        private Color ColourFromNode(XPathNavigator node, ZipFile zipFile)
        {
            var colourName = node.GetAttribute("Name", String.Empty).Trim();
            var colourHex = node.GetAttribute("Value", String.Empty).Trim();
            if (!String.IsNullOrWhiteSpace(colourName))
                return System.Drawing.Color.FromName(colourName).ToXnaColour();
            else if (!String.IsNullOrWhiteSpace(colourHex))
                return ColorHelper.FromArgbHexString(colourHex);  
            else
                throw this.CreateLoadException(node, String.Format("Unable to parse colour '{0}'.", !String.IsNullOrEmpty(colourName) ? colourName : colourHex));            
        }
        private String GetPathFromNode(XPathNavigator node, ZipFile zipFile)
        {
            var path = node.GetAttribute("Path", String.Empty).Trim().ToLower().Replace(@"\", "/");
            if (String.IsNullOrWhiteSpace(path))
                throw this.CreateLoadException(node, "Unable to find required Path attribute");
            if (!zipFile.ContainsEntry(path))
                throw this.CreateLoadException(node, String.Format("Unable to find Path '{0}' in zip file", path));
            return path;
        }
        private Texture2D TextureFromPath(String path, ZipFile zipFile)
        {
            // Load the texture up via a memory stream (because Texture2D needs a seekable stream).
            return (Texture2D)this._LoadedObjects.CachedOrNew(path, () =>
            {
                using (var zipStream = zipFile[path].OpenReader())
                using (var textureStream = zipStream.ToMemoryStream())
                    return Texture2D.FromStream(this.Game.GraphicsDevice, textureStream);
            });
        }
        private Texture2D TextureFromNode(XPathNavigator node, ZipFile zipFile)
        {
            var path = this.GetPathFromNode(node, zipFile);
            return this.TextureFromPath(path, zipFile);
        }
        private SoundEffect SoundFromPath(String path, ZipFile zipFile, XPathNavigator node)
        {
            // Load the texture up via a memory stream (because SoundEffect needs a seekable stream).
            return (SoundEffect)this._LoadedObjects.CachedOrNew(path, () =>
            {
                using (var rawStream = zipFile[path].OpenReader())
                using (var decoderStream = this.GetAsWavStream(rawStream, path, node))
                using (var wavStream = decoderStream.ToMemoryStream(this.GetHeaderSpace(path)))
                {
                    // If it's an MP3, add a RIFF WAV header to the decoded stream.
                    this.AddRiffHeader(wavStream, path);
                    return SoundEffect.FromStream(wavStream);
                }
            });
        }
        private SoundEffect SoundFromNode(XPathNavigator node, ZipFile zipFile)
        {
            var path = this.GetPathFromNode(node, zipFile);
            return this.SoundFromPath(path, zipFile, node);
        }
        private Stream GetAsWavStream(Stream s, string path, XPathNavigator node)
        {
            if (Path.GetExtension(path).ToLower() == ".wav")
                return s;
            else if (Path.GetExtension(path).ToLower() == ".mp3")
                // TODO: this decoder is by far the slowest part of loading.
                // Profile and optimise it!
                return new Mp3Sharp.Mp3Stream(s, 16384, Mp3Sharp.SoundFormat.Pcm16BitMono);     
            else
                throw this.CreateLoadException(node, String.Format("Unsupported file type '{0}'", Path.GetExtension(path)));

            throw new ApplicationException();
        }
        private int GetHeaderSpace(string path)
        {
            if (Path.GetExtension(path).ToLower() == ".wav")
                return 0;
            else if (Path.GetExtension(path).ToLower() == ".mp3")
                return 0x2c;
            else
                throw this.CreateLoadException(String.Format("Unsupported file type '{0}'", path));

            throw new ApplicationException();
        }
        private void AddRiffHeader(MemoryStream ms, string path)
        {
            if (Path.GetExtension(path).ToLower() != ".mp3")
                return;

            // http://www-mmsp.ece.mcgill.ca/Documents/AudioFormats/WAVE/WAVE.html
            ms.Seek(0, SeekOrigin.Begin);
            ms.Write("RIFF".ToAsciiBytes(), 0, 4);      // RIFF magic value.
            ms.Write(BitConverter.GetBytes((UInt32)ms.Length - 8).ToLittleEndian(), 0, 4);         // Chunk size = file size - 8 bytes for RIFF header.
            ms.Write("WAVE".ToAsciiBytes(), 0, 4);      // WAVE magic value.

            // Chunk.
            ms.Write("fmt ".ToAsciiBytes(), 0, 4);      // Chunk magic value.
            ms.Write(BitConverter.GetBytes((UInt32)16).ToLittleEndian(), 0 , 4);     // Chunk size = 16 bytes.
            ms.Write(BitConverter.GetBytes((UInt16)1).ToLittleEndian(), 0, 2);      // Format = PCM = 1.
            ms.Write(BitConverter.GetBytes((UInt16)1).ToLittleEndian(), 0, 2);      // Channels = 1 = Mono.
            ms.Write(BitConverter.GetBytes((UInt32)44100).ToLittleEndian(), 0, 4);      // Samples / Sec = 44khz.
            ms.Write(BitConverter.GetBytes((UInt32)44100 * 2).ToLittleEndian(), 0, 4);      // Data rate = 44k * 2 bytes / sec.
            ms.Write(BitConverter.GetBytes((UInt16)2).ToLittleEndian(), 0, 2);      // Data block size = 2 bytes.
            ms.Write(BitConverter.GetBytes((UInt16)16).ToLittleEndian(), 0, 2);      // Bits / sample = 16.

            // Data.
            ms.Write("data".ToAsciiBytes(), 0, 4);      // Data magic value.
            ms.Write(BitConverter.GetBytes((UInt32)(ms.Length - 44)).ToLittleEndian(), 0, 4);      // Number of chunks = file size - 44 bytes of headers.
            
            ms.Seek(0, SeekOrigin.Begin);
        }
        private void LoadShapeTemplates(XPathNavigator root)
        {
            foreach (var t in root.Select("ShapeTemplates/ShapeTemplate").Cast<XPathNavigator>())
            {
                if (this._CancelMarker.IsCancellationRequested)
                    this._CancelMarker.ThrowIfCancellationRequested();

                var name = t.GetAttribute("Name", String.Empty).Trim();
                if (String.IsNullOrEmpty(name))
                    throw this.CreateLoadException(t, "Unable to find required Name attribute for ShapeTemplate");

                var shape = new BabyShape(this.Game);
                shape.Size = Vector2Helper.GetProportionalSize(Single.Parse(t.GetAttribute("Size", String.Empty).Trim()), this.Game.GraphicsDevice.Viewport);
                shape.SpinTime = TimeSpan.FromSeconds(Double.Parse(t.GetAttribute("SpinTime", String.Empty).Trim()));
                shape.FadeInTime = TimeSpan.FromSeconds(Double.Parse(t.GetAttribute("FadeInTime", String.Empty).Trim()));
                shape.SolidTime = TimeSpan.FromSeconds(Double.Parse(t.GetAttribute("SolidTime", String.Empty).Trim()));
                shape.FadeOutTime = TimeSpan.FromSeconds(Double.Parse(t.GetAttribute("FadeOutTime", String.Empty).Trim()));

                this._ShapeTemplates.Add(name, shape);
            }
        }

        private void LoadBackground(XPathNavigator root, ZipFile zipFile)
        {
            var b = root.SelectSingleNode("Background");

            // Colour.
            var pool = b.GetAttribute("ColourPool", String.Empty).Trim();
            if (String.IsNullOrEmpty(pool))
                pool = b.GetAttribute("ColorPool", String.Empty).Trim();        // Try american spelling.
            if (!String.IsNullOrEmpty(pool) && this._ColourPools.ContainsKey(pool))
            {
                this._BackgroundColour = this._ColourPools[pool][this.Game.RandomGenerator.Next(0, this._ColourPools[pool].Count)];
            }
            else
            {
                var colour = b.GetAttribute("Colour", String.Empty).Trim();
                if (String.IsNullOrEmpty(colour))
                    colour = b.GetAttribute("Color", String.Empty).Trim();          // Try american spelling.

                if (!String.IsNullOrEmpty(colour))
                    this._BackgroundColour = ColorHelper.Parse(colour);
            }
            if (this._CancelMarker.IsCancellationRequested)
                this._CancelMarker.ThrowIfCancellationRequested();

            // Texture.
            var tPath = b.GetAttribute("TexturePath", String.Empty).Trim().ToLower().Replace(@"\", "/");
            var tPool = b.GetAttribute("TexturePool", String.Empty).Trim();
            if (!String.IsNullOrEmpty(tPool) && this._TexturePools.ContainsKey(tPool))
            {
                // Pool.
                this._BackgroundPoolName = tPool;
                this._BackgroundIndex = this.Game.RandomGenerator.Next(0, this._TexturePools[tPool].Count);
            }
            else
            {
                // Path.
                this._TexturePools.Add("__Background_", this.CreateAndInsertList(this.TextureFromPath(tPath, zipFile)));
                this._BackgroundPoolName = "__Background_";
                this._BackgroundIndex = 0;
            }
            if (this._CancelMarker.IsCancellationRequested)
                this._CancelMarker.ThrowIfCancellationRequested();
            
        }

        private void LoadEventActions(IEnumerable<XPathNavigator> nodes, ZipFile zipFile, EventType e, Dictionary<EventKey, EventAction> eventIndex)
        {
            foreach (var node in nodes)
            {
                if (this._CancelMarker.IsCancellationRequested)
                    this._CancelMarker.ThrowIfCancellationRequested();

                var keyedAction = this.LoadEventAction(node, zipFile, e, false);
                if (eventIndex.ContainsKey(keyedAction.Key))
                    throw new BabyPackageLoadException();
                else
                    eventIndex.Add(keyedAction.Key, keyedAction.Value);
            }
        }
        private KeyValuePair<EventKey, EventAction> LoadEventAction(XPathNavigator node, ZipFile zipFile, EventType e, bool isDefaultAction)
        {
            if (node == null)
                return new KeyValuePair<EventKey, EventAction>(EventKey.Empty, new EventAction());

            // Load up some attributes!
            var key = node.GetAttribute("Key", String.Empty).Trim();
            var shapeTemplate = node.GetAttribute("ShapeTemplate", String.Empty).Trim();
            var texPool = node.GetAttribute("TexturePool", String.Empty).Trim();
            var texPath = node.GetAttribute("TexturePath", String.Empty).Trim().Replace(@"\", "/");
            var sndPool = node.GetAttribute("SoundPool", String.Empty).Trim();
            var sndPath = node.GetAttribute("SoundPath", String.Empty).Trim().Replace(@"\", "/");
            var clrPool = node.GetAttribute("ColourPool", String.Empty).Trim();
            if (String.IsNullOrEmpty(clrPool))
                clrPool = node.GetAttribute("ColorPool", String.Empty).Trim();      // Try American spelling.
            var clrName = node.GetAttribute("Colour", String.Empty).Trim();
            if (String.IsNullOrEmpty(clrName))
                clrName = node.GetAttribute("Color", String.Empty).Trim();     // Try American spelling.

            if (this._CancelMarker.IsCancellationRequested)
                this._CancelMarker.ThrowIfCancellationRequested();

            EventKey resultKey = EventKey.Empty;
            if (this.KeyedEvents.Contains(e) && !isDefaultAction)
            {
                // Load and parse the key.
                if (e == EventType.KeyPress)
                    resultKey = new EventKey(e, (Keys)Enum.Parse(typeof(Keys), key));
                else if (e == EventType.MouseButtonPress)
                    resultKey = new EventKey(e, (MouseButton)Enum.Parse(typeof(MouseButton), key));
                else if (e == EventType.MouseWheel)
                    resultKey = new EventKey(e, (MouseWheelDirection)Enum.Parse(typeof(MouseWheelDirection), key));
                else if (e == EventType.ControllerButtonPress)
                    resultKey = new EventKey(e, (ControllerButton)Enum.Parse(typeof(ControllerButton), key));
                else
                    throw new ApplicationException(String.Format("Unexpected value of EventType: '{0}'", e));
            }

            var result = new EventAction();

            // Shape template.
            if (!String.IsNullOrEmpty(shapeTemplate))
            {
                if (!this._ShapeTemplates.ContainsKey(shapeTemplate))
                    throw this.CreateLoadException(node, String.Format("Unable to find ShapeTemplate with Name '{0}'", shapeTemplate));
                result.Template = this._ShapeTemplates[shapeTemplate];
            }

            if (this._CancelMarker.IsCancellationRequested)
                this._CancelMarker.ThrowIfCancellationRequested();

            // Colour / ColourPool.
            if (!String.IsNullOrEmpty(clrName) && !String.IsNullOrEmpty(clrPool))
                throw this.CreateLoadException(node, "Action cannot have Colour and ColourPool");
            if (!String.IsNullOrEmpty(clrName))
                // Named / hex value colour.
                result.ColourPool.Add(ColorHelper.Parse(clrName));
            if (!String.IsNullOrEmpty(clrPool) && !this._ColourPools.ContainsKey(clrPool))
                throw this.CreateLoadException(node, String.Format("Unable to find ColourPool with Name '{0}'", shapeTemplate));
            if (!String.IsNullOrEmpty(clrPool) && this._ColourPools.ContainsKey(clrPool))
                // Colour pool.
                result.ColourPool = this._ColourPools[clrPool];

            if (this._CancelMarker.IsCancellationRequested)
                this._CancelMarker.ThrowIfCancellationRequested();


            // Texture / TexturePool.
            if (!String.IsNullOrEmpty(texPath) && !String.IsNullOrEmpty(texPool))
                throw this.CreateLoadException(node, "Action cannot have TexturePath and TexturePool");
            if (!String.IsNullOrEmpty(texPath))
            {
                // Path to standalone texture.
                var tex = this.TextureFromPath(texPath, zipFile);
                this._TexturePools.Add("__" + e.ToString() + "_" + texPath + "_", this.CreateAndInsertList(tex));           // Add to the main texture pool dictionary so it is cleaned up nicely at shutdown.
                result.TexturePool.Add(tex);
            }
            if (!String.IsNullOrEmpty(texPool) && !this._TexturePools.ContainsKey(texPool))
                throw this.CreateLoadException(node, String.Format("Unable to find TexturePool with Name '{0}'", texPool));
            if (!String.IsNullOrEmpty(texPool) && this._TexturePools.ContainsKey(texPool))
                // Texture pool.
                result.TexturePool = this._TexturePools[texPool];

            if (this._CancelMarker.IsCancellationRequested)
                this._CancelMarker.ThrowIfCancellationRequested();


            // Sound / SoundPool.
            if (!String.IsNullOrEmpty(sndPath) && !String.IsNullOrEmpty(sndPool))
                throw this.CreateLoadException(node, "Action cannot have SoundPath and SoundPool");
            if (!String.IsNullOrEmpty(sndPath))
            {
                // Path to standalone texture.
                var snd = this.SoundFromPath(sndPath, zipFile, node);
                this._SoundPools.Add("__" + e.ToString() + "_" + sndPath + "_", this.CreateAndInsertList(snd));           // Add to the main sound pool dictionary so it is cleaned up nicely at shutdown.
                result.SoundPool.Add(snd);
            }
            if (!String.IsNullOrEmpty(sndPool) && !this._SoundPools.ContainsKey(sndPool))
                throw this.CreateLoadException(node, String.Format("Unable to find SoundPool with Name '{0}'", sndPool));
            if (!String.IsNullOrEmpty(sndPool) && this._SoundPools.ContainsKey(sndPool))
                // Sound pool.
                result.SoundPool = this._SoundPools[sndPool];


            return new KeyValuePair<EventKey, EventAction>(resultKey, result);
        }

        private void LoadFontFromZipFolder(XPathNavigator node, ZipFile zipFile, Dictionary<EventKey, EventAction> eventIndex)
        {
            // This gets us lots of values.
            var template = this.LoadEventAction(node, zipFile, EventType.KeyPress, true);
            
            // Load up the rest of the attributes!
            var texPath = node.GetAttribute("Path", String.Empty).Trim().Replace(@"\", "/").ToLower();
            var filePrefix = node.GetAttribute("FilePrefix", String.Empty).Trim().Replace(@"\", "/");
            var ext = "." + node.GetAttribute("FileType", String.Empty).Trim();

            var keyEnums = Enum.GetNames(typeof(Keys)).Select(s=>s.ToLower());
            foreach (var f in zipFile.Where(f => f.FileName.Replace(@"\", "/").ToLower().StartsWith(texPath)
                                            && String.Equals(Path.GetExtension(f.FileName), ext, StringComparison.CurrentCultureIgnoreCase)))
            {
                if (this._CancelMarker.IsCancellationRequested)
                    this._CancelMarker.ThrowIfCancellationRequested();

                var candidateEnum = Path.GetFileNameWithoutExtension(f.FileName).ToLower();
                if (!String.IsNullOrEmpty(texPath))
                    candidateEnum = candidateEnum.Replace(texPath, String.Empty);
                if (!String.IsNullOrEmpty(filePrefix))
                    candidateEnum = candidateEnum.Replace(filePrefix, String.Empty);
                if (keyEnums.Contains(candidateEnum))
                {
                    var key = new EventKey(EventType.KeyPress, (Keys)Enum.Parse(typeof(Keys), candidateEnum, true));
                    if (this._KeyActionIndex.ContainsKey(key))
                        throw this.CreateLoadException(node, String.Format("Key '{0}' from Font has already been loaded", key.ValueAsKeyboard));
                    var result = new EventAction();
                    result.ColourPool = template.Value.ColourPool;
                    result.SoundPool = template.Value.SoundPool;
                    result.Template = template.Value.Template;

                    var tex = this.TextureFromPath(f.FileName, zipFile);
                    this._TexturePools.Add("__Key_" + key.ValueAsKeyboard.ToString() + "_" + texPath + "_", this.CreateAndInsertList(tex));           // Add to the main texture pool dictionary so it is cleaned up nicely at shutdown.
                    result.TexturePool.Add(tex);

                    eventIndex.Add(key, result);
                }
            }

        }
        private TimeSpan LoadIdleTimeout(XPathNavigator node)
        {
            var timeStr = node.GetAttribute("Timeout", String.Empty).Trim();
            TimeSpan result;
            if (!TimeSpan.TryParseExact(timeStr, @"hh\:mm\:ss", System.Globalization.CultureInfo.InvariantCulture, out result))
                throw this.CreateLoadException(node, "Unable to parse Timeout attribute: expecting the format hh:mm:ss");
            return result;
        }
        #endregion

        #region Events
        #region Background
        public virtual Texture2D GetBackground()
        {
            if (this._BackgroundIndex == -1)
                return null;
            else
                return this._TexturePools[this._BackgroundPoolName][this._BackgroundIndex];
        }
        public virtual Color GetBackgroundColour()
        {
            return this._BackgroundColour;
        }
        #endregion

        #region Start
        public virtual ShapeAndSoundTuple Startup()
        {
            return this.DoEvent(this._StartAction);
        }
        #endregion

        #region Keyboard
        public virtual ShapeAndSoundTuple MapKeyPress(Keys keyPress)
        {
            var key = new EventKey(EventType.KeyPress, keyPress);
            if (this._KeyActionIndex.ContainsKey(key))
                return this.DoEvent(this._KeyActionIndex[key]);
            else
                return this.DoEvent(this._DefaultKeyAction);
        }
        #endregion

        #region Mouse
        public virtual ShapeAndSoundTuple MapMouseButton(MouseButton buttonPress, int currentX, int currentY)
        {
            var config = this.Game.Services.GetService<IConfigurationService>().Current;
            
            // Only return mouse button shapes if you don't have to hold the mouse button down to draw.
            if (config.MouseButtonRequiredWhenMoving)
                return null;

            var key = new EventKey(EventType.MouseButtonPress, buttonPress);
            if (this._MouseButtonActionIndex.ContainsKey(key))
                return this.DoEvent(this._MouseButtonActionIndex[key], new Vector2(currentX, currentY));
            else
                return this.DoEvent(this._DefaultMouseButtonAction, new Vector2(currentX, currentY));
        }

        public virtual ShapeAndSoundTuple DoMouseMovement(int currentX, int currentY, int lastX, int lastY, bool anyButtonPressed)
        {
            var config = this.Game.Services.GetService<IConfigurationService>().Current;
            if (
                // Check config to see if a button must be held while moving the mouse.
                ((config.MouseButtonRequiredWhenMoving && anyButtonPressed)
                || !config.MouseButtonRequiredWhenMoving)

                // Mouse state must be different from last frame and on screen.
                && lastX != currentX && lastY != currentY
                && currentX >= this.Game.GraphicsDevice.Viewport.X && currentY >= this.Game.GraphicsDevice.Viewport.Y
                && currentX <= this.Game.GraphicsDevice.Viewport.Width && currentY <= this.Game.GraphicsDevice.Viewport.Height
               )
            {
                // TODO: possibly check the distance between current and last positions and interpolate extra circles if it's too much.
                return this.DoEvent(this._MouseMoveAction, new Vector2(currentX, currentY));
            }
            else
            {
                return null;
            }
        }
        public virtual ShapeAndSoundTuple DoMouseWheelMovement(int wheelState, int lastWheelState, bool anyButtonPressed)
        {
            if (wheelState == lastWheelState)
                return null;

            var scrollWheelDiff = wheelState - lastWheelState;        // +ve = up / away, -ve = down / toward.

            if (scrollWheelDiff > 0)
                return this.DoEvent(this._MouseWheelActionIndex[new EventKey(EventType.MouseWheel, MouseWheelDirection.Up)]);
            else
                return this.DoEvent(this._MouseWheelActionIndex[new EventKey(EventType.MouseWheel, MouseWheelDirection.Down)]);
        }
        #endregion

        #region Controller
        public virtual ShapeAndSoundTuple MapControllerButton(ControllerButton buttonPress, PlayerIndex player)
        {
            var key = new EventKey(EventType.ControllerButtonPress, buttonPress);
            if (this._ControllerButtonActionIndex.ContainsKey(key))
                return this.DoEvent(this._ControllerButtonActionIndex[key]);
            else
                return this.DoEvent(this._DefaultControllerButtonAction);
        }
        public virtual IEnumerable<ShapeAndSoundTuple> DoControllerAnalogue(ControllerAnalogue analogueInputs, PlayerIndex player)
        {
            // TODO: test this with multiple controllers.
            EventAction analogueThingTemplate = this._AnalogueControllerAction;
            if (analogueThingTemplate == null)
                return null;

            var analogueShapeIsLive = this._AnalogueControllerThing != null && !this._AnalogueControllerThing.AtEndOfLife;
            if (analogueShapeIsLive || !analogueInputs.IsZero)
            {
                // Ensure the analogue object exists.
                if (this._AnalogueControllerThing == null || this._AnalogueControllerThing.AtEndOfLife)
                    this._AnalogueControllerThing = this.CreateBabyShapeFromAction(analogueThingTemplate);
                else if (this._AnalogueControllerThing != null && this._AnalogueControllerThing.State == BabyShape.DrawingState.Solid && !analogueInputs.IsZero)
                    this._AnalogueControllerThing.ResetTimeInCurrentState();

                // Add sound only if the pool contains a sound and the analogue inputs are being used.
                SoundEffect sound = null;
                if (analogueThingTemplate.SoundPool.Count > 0 && !analogueInputs.IsZero)
                    sound = this.CreateSoundEffectFromAction(analogueThingTemplate);


                // Move the location based on the left stick.
                this._AnalogueControllerThing.Location = this._AnalogueControllerThing.Location + (analogueInputs.LeftThumbStick * (float)(this.Game.GraphicsDevice.Viewport.Bounds.Width * this.Game.GraphicsDevice.Viewport.Bounds.Height / 80000));
                // Wrap around the screen when the shape gets close to an edge.
                // TODO: handle transitions of this object between monitors.
                var overlapFactor = 0.25f;
                if (this._AnalogueControllerThing.Location.X < this.Game.GraphicsDevice.Viewport.Bounds.X - (this._AnalogueControllerThing.ActualSize.X * overlapFactor))      // Left.
                    this._AnalogueControllerThing.Location = new Vector2(this.Game.GraphicsDevice.Viewport.Bounds.Width + (this._AnalogueControllerThing.ActualSize.X * overlapFactor), this._AnalogueControllerThing.Location.Y);
                else if (this._AnalogueControllerThing.Location.X > this.Game.GraphicsDevice.Viewport.Bounds.Width + (this._AnalogueControllerThing.ActualSize.X * overlapFactor))      // Right.
                    this._AnalogueControllerThing.Location = new Vector2(this.Game.GraphicsDevice.Viewport.Bounds.X - (this._AnalogueControllerThing.ActualSize.X * overlapFactor), this._AnalogueControllerThing.Location.Y);
                else if (this._AnalogueControllerThing.Location.Y < this.Game.GraphicsDevice.Viewport.Bounds.Y - (this._AnalogueControllerThing.ActualSize.Y * overlapFactor))  // Top.
                    this._AnalogueControllerThing.Location = new Vector2(this._AnalogueControllerThing.Location.X, this.Game.GraphicsDevice.Viewport.Bounds.Height + (this._AnalogueControllerThing.ActualSize.Y * overlapFactor));
                else if (this._AnalogueControllerThing.Location.Y > this.Game.GraphicsDevice.Viewport.Bounds.Height + (this._AnalogueControllerThing.ActualSize.Y * overlapFactor))  // Bottom.
                    this._AnalogueControllerThing.Location = new Vector2(this._AnalogueControllerThing.Location.X, this.Game.GraphicsDevice.Viewport.Bounds.Y - (this._AnalogueControllerThing.ActualSize.Y * overlapFactor));

                // Change the rotation based on the right stick.
                this._AnalogueControllerThing.Rotation = this._AnalogueControllerThing.Rotation + ((analogueInputs.RightThumbStick.X * 1.25f) * (float)this.CurrentGameTime.ElapsedGameTime.TotalSeconds);

                // Change the size based on the two triggers (left makes smaller, right makes bigger).
                this._AnalogueControllerThing.Size = analogueThingTemplate.Template.Size * Math.Max((analogueInputs.RightTrigger * 1.5f) + 1.0f - (analogueInputs.LeftTrigger * 1.5f), 0.2f);


                return new ShapeAndSoundTuple[] { new ShapeAndSoundTuple(this._AnalogueControllerThing, sound) };
            }
            return null;
        }
        #endregion

        #region KeyBashing
        public virtual ShapeAndSoundTuple KeyBashingDetected()
        {
            // Respond with something a bit nasty!
            return this.DoEvent(this._KeyBashingAction, this.Game.GraphicsDevice.Viewport.Bounds.Center.ToVector2());
        }
        #endregion

        #region Idle
        public virtual ShapeAndSoundTuple DoIdleTimeout(TimeSpan idleTime)
        {
            return this.DoEvent(this._IdleAction, this.Game.GraphicsDevice.Viewport.Bounds.Center.ToVector2());
        }
        #endregion

        #region Common Methods
        protected ShapeAndSoundTuple DoEvent(EventAction action)
        {
            return this.DoEvent(action, Vector2.Zero);
        }
        protected ShapeAndSoundTuple DoEvent(EventAction action, Vector2 location)
        {
            if (action == null)
                return null;

            var shape = this.CreateBabyShapeFromAction(action);
            if (shape != null)
            {
                if (location != Vector2.Zero)
                    shape.Location = location;
            }

            var sound = this.CreateSoundEffectFromAction(action);

            return new ShapeAndSoundTuple(shape, sound);
        }
        protected SoundEffect CreateSoundEffectFromAction(EventAction action)
        {
            if (action.SoundPool.Count > 0)
                return action.SoundPool[this.Game.RandomGenerator.Next(0, action.SoundPool.Count)];
            else
                return null;
        }
        protected BabyShape CreateBabyShapeFromAction(EventAction action)
        {
            if (action.TexturePool.Count > 0)
            {
                var template = action.Template;
                var shape = new BabyShape(this.Game);
                shape.Size = template.Size;
                shape.SpinTime = template.SpinTime;
                shape.SolidTime = template.SolidTime;
                shape.FadeOutTime = template.FadeOutTime;
                shape.Colour = action.ColourPool[this.Game.RandomGenerator.Next(0, action.ColourPool.Count)];
                shape.Texture = action.TexturePool[this.Game.RandomGenerator.Next(0, action.TexturePool.Count)];
                return shape;
            }
            else
            {
                return null;
            }
        }
        #endregion
        #endregion

        #region Helpers
        private List<T> CreateAndInsertList<T>(T item) 
        {
            var result = new List<T>();
            result.Add(item);
            return result;
        }
        private BabyPackageLoadException CreateLoadException(String errorMessage)
        {
            return this.CreateLoadException(null, errorMessage);
        }
        private BabyPackageLoadException CreateLoadException(String errorMessage, Exception innerException)
        {
            return this.CreateLoadException(null, errorMessage, innerException);
        }
        private BabyPackageLoadException CreateLoadException(XPathNavigator nodeInError, String errorMessage)
        {
            return this.CreateLoadException(nodeInError, errorMessage, null);
        }
        private BabyPackageLoadException CreateLoadException(XPathNavigator nodeInError, String errorMessage, Exception innerException)
        {
            // Line numbers are available via reflection into MS's internal representation of the XPathNavigator.
            // Try to get them, but degrade gracefully if anything goes wrong.
            var lineNum = -1;
            try
            {
                if (nodeInError != null)
                {
                    var lineNumProp = nodeInError.GetType().GetProperty("LineNumber");
                    if (lineNumProp != null)
                        lineNum = Convert.ToInt32(lineNumProp.GetValue(nodeInError, null));
                }
            } 
            catch (Exception)
            {
                lineNum = -1;
            }

            try
            {

                var errorTemplate = "BabyPackage '{0}': {1}";
                if (lineNum > -1)
                    errorTemplate += " (line {2})";

                return new BabyPackageLoadException(String.Format(errorTemplate, this._PackageFileForExceptionHandling.Name, errorMessage, lineNum), innerException);
            }
            catch (Exception ex)
            {
                if (innerException == null)
                    return new BabyPackageLoadException(errorMessage, ex);
                else 
                    return new BabyPackageLoadException(errorMessage, innerException);
            }
        }
        #endregion
    }
}
