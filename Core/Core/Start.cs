using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Collections;

namespace ETModel
{

    public class ComponentStart : Component
    {
        public override void Awake(JToken jd = null)
        {
            LoadComponent(jd, entity);

        }

        static public void LoadComponent(JToken jd,Entity parent)
        {
            List<Type> list = AssemblyHelper.GetTypes();

            foreach (JProperty jp in jd)//遍历数组
            {
                Type item = list.Find(c => c.Name == jp.Name);//返回指定条件的元素
                if (item != null&& item.Name!= "Entity")
                {
                    Entity obj = parent;

                    try
                    {
                        if (jd[item.Name]["Entity"] != null)
                        {
                            string name = jd[item.Name]["Entity"].ToString();

                            obj = Entity.Root.Find(name);
                            if (obj == null)
                                obj = new Entity(name, Entity.Root);
                        }

                        if(parent!=null&& parent!= obj)
                            Log.Debug($"{parent.Name}.{item.Name} Entity");
                        else
                            Log.Debug($"{obj.Name}.{item.Name}");
                        obj.AddComponent(item, jd[item.Name]);

                        LoadComponent(jd[item.Name], obj);
                    }
                    catch(Exception e)
                    {
                        int HResult = e.HResult;
                        Log.Error($"{obj.Name} + {item.Name} {e.ToString()}");
                    }
                }
            }
        }

        public override void Start()
        {



        }



    }
    
}
