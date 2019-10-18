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
        private static Dictionary<string, Assembly> _loadedAssemblies = new Dictionary<string, Assembly>();

        static void PrintUsage() {
            Console.WriteLine("Usage: * DataLoader -init <CollectorName> <ConnectionString>");
            Console.WriteLine("       * DataLoader -xunit <CollectorName> <ConnectionString>");
            Console.WriteLine("       * DataLoader -samples <CollectorName> <ConnectionString> <DataCollection> <DataEntity,DataEntity:type,...> <SamplesCount>");
            Console.WriteLine("  Data types: string,int,bool,double,date. Default is string");

            Environment.Exit(1);
        }

        private static (SupplyCollectorDataLoaderBase, ISupplyCollector) LoadSupplyCollector(string supplyCollectorName) {
            string supplyCollectorPath = Path.Combine(Environment.CurrentDirectory, supplyCollectorName + ".dll");
            string supplyCollectorLoaderPath = Path.Combine(Environment.CurrentDirectory, supplyCollectorName + "Loader.dll");

            AssemblyLoadContext.Default.Resolving += Assembly_Resolving;

            if (_debug)
                Console.WriteLine($"[DEBUG] Loading supply collector from {supplyCollectorPath}");
            Assembly supplyCollectorAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(supplyCollectorPath);

            if (_debug)
                Console.WriteLine($"[DEBUG] Loading supply collector loader from {supplyCollectorLoaderPath}");
            Assembly supplyCollectorLoaderAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(supplyCollectorLoaderPath);

            Type supplyCollectorType = supplyCollectorAssembly.GetType(String.Format("{0}.{0}", supplyCollectorName));
            Type loaderType = supplyCollectorLoaderAssembly.GetType(String.Format("{0}Loader.{0}Loader", supplyCollectorName));

            ISupplyCollector supplyCollector = (ISupplyCollector)Activator.CreateInstance(supplyCollectorType);
            SupplyCollectorDataLoaderBase loader = (SupplyCollectorDataLoaderBase)Activator.CreateInstance(loaderType);

            return (loader, supplyCollector);
        }

        private static DataType ConvertDataType(string dataType) {
            if ("string".Equals(dataType, StringComparison.InvariantCultureIgnoreCase)) {
                return DataType.String;
            } else if ("int".Equals(dataType, StringComparison.InvariantCultureIgnoreCase)) {
                return DataType.Int;
            } else if ("bool".Equals(dataType, StringComparison.InvariantCultureIgnoreCase)) {
                return DataType.Boolean;
            } else if ("double".Equals(dataType, StringComparison.InvariantCultureIgnoreCase)) {
                return DataType.Double;
            } else if ("date".Equals(dataType, StringComparison.InvariantCultureIgnoreCase)) {
                return DataType.DateTime;
            }

            return DataType.Unknown;
        }

        static void Main(string[] args) {
            Console.WriteLine("SupplyCollectorDataLoader v." + typeof(Program).Assembly.GetName().Version);
            if (args.Length == 0 || "--help".Equals(args[0]) || "/?".Equals(args[0])) {
                PrintUsage();
                return;
            }

            var mode = args[0].Trim();
            if ("-init".Equals(mode, StringComparison.InvariantCultureIgnoreCase)) {
                if (args.Length < 3) {
                    PrintUsage();
                    return;
                }

                var supplyCollectorName = args[1].Trim();
                var connectString = args[2].Trim();

                var (loader, _) = LoadSupplyCollector(supplyCollectorName);

                try {
                    Console.WriteLine("Initializing data container...");
                    loader.InitializeDatabase(new DataContainer() {ConnectionString = connectString});
                    Console.WriteLine("Success.");
                }
                catch (Exception ex) {
                    Console.WriteLine($"Error: {ex}");
                    Environment.Exit(2);
                }
            } else if ("-xunit".Equals(mode, StringComparison.InvariantCultureIgnoreCase)) {
                if (args.Length < 3)
                {
                    PrintUsage();
                    return;
                }

                var supplyCollectorName = args[1].Trim();
                var connectString = args[2].Trim();

                var (loader, _) = LoadSupplyCollector(supplyCollectorName);

                try {
                    Console.WriteLine("Loading unit test data...");
                    loader.LoadUnitTestData(new DataContainer() {ConnectionString = connectString});
                    Console.WriteLine("Success.");
                }
                catch (Exception ex) {
                    Console.WriteLine($"Error: {ex}");
                    Environment.Exit(2);
                }
            } else if ("-samples".Equals(mode, StringComparison.InvariantCultureIgnoreCase))
            {
                if (args.Length < 6)
                {
                    PrintUsage();
                    return;
                }

                var supplyCollectorName = args[1].Trim();

                var (loader, collector) = LoadSupplyCollector(supplyCollectorName);

                var connectString = args[2].Trim();
                var dataCollectionName = args[3];
                var dataEntityNamesArg = args[4];
                var samplesCount = Int64.Parse(args[5]);

                var dataEntityNames = dataEntityNamesArg.Split(",", StringSplitOptions.RemoveEmptyEntries);
                var dataEntityTypes = new string[dataEntityNames.Length];

                for (int i = 0; i < dataEntityNames.Length; i++) {
                    if (dataEntityNames[i].Contains(":")) {
                        var entityNameParts = dataEntityNames[i].Split(":");

                        dataEntityNames[i] = entityNameParts[0];
                        dataEntityTypes[i] = entityNameParts[1];
                    }
                    else {
                        dataEntityTypes[i] = "string";
                    }
                }

                try
                {
                    Console.WriteLine("Loading samples data...");
                    var dataContainer = new DataContainer() { ConnectionString = connectString };

                    var (collections, entities) = collector.GetSchema(dataContainer);
                    var dataCollection = collections.Find(x => x.Name.Equals(dataCollectionName));
                    DataEntity[] dataEntities = new DataEntity[dataEntityNames.Length];

                    if (dataCollection == null) {
                        dataCollection = new DataCollection(dataContainer, dataCollectionName);
                    }
                    else {
                        foreach (var dataEntityName in dataEntityNames) {
                            if(!entities.Any(x => x.Collection.Name.Equals(dataCollectionName) && x.Name.Equals(dataEntityName)))
                                throw new ApplicationException($"Collection {dataCollectionName} exists, but data entity {dataEntityName} is missing! Cannot proceed.");
                        }
                    }

                    for (int i = 0; i < dataEntityNames.Length; i++) {
                        dataEntities[i] = entities.Find(x =>
                            x.Collection.Name.Equals(dataCollectionName) && x.Name.Equals(dataEntityNames[i]));

                        if (dataEntities[i] == null) {
                            dataEntities[i] = new DataEntity(dataEntityNames[i], ConvertDataType(dataEntityTypes[i]), dataEntityTypes[i], dataContainer,
                                dataCollection);
                        }
                    }

                    loader.LoadSamples(dataEntities, samplesCount);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex}");
                    Environment.Exit(2);
                }
            }
        }

        private static Assembly Assembly_Resolving(AssemblyLoadContext context, AssemblyName name) {
            if (_debug)
                Console.WriteLine($"[DEBUG] Resolving {name.FullName}");

            if (_loadedAssemblies.ContainsKey(name.Name)) {
                return _loadedAssemblies[name.Name];
            }

            Assembly a = null;

            var foundDlls = Directory.GetFileSystemEntries(new FileInfo(Environment.CurrentDirectory).FullName,
                name.Name + ".dll", SearchOption.AllDirectories);
            if (foundDlls.Any()) {
                if (_debug) {
                    foreach (var foundDll in foundDlls) {
                        Console.WriteLine($"  resolved to {foundDll}");
                    }
                }

                a = context.LoadFromAssemblyPath(foundDlls[0]);
            }
            else {
                throw new AssemblyMissingException($"Assembly {name} is missing!");
            }

            if(a != null)
                _loadedAssemblies.Add(name.Name, a);

            return a;
        }
    }
}
