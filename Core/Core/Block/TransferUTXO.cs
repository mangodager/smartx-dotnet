using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Collections;

namespace ETModel
{
    public class TransferUTXO : ComponentWithId
    {
        public int          version;
        public int          type;
        public long         locktime;
        public List<Vin>    vin;
        public List<Vout>   vout;

        public class Vin
        {
            public string txid;
            public string vout;
            public string scriptSig;
            public string sequence;
        }
         
        public class Vout
        {
            public string value;
            public string scriptPubKey;
        }

    }

}
