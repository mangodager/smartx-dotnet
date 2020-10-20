using System.Collections.Generic;
using LevelDB;

namespace ETModel
{
    public class DbUndo
    {
        public long height;
        public List<string> keys = new List<string>();


    }


}
