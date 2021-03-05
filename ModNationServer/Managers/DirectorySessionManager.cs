using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModNationServer
{
    class DirectorySessionManager
    {
        public static string RandomSessionUUID()
        {
            Guid uuid = Guid.NewGuid();
            //Create a session UUID
            return uuid.ToString();
        }
    }
}
