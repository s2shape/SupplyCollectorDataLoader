using System;
using System.Collections.Generic;
using System.Text;

namespace SupplyCollectorDataLoader
{
    public class AssemblyMissingException : Exception
    {
        public AssemblyMissingException() {
        }

        public AssemblyMissingException(string message) : base(message) {
        }

        public AssemblyMissingException(string message, Exception innerException) : base(message, innerException) {
        }
    }
}
