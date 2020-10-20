using LitJson;
using Newtonsoft.Json;
using System;

namespace ETModel
{
	public static class JsonHelper
	{
        //--------------------------
        //public static string ToJson(object obj)
        //{
        //    return JsonMapper.ToJson(obj);
        //}

        //public static T FromJson<T>(string str)
        //{
        //    return JsonMapper.ToObject<T>(str);
        //}

        //public static object FromJson(Type type, string str)
        //{
        //    return JsonMapper.ToObject(type, str);
        //}

        //public static T Clone<T>(T t)
        //{
        //    return JsonMapper.ToObject<T>(ToJson(t));
        //}

        //--------------------------
        //public static string ToJson(object obj)
        //{
        //    return MongoHelper.ToJson(obj);
        //}

        //public static T FromJson<T>(string str)
        //{
        //    return MongoHelper.FromJson<T>(str);
        //}

        //public static object FromJson(Type type, string str)
        //{
        //    return MongoHelper.FromJson(type, str);
        //}

        //public static T Clone<T>(T t)
        //{
        //    return FromJson<T>(ToJson(t));
        //}

        //--------------------------
        public static string ToJson(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public static T FromJson<T>(string str)
        {
            return JsonConvert.DeserializeObject<T>(str);
        }

        public static object FromJson(Type type, string str)
        {
            return JsonConvert.DeserializeObject(str, type);
        }

        public static T Clone<T>(T t)
        {
            return JsonConvert.DeserializeObject<T>(ToJson(t));
        }

    }
}