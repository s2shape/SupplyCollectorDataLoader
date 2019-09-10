using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using S2.BlackSwan.SupplyCollector.Models;

namespace SupplyCollectorDataLoader {
    class Program {
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

            Assembly supplyCollectorAssembly = Assembly.LoadFile(supplyCollectorPath);
            Type supplyCollectorType = supplyCollectorAssembly.GetType(String.Format("{0}.{0}", supplyCollectorName));

            SupplyCollectorDataLoaderBase loader = (SupplyCollectorDataLoaderBase)Activator.CreateInstance(supplyCollectorType);

            var dataContainer = new DataContainer() {ConnectionString = connectString};
            var dataCollection = new DataCollection(dataContainer, dataCollectionName);
            var dataEntity = new DataEntity(dataEntityName, DataType.String, "String", dataContainer, dataCollection);

            loader.LoadSamples(dataEntity, samplesCount);
        }
    }
}
