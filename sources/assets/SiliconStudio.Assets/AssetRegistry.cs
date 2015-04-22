﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SharpYaml.Serialization;
using SiliconStudio.Assets.Analysis;
using SiliconStudio.Assets.Diff;

using SiliconStudio.Assets.Compiler;
using SiliconStudio.Assets.Serializers;
using SiliconStudio.Core;
using SiliconStudio.Core.Diagnostics;
using SiliconStudio.Core.Reflection;
using SiliconStudio.Core.VisualStudio;
using SiliconStudio.Core.Yaml;
using AttributeRegistry = SiliconStudio.Core.Reflection.AttributeRegistry;
using IObjectFactory = SiliconStudio.Core.Reflection.IObjectFactory;

namespace SiliconStudio.Assets
{
    /// <summary>
    /// A registry for file extensions, <see cref="IAssetImporter"/>, <see cref="IObjectFactory"/> 
    /// and aliases associated with assets.
    /// </summary>
    public static class AssetRegistry
    {
        private static Logger log = GlobalLogger.GetLogger("Assets.Registry");
        private static readonly SolutionPlatformCollection supportedPlatforms = new SolutionPlatformCollection();
        private static readonly Dictionary<Type, string> RegisteredDefaultAssetExtension = new Dictionary<Type, string>();
        private static readonly Dictionary<Type, bool> RegisteredDynamicThumbnails = new Dictionary<Type, bool>();
        private static readonly HashSet<Type> AssetTypes = new HashSet<Type>();
        internal static readonly HashSet<Type> RegisteredPackageSessionAnalysisTypes = new HashSet<Type>();

        private static readonly Dictionary<Guid, IAssetImporter> RegisteredImportersInternal = new Dictionary<Guid, IAssetImporter>();
        private static readonly Dictionary<Type, int> RegisteredFormatVersions = new Dictionary<Type, int>();
        private static readonly HashSet<Type> RegisteredInternalAssetTypes = new HashSet<Type>();
        private static readonly Dictionary<Type, Type[]> RegisteredFormatVersionUpdaterTypes = new Dictionary<Type, Type[]>();
        private static readonly Dictionary<string, List<IAssetImporter>> RegisterImportExtensions = new Dictionary<string, List<IAssetImporter>>(StringComparer.InvariantCultureIgnoreCase);
        private static readonly HashSet<string> RegisteredAssetFileExtensions = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        internal static readonly HashSet<Assembly> RegisteredAssemblies = new HashSet<Assembly>();
        internal static readonly HashSet<IYamlSerializableFactory> RegisteredSerializerFactories = new HashSet<IYamlSerializableFactory>();
        internal static readonly List<IDataCustomVisitor> RegisteredDataVisitNodes = new List<IDataCustomVisitor>();
        internal static readonly List<IDataCustomVisitor> RegisteredDataVisitNodeBuilders = new List<IDataCustomVisitor>();
        private static Func<object, string, string> stringExpander;

        /// <summary>
        /// Gets the supported platforms.
        /// </summary>
        /// <value>The supported platforms.</value>
        public static SolutionPlatformCollection SupportedPlatforms
        {
            get
            {
                return supportedPlatforms;
            }
        }

        /// <summary>
        /// Registers the supported platforms.
        /// </summary>
        /// <param name="platforms">The platforms.</param>
        /// <exception cref="System.ArgumentNullException">platforms</exception>
        public static void RegisterSupportedPlatforms(List<SolutionPlatform> platforms)
        {
            if (platforms == null) throw new ArgumentNullException("platforms");
            if (supportedPlatforms.Count > 0) throw new InvalidOperationException("Cannot register new platforms. RegisterSupportedPlatforms can only be called once");

            supportedPlatforms.AddRange(platforms);
        }

        /// <summary>
        /// Registers the string expander used by the package references.
        /// </summary>
        /// <param name="expander">The expander.</param>
        public static void RegisterStringExpander(Func<object, string, string> expander)
        {
            stringExpander = expander;
        }

        /// <summary>
        /// Expands a string using the registered string expander (<see cref="RegisterStringExpander"/>)
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="stringToExpand">The string to expand.</param>
        /// <returns>System.String.</returns>
        public static string ExpandString(object context, string stringToExpand)
        {
            if (stringExpander != null)
            {
                return stringExpander(context, stringToExpand);
            }
            return stringToExpand;
        }

        /// <summary>
        /// Gets the asset file extensions.
        /// </summary>
        /// <returns>System.String[][].</returns>
        public static string[] GetAssetFileExtensions()
        {
            return RegisteredAssetFileExtensions.ToArray();
        }

