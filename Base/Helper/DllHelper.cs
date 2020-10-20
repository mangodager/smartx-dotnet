using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace ETModel
{
	public static class AssemblyHelper
    {
        private static readonly Dictionary<string, Assembly> assemblies = new Dictionary<string, Assembly>();
        public static void AddAssembly(string dllType, Assembly assembly)
        {
            assemblies[dllType] = assembly;
        }
        public static Assembly GetAssembly(string dllType)
        {
            return assemblies[dllType];
        }
        public static List<Type> GetTypes()
        {
            List<Type> list = new List<Type>();
            foreach (string key in assemblies.Keys)
            {
                list.AddRange(assemblies[key].GetTypes());
            }
            return list;
        }
    }
}
