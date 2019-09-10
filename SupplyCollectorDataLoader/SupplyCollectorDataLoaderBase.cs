using System;
using S2.BlackSwan.SupplyCollector.Models;

namespace SupplyCollectorDataLoader
{
    public abstract class SupplyCollectorDataLoaderBase {

        public abstract void LoadSamples(DataEntity dataEntity, long count);

    }
}