        /// <summary>
        /// Gets an enumeration of registered importers.
        /// </summary>
        /// <value>The registered importers.</value>
        public static IEnumerable<IAssetImporter> RegisteredImporters
        {
            get
            {
                lock (RegisteredImportersInternal)
                {
                    return RegisteredImportersInternal.Values;
                }
            }
        }

        /// <summary>
        /// Determines whether the extension is an asset file type.
        /// </summary>
        /// <param name="extension">The extension.</param>
        /// <returns><c>true</c> if [is asset file extension] [the specified extension]; otherwise, <c>false</c>.</returns>
        public static bool IsAssetFileExtension(string extension)
        {
            if (extension == null) return false;
            return RegisteredAssetFileExtensions.Contains(extension);
        }

        /// <summary>
        /// Gets the default extension associated with an asset.
        /// </summary>
        /// <param name="assetType">The type.</param>
        /// <returns>System.String.</returns>
        public static string GetDefaultExtension(Type assetType)
        {
            AssertAssetType(assetType);
            string extension;
            RegisteredDefaultAssetExtension.TryGetValue(assetType, out extension);
            return extension;
        }

        /// <summary>
        /// Gets the current format version of an asset.
        /// </summary>
        /// <param name="assetType">The asset type.</param>
        /// <returns>The current format version of this asset.</returns>
        public static int GetFormatVersion(Type assetType)
        {
            AssertAssetType(assetType);
            int version;
            RegisteredFormatVersions.TryGetValue(assetType, out version);
            return version;
        }

        /// <summary>
        /// Gets the current format version of an asset.
        /// </summary>
        /// <param name="assetType">The asset type.</param>
        /// <returns>The current format version of this asset.</returns>
        public static Type[] GetFormatVersionUpdaterTypes(Type assetType)
        {
            AssertAssetType(assetType);
            Type[] updaters;
            RegisteredFormatVersionUpdaterTypes.TryGetValue(assetType, out updaters);
            return updaters;
        }

        /// <summary>
        /// Gets the default extension associated with an asset.
        /// </summary>
        /// <typeparam name="T">Type of the asset.</typeparam>
        /// <returns>System.String.</returns>
        public static string GetDefaultExtension<T>() where T : Asset
        {
            return GetDefaultExtension(typeof(T));
        }

        /// <summary>
        /// Returns an array of asset types that can be instanced with <see cref="ObjectFactory.NewInstance"/>.
        /// </summary>
        /// <returns>An array of <see cref="Type"/> elements.</returns>
        public static Type[] GetInstantiableTypes()
        {
            return ObjectFactory.FindRegisteredFactories().Where(type => typeof(Asset).IsAssignableFrom(type) && type.IsPublic && !RegisteredInternalAssetTypes.Contains(type)).ToArray();
        }

        /// <summary>
        /// Gets a boolean indicating whether an asset type has a dynamic thumbnail.
        /// </summary>
        /// <param name="assetType">Type of the asset.</param>
        /// <returns><c>true</c> if [has dynamic thumbnail] [the specified asset type]; otherwise, <c>false</c>.</returns>
        public static bool HasDynamicThumbnail(Type assetType)
        {
            AssertAssetType(assetType);
            bool hasThumbnail;
            RegisteredDynamicThumbnails.TryGetValue(assetType, out hasThumbnail);
            return hasThumbnail;
        }

        /// <summary>
        /// Returns an array of asset types that are non-abstract and public.
        /// </summary>
        /// <returns>An array of <see cref="Type"/> elements.</returns>
        public static Type[] GetPublicTypes()
        {
            return AssetTypes.ToArray();
        }

        /// <summary>
        /// Determines whether [is importer supporting extension] [the specified extension].
        /// </summary>
        /// <param name="extension">The extension.</param>
        /// <returns><c>true</c> if [is importer supporting extension] [the specified extension]; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.ArgumentNullException">extension</exception>
        public static bool IsImporterSupportingExtension(string extension)
        {
            if (extension == null) throw new ArgumentNullException("extension");

            lock (RegisterImportExtensions)
            {
                return RegisterImportExtensions.ContainsKey(extension);
            }
        }

        /// <summary>
        /// Finds the importer associated with an asset by the extension of the file to import.
        /// </summary>
        /// <param name="extension">The extension of the file to import.</param>
        /// <returns>An instance of the importer of null if not found.</returns>
        public static IEnumerable<IAssetImporter> FindImporterByExtension(string extension)
        {
            if (extension == null) throw new ArgumentNullException("extension");

            lock (RegisterImportExtensions)
            {
                List<IAssetImporter> importers;
                if (RegisterImportExtensions.TryGetValue(extension, out importers))
                {
                    var newImporters = new List<IAssetImporter>(importers);
                    return newImporters;
                }
            }
            return Enumerable.Empty<IAssetImporter>();
        }

