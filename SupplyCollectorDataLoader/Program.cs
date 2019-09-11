using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using S2.BlackSwan.SupplyCollector;
using S2.BlackSwan.SupplyCollector.Models;

namespace SupplyCollectorDataLoader {
    public class Program {
        private static bool _debug = false;

        static void Main(string[] args) {
            if (args.Length == 0 || args.Length != 5 || "--help".Equals(args[0]) || "/?".Equals(args[0])) {
                Console.WriteLine("Usage: DataLoader <CollectorName> <ConnectionString> <DataCollection> <DataEntity> <SamplesCount>");
                return;
            }

            var supplyCollectorName = args[0];
            var connectString = args[1];
            var dataCollectionName = args[2];
            var dataEntityName = args[3];
            var samplesCount = Int64.Parse(args[4]);

            string supplyCollectorPath = Path.Combine(Environment.CurrentDirectory, supplyCollectorName + ".dll");
            if(_debug)
                Console.WriteLine($"[DEBUG] Loading supply collector from {supplyCollectorPath}");

            AssemblyLoadContext.Default.Resolving += Assembly_Resolving;
            Assembly supplyCollectorAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(supplyCollectorPath);
            Type supplyCollectorType = supplyCollectorAssembly.GetType(String.Format("{0}.{0}", supplyCollectorName));
            Type loaderType = supplyCollectorAssembly.GetType(String.Format("{0}.{0}Loader", supplyCollectorName));

            SupplyCollectorDataLoaderBase loader = (SupplyCollectorDataLoaderBase)Activator.CreateInstance(loaderType);
            ISupplyCollector supplyCollector = (ISupplyCollector)Activator.CreateInstance(supplyCollectorType);

            try {
                var dataContainer = new DataContainer() { ConnectionString = connectString };

                var (collections, entities) = supplyCollector.GetSchema(dataContainer);
                var dataCollection = collections.Find(x => x.Name.Equals(dataCollectionName));
                DataEntity dataEntity;

                if (dataCollection == null) {
                    dataCollection = new DataCollection(dataContainer, dataCollectionName);

                    dataEntity = new DataEntity(dataEntityName, DataType.String, "String", dataContainer,
                        dataCollection);
                }
                else {
                    dataEntity = entities.Find(x =>
                        x.Collection.Name.Equals(dataCollectionName) && x.Name.Equals(dataEntityName));

                    if (dataEntity == null) {
                        throw new ApplicationException($"Collection {dataCollectionName} exists, but data entity {dataEntityName} is missing! Cannot proceed.");
                    }
                }

                loader.LoadSamples(dataEntity, samplesCount);
            }
            catch (Exception ex) {
                Console.WriteLine($"Error loading data: {ex}");
            }
        }

        private static Assembly Assembly_Resolving(AssemblyLoadContext context, AssemblyName name)
        {
            if (_debug)
                Console.WriteLine($"[DEBUG] Resolving {name.FullName}");

            var foundDlls = Directory.GetFileSystemEntries(new FileInfo(Environment.CurrentDirectory).FullName, name.Name + ".dll", SearchOption.AllDirectories);
            if (foundDlls.Any())
            {
                if (_debug) {
                    foreach (var foundDll in foundDlls) {
                        Console.WriteLine($"  resolved to {foundDll}");
                    }
                }

                return context.LoadFromAssemblyPath(foundDlls[0]);
            }

            return context.LoadFromAssemblyName(name);
        }
    }
}
