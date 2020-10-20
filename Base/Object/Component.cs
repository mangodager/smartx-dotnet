using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using MongoDB.Bson.Serialization.Attributes;

namespace ETModel
{
    [BsonIgnoreExtraElements]
    public class Component : Object, IDisposable
    {
        [BsonIgnore]
        public Entity entity;
        [BsonIgnore]
        public bool enable;

        public virtual void Awake(JToken jd =null)
        {

        }

        [BsonIgnore]
        public bool bStart = false;
        public virtual void Start()
        {

        }

        public virtual void Update()
        {

        }

        [BsonIgnore]
        protected bool IsDispose;
        public virtual void Dispose()
        {
            bool IsDisposeLocal = this.IsDispose;
            this.IsDispose = true;
            if (IsDisposeLocal)
                return;

            OnDestroy();
            entity?.RemoveComponent(this.GetType().Name);
            entity = null;

        }

        protected virtual void OnDestroy()
        {


        }

        public virtual void OnEnable()
        {


        }

        public virtual void OnDisable()
        {


        }

    }

}