        /// <summary>
        /// Finds an importer by its id.
        /// </summary>
        /// <param name="importerId">The importer identifier.</param>
        /// <returns>An instance of the importer of null if not found.</returns>
        public static IAssetImporter FindImporterById(Guid importerId)
        {
            lock (RegisteredImportersInternal)
            {
                IAssetImporter importer;
                if (RegisteredImportersInternal.TryGetValue(importerId, out importer))
                {
                    return importer;
                }
            }
            return null;
        }

        /// <summary>
        /// Registers a <see cref="IAssetImporter" /> for the specified asset type.
        /// </summary>
        /// <param name="importer">The importer.</param>
        /// <exception cref="System.ArgumentNullException">importer</exception>
        public static void RegisterImporter(IAssetImporter importer)
        {
            if (importer == null) throw new ArgumentNullException("importer");

            // Register this importer
            lock (RegisteredImportersInternal)
            {
                if (RegisteredImportersInternal.ContainsKey(importer.Id))
                    return;
                RegisteredImportersInternal[importer.Id] = importer;
            }

            // Register file extensions to type
            var extensions = FileUtility.GetFileExtensions(importer.SupportedFileExtensions);
            lock (RegisterImportExtensions)
            {
                foreach (var extension in extensions)
                {
                    List<IAssetImporter> importers;
                    if (!RegisterImportExtensions.TryGetValue(extension, out importers))
                    {
                        importers = new List<IAssetImporter>();
                        RegisterImportExtensions.Add(extension, importers);
                    }
                    if (!importers.Contains(importer))
                    {
                        importers.Add(importer);
                        // Always keep the list of importer sotred by their DisplayRank
                        importers.Sort( (left, right) => -left.DisplayRank.CompareTo(right.DisplayRank));
                    }
                }
            }
        }

        /// <summary>
        /// Registers a <see cref="AssetDescription" /> for the specified asset type.
        /// </summary>
        /// <param name="assetType">Type of the asset.</param>
        /// <param name="isDynamicThumbnail">if set to <c>true</c> [is dynamic thumbnail].</param>
        /// <exception cref="System.ArgumentNullException">description</exception>
        private static void RegisterDynamicThumbnail(Type assetType, bool isDynamicThumbnail)
        {
            AssertAssetType(assetType);
            RegisteredDynamicThumbnails[assetType] = isDynamicThumbnail;
        }

