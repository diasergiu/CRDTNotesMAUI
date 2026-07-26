using DatabaseLibrary.Entities.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseLibrary.Entities
{
    public class DatabaseServices : IDatabaseServices
    {
        private readonly string _instanceId;

        public DatabaseServices(string instanceId)
        {
            _instanceId = instanceId;
        }
        public DbContextClient GetContext()
        {
            return new DbContextClient(_instanceId);
        }
    }
}
