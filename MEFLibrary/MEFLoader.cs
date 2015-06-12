using System;
using System.Collections.Generic;
using System.Linq; 
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;

namespace lawsoncs.MEFLibrary.MEF
{

    /// <summary>
    /// MEFLoader class that responsible for :
    ///    The interface for all the MEF loading process, i.e. he is the black-box.
    ///    holding all the already imported object (for better performance) 
    ///    holding all the already exist importers (one for each type)
    /// Written by Shai Vashdi - Shai.Vashdi.Net@gmail.com - All rights reserved.
    /// </summary>
    public class MEFLoader
    {
        Dictionary<string, List<object>> importers = new Dictionary<string, List<object>>();

        public virtual ICollection<T> LoadByTag<T>(string path, string tag)
        {
            var importer = GetImporter<T>(path);

            return importer.LoadByMEF(path, tag);
        }

        protected MEFImporter<T> GetImporter<T>(string path)
        {
            var importerList = GetImporterList(path);

            var importer = importerList.OfType<MEFImporter<T>>().FirstOrDefault();

            if (importer == null)
            {
                importer = new MEFImporter<T>(path);
                importerList.Add(importer);

                //Write to Log:
                //UILogService.Instance.WriteToLog(E_ErrorSeverity.e_Information, "New MEFImporter was created for Path & Type" + path + importer.GetType().ToString());
            }

            return importer;
        }

        protected List<object> GetImporterList(string path)
        {
            if (importers.ContainsKey(path) == false)
                importers.Add(path, new List<object>());

            return importers[path];
        }

        public virtual ICollection<T> LoadByType<T>(string path)
        {
            return LoadByTag<T>(path, String.Empty);
        }
    }

    /// <summary>
    /// The imported objects metadata interface. i.e. the set of 
    /// properties we can filter by all the already imported objects.
    /// Written by Shai Vashdi - Shai.Vashdi.Net@gmail.com - All rights reserved.
    /// </summary>
    public interface IMetadata
    {
        string Name { get; }
    }

    /// <summary>
    /// Generic Class is responsible for MEF Import of certain type T.
    /// Written by Shai Vashdi - Shai.Vashdi.Net@gmail.com - All rights reserved.
    /// </summary>
    public class MEFImporter<T>
    {
        [ImportMany(AllowRecomposition = true)]
        public IEnumerable<Lazy<T, IMetadata>> imports { get; set; }

        MEFImporter()
        {
        }

        public MEFImporter(string path)
            : this()
        {
            directoryCatalog = new DirectoryCatalog(path);
        }

        protected DirectoryCatalog directoryCatalog = null;

        protected void DoImport(string path)
        {
            //An aggregate catalog that combines multiple catalogs
            var catalog = new AggregateCatalog();
            //Adds all the parts found in all assemblies in 
            //the same directory as the executing program
            catalog.Catalogs.Add(directoryCatalog);

            //Create the CompositionContainer with the parts in the catalog
            CompositionContainer container = new CompositionContainer(catalog);

            //Fill the imports of this object
            container.ComposeParts(this);
        }

        public ICollection<T> LoadByMEF(string path, string name)
        {
            var res = new List<T>();

            DoImport(path);
            //Test MEF
            //AppDomain MyDomain = AppDomain.CurrentDomain;
            //var AssembliesLoaded = MyDomain.GetAssemblies();

            foreach (Lazy<T, IMetadata> module in imports)
            {
                if (module.Metadata.Name == name || String.IsNullOrEmpty(name))
                {
                    res.Add(module.Value); //Will create an instance
                }
            }

            return res;
        }
    }
}