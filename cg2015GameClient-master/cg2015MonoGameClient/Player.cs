using System;
using System.Web;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.ComponentModel;

namespace cgMonoGameServer2015
{
    [Serializable]
    public class Player
    {
        public Guid playerID;
        public string GamerTag;
        public string UserName;
        public string FirstName;
        public string SecondName;
        public int XP;
    }
}