        /// <summary>
        /// Registers the asset assembly. This assembly should provide <see cref="Asset"/> objects, associated with
        /// <see cref="ICompiler"/> and optionaly a <see cref="IAssetImporter"/>.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <exception cref="System.ArgumentNullException">assembly</exception>
        /// <exception cref="AssetException">
        /// Invalid compiler type [{0}], must inherit from IAssetImporter.ToFormat(assetCompiler.TypeName)
        /// or
        /// Unable to instantiate compiler [{0}].ToFormat(assetCompiler.TypeName)
        /// or
        /// Invalid importer type [{0}], must inherit from IAssetImporter.ToFormat(assetImporter.ImpoterTypeName)
        /// or
        /// Unable to instantiate importer [{0}].ToFormat(assetImporter.ImpoterTypeName)
        /// </exception>
        public static void RegisterAssembly(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException("assembly");

            if (RegisteredAssemblies.Contains(assembly))
            {
                return;
            }
            RegisteredAssemblies.Add(assembly);

            // Process Asset types.
            foreach (var type in assembly.GetTypes())
            {
                // Register serializer factories
                if (type.GetCustomAttribute<YamlSerializerFactoryAttribute>() != null)
                {
                    if (typeof(IYamlSerializableFactory).IsAssignableFrom(type))
                    {
                        try
                        {
                            var yamlFactory = (IYamlSerializableFactory)Activator.CreateInstance(type);
                            RegisteredSerializerFactories.Add(yamlFactory);

                            // TODO: Handle IDataCustomVisitor on its own instead of relying on the coupling with IYamlSerializableFactory
                            var dataCustomVisitor = yamlFactory as IDataCustomVisitor;
                            if (dataCustomVisitor != null)
                            {
                                RegisteredDataVisitNodes.Add(dataCustomVisitor);
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Error("Unable to instantiate serializer factory [{0}]", ex, type);
                        }
                    }
                }

                if (type.GetCustomAttribute<DiffNodeBuilderAttribute>() != null)
                {
                    if (typeof(IDataCustomVisitor).IsAssignableFrom(type))
                    {
                        try
                        {
                            var dataCustomVisitor = (IDataCustomVisitor)Activator.CreateInstance(type);
                            RegisteredDataVisitNodeBuilders.Add(dataCustomVisitor);
                        }
                        catch (Exception ex)
                        {
                            log.Error("Unable to instantiate diff converter [{0}]", ex, type);
                        }
                    }
                }

                // Asset importer
                if (typeof(IAssetImporter).IsAssignableFrom(type) && type.GetConstructor(new Type[0]) != null)
                {
                    try
                    {
                        var importerInstance = Activator.CreateInstance(type) as IAssetImporter;

                        // Register the importer instance
                        RegisterImporter(importerInstance);
                    }
                    catch (Exception ex)
                    {
                        log.Error("Unable to instantiate importer [{0}]", ex, type.Name);
                    }
                }

                if (typeof(PackageSessionAnalysisBase).IsAssignableFrom(type) && type.GetConstructor(new Type[0]) != null )
                {
                    RegisteredPackageSessionAnalysisTypes.Add(type);
                }

                // Only process Asset types
                var assetType = type;
                if (!typeof(Asset).IsAssignableFrom(assetType) || !assetType.IsClass)
                {
                    continue;
                }

                // Store in a list all asset types loaded
                if (assetType.IsPublic && !assetType.IsAbstract)
                {
                    AssetTypes.Add(assetType);
                }

                var isSourceCodeAsset = typeof(SourceCodeAsset).IsAssignableFrom(assetType);

                // Asset FileExtensions
                var assetDescriptionAttribute = assetType.GetCustomAttribute<AssetDescriptionAttribute>();
                if (assetDescriptionAttribute != null)
                {
                    if (assetDescriptionAttribute.FileExtensions != null)
                    {
                        var extensions = FileUtility.GetFileExtensions(assetDescriptionAttribute.FileExtensions);
                        RegisteredDefaultAssetExtension[assetType] = extensions.FirstOrDefault();
                        foreach (var extension in extensions)
                        {
                            RegisteredAssetFileExtensions.Add(extension);

                            // If the asset is a pure sourcecode asset, then register the serializer
                            if (isSourceCodeAsset)
                            {
                                SourceCodeAssetSerializer.RegisterExtension(assetType, extension);
                            }
                        }
                    }
                    if (!assetDescriptionAttribute.AllowUserCreation)
                    {
                        RegisteredInternalAssetTypes.Add(assetType);
                    }
                }

                var assetFormatVersion = assetType.GetCustomAttribute<AssetFormatVersionAttribute>();
                if (assetFormatVersion != null)
                {
                    RegisteredFormatVersions.Add(assetType, assetFormatVersion.Version);
                    RegisteredFormatVersionUpdaterTypes.Add(assetType, assetFormatVersion.AssetUpdaterTypes);
                }

                // Asset factory
                var assetFactory = assetType.GetCustomAttribute<ObjectFactoryAttribute>();
                if (assetFactory != null)
                {
                    try
                    {
                        ObjectFactory.RegisterFactory(assetType);
                    }
                    catch (Exception ex)
                    {
                        log.Error("Unable to instantiate factory [{0}] for asset [{1}]", ex, assetFactory.FactoryTypeName, assetType);
                    }
                }
                else
                {
                    var assetConstructor = assetType.GetConstructor(Type.EmptyTypes);
                    if (assetConstructor != null)
                    {
                        // Register the asset even if it has no factory (default using empty constructor)
                        ObjectFactory.RegisterFactory(assetType, null);
                    }
                }

                // Asset description
                var thumbnailCompilerAttribute = assetType.GetCustomAttribute<ThumbnailCompilerAttribute>();
                if (thumbnailCompilerAttribute != null)
                {
                    RegisterDynamicThumbnail(assetType, thumbnailCompilerAttribute.DynamicThumbnails);
                }
            }
        }

        private static void AssertAssetType(Type assetType)
        {
            if (assetType == null)
                throw new ArgumentNullException("assetType");

            if (!typeof(Asset).IsAssignableFrom(assetType)) 
                throw new ArgumentException("Type [{0}] must be assignable to Asset".ToFormat(assetType), "assetType");
        }

        static AssetRegistry()
        {
            // Statically find all assemblies related to assets and register them
            var assemblies = AssemblyRegistry.Find(AssemblyCommonCategories.Assets);
            foreach (var assembly in assemblies)
            {
                RegisterAssembly(assembly);
            }
            AssemblyRegistry.AssemblyRegistered += AssemblyRegistry_AssemblyRegistered;
        }

        static void AssemblyRegistry_AssemblyRegistered(object sender, AssemblyRegisteredEventArgs e)
        {
            // Handle delay-loading assemblies
            if (e.Categories.Contains(AssemblyCommonCategories.Assets))
            {
                RegisterAssembly(e.Assembly);
            }
        }
    }
}