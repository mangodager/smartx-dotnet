using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ETModel
{
    public class Entity
    {
        static public Entity Root = new Entity("Root");

        private Dictionary<string, Component> componentsSet = new Dictionary<string, Component>();
        private List<Component>  components = new List<Component>();
        public  bool enable = false;
        public  string Name = "";

        public Entity( string n = "" , Entity parent = null )
        {
            Name = n;
            if (parent!=null)
                parent.AddChild(this);
        }

        public virtual K AddComponent<K>(JToken jd =null) where K : Component, new()
        {
            Type type = typeof(K);
            if (this.componentsSet.ContainsKey(type.Name))
            {
                throw new Exception($"AddComponent, component already exist, component: {type.Name}");
            }
            K component = new K();
            component.entity = this;
            this.componentsSet.Add(type.Name, component);
            components.Add(component);
            component.Awake(jd);
            return component;
        }
        public virtual Component AddComponent(Type type,JToken jd = null)
        {
            if (this.componentsSet.ContainsKey(type.Name))
            {
                throw new Exception($"AddComponent, component already exist, component: {type.Name}");
            }
            Component component = Activator.CreateInstance(type) as Component;
            if (component != null)
            {
                component.entity = this;
                this.componentsSet.Add(type.Name, component);
                components.Add(component);
                component.Awake(jd);
            }
            return component;
        }

        public virtual K GetComponent<K>() where K : Component
        {
            return GetComponent(typeof(K).Name) as K;
        }

        public virtual Component GetComponent(string name)
        {
            Component component;
            if (!this.componentsSet.TryGetValue(name, out component))
            {
                return null;
            }
            return this.componentsSet[name];
        }

        public virtual void RemoveComponent<K>() where K : Component
        {
            RemoveComponent(typeof(K).Name);
        }

        public virtual void RemoveComponent(string name)
        {
            Component component;
            if (!this.componentsSet.TryGetValue(name, out component))
            {
                return;
            }
            component.Dispose();
            components.Remove(component);
            this.componentsSet.Remove(name);
        }

        public void SetActive(bool b)
        {
            if(enable==false&&b==true)
            {
                enable = b;
                for (int i = 0; i < components.Count; ++i)
                {
                    components[i].OnEnable();
                }
                for (int i = 0; i < childs.Count; ++i)
                {
                    childs[i].OnEnable();
                }
            }
            else
            if(enable==true&&b==false)
            {
                enable = b;
                for (int i = 0; i < components.Count; ++i)
                {
                    components[i].OnDisable();
                }
                for (int i=0;i< childs.Count;++i)
                {
                    childs[i].OnDisable();
                }
            }
        }

        public virtual void Awake()
        {
            for (int i = 0; i < components.Count; ++i)
            {
                components[i].Awake();
            }
            for (int i = 0; i < childs.Count; ++i)
            {
                childs[i].Awake();
            }
        }

        public virtual void Update()
        {
            for (int i = 0; i < components.Count; ++i)
            {
                if (components[i].bStart == false)
                {
                    components[i].bStart = true;
                    components[i].Start();
                }
                components[i].Update();
            }
            for (int i = 0; i < childs.Count; ++i)
            {
                childs[i].Update();
            }
        }

        private void Dispose()
        {
            for (int i = 0; i < components.Count; ++i)
            {
                components[i].Dispose();
            }
            for (int i = 0; i < childs.Count; ++i)
            {
                childs[i].Dispose();
            }
        }

        protected virtual void OnEnable()
        {
            for (int i = 0; i < components.Count; ++i)
            {
                components[i].OnEnable();
            }
            for (int i = 0; i < childs.Count; ++i)
            {
                childs[i].OnEnable();
            }
        }

        protected virtual void OnDisable()
        {
            for (int i = 0; i < components.Count; ++i)
            {
                components[i].OnDisable();
            }
            for (int i = 0; i < childs.Count; ++i)
            {
                childs[i].OnDisable();
            }
        }

        public K GetComponentInChild<K>() where K : Component
        {
            return GetComponentInChild(typeof(K).Name) as K;
        }

        public virtual Component GetComponentInChild(string name)
        {
            Component component = GetComponent(name);
            if(component==null)
            for (int i = 0; i < childs.Count; i++)
            {
                component = childs[i].GetComponentInChild(name);
                if (component != null)
                    break;
            }
            return component;
        }

        public static void Dispose(Entity entity)
        {
            if(entity!=null)
            {
                entity.parent?.RemoveChild(entity);
                entity.parent = null;
                entity.Dispose();
            }
        }

        public List<Entity> childs = new List<Entity>();
        public Entity parent;
        public void AddChild(Entity tran)
        {
            tran.parent = this;
            childs.Add(tran);
        }
        public void RemoveChild(Entity tran)
        {
            tran.parent = null;
            childs.Remove(tran);
        }
        public Entity GetChild(int idx)
        {
            if (0 <= idx && idx < childs.Count)
                return childs[idx];
            return null;
        }

        public Entity Find(string name)
        {
            Entity t = childs.Find(c => c.Name == name);
            if (t != null)
                return t;

            for (int i = 0; i < childs.Count; i++)
            {
                Entity tran = childs[i].Find(name);
                if (tran != null)
                    return tran;
            }
            return null;
        }


    }
}