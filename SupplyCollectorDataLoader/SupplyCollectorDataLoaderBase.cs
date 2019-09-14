using System;
using S2.BlackSwan.SupplyCollector.Models;

namespace SupplyCollectorDataLoader
{
    public abstract class SupplyCollectorDataLoaderBase {
        /// <summary>
        /// Does initialization work, if necessary. E.g., creates a database
        /// </summary>
        /// <param name="dataContainer">Data container parameters</param>
        public abstract void InitializeDatabase(DataContainer dataContainer);

        /// <summary>
        /// Load random string samples into data entity. Use for load test and test harness
        /// </summary>
        /// <param name="dataEntities">Data entities to load data to. Must belong to same DataCollection</param>
        /// <param name="count">Amount of samples to load</param>
        public abstract void LoadSamples(DataEntity[] dataEntities, long count);

        /// <summary>
        /// Loads data which is used by supply collector unit tests. Must check if data already exists
        /// </summary>
        /// <param name="dataContainer">Data container parameters</param>
        public abstract void LoadUnitTestData(DataContainer dataContainer);
    }
}
