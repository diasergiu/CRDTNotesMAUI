using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.Entities.Server;
using DatabaseLibrary.RequestBody;
using DatabaseLibrary.RequestBody.EntityMappers;
using DatabaseLibrary.ResponsBody;
using DatabaseLibrary.WrapperClasses;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SlackAPI;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace MAUIClientUI.Services
{
    public class ClientServices : ServicesClient
    {
        public ClientServices(string URLModifier) : base(URLModifier)
        {

        }


        // untested if it workes
        
    }
}
