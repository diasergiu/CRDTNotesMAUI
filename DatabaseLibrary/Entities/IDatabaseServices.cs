using DatabaseLibrary.Entities.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseLibrary.Entities
{
    public interface IDatabaseServices
    {
        DbContextClient GetContext();
    }
}
