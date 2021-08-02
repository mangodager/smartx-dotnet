using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace ETModel
{
	public static class FileHelper
	{
		public static void GetAllFiles(List<string> files, string dir)
		{
			string[] fls = Directory.GetFiles(dir);
			foreach (string fl in fls)
			{
				files.Add(fl);
			}

			string[] subDirs = Directory.GetDirectories(dir);
			foreach (string subDir in subDirs)
			{
				GetAllFiles(files, subDir);
			}
		}
		
		public static void CleanDirectory(string dir)
		{
			foreach (string subdir in Directory.GetDirectories(dir))
			{
				Directory.Delete(subdir, true);		
			}

			foreach (string subFile in Directory.GetFiles(dir))
			{
				File.Delete(subFile);
			}
		}

		public static void CopyDirectory(string srcDir, string tgtDir)
		{
			DirectoryInfo source = new DirectoryInfo(srcDir);
			DirectoryInfo target = new DirectoryInfo(tgtDir);
	
			if (target.FullName.StartsWith(source.FullName, StringComparison.CurrentCultureIgnoreCase))
			{
				throw new Exception("父目录不能拷贝到子目录！");
			}
	
			if (!source.Exists)
			{
				return;
			}
	
			if (!target.Exists)
			{
				target.Create();
			}
	
			FileInfo[] files = source.GetFiles();
	
			for (int i = 0; i < files.Length; i++)
			{
				File.Copy(files[i].FullName, Path.Combine(target.FullName, files[i].Name), true);
			}
	
			DirectoryInfo[] dirs = source.GetDirectories();
	
			for (int j = 0; j < dirs.Length; j++)
			{
				CopyDirectory(dirs[j].FullName, Path.Combine(target.FullName, dirs[j].Name));
			}
		}

#if RELEASE
		static Dictionary<string, string> GetFileDataCache = new Dictionary<string, string>();
		public static string GetFileData(string file)
		{
			if (GetFileDataCache.TryGetValue(file, out string cache))
			{
				return cache;
			}

			StreamReader streamReader = File.OpenText(file);
			var str = streamReader.ReadToEnd();
			streamReader.Close();
			streamReader.Dispose();

			GetFileDataCache.Remove(file);
			GetFileDataCache.Add(file, str);
			return str;
		}

		static Dictionary<string, byte[]> ReadAllBytesCache = new Dictionary<string, byte[]>();
		public static byte[] ReadAllBytes(string path)
		{
			if (ReadAllBytesCache.TryGetValue(path, out byte[] cache))
			{
				return cache;
			}
			var bytes = File.ReadAllBytes(path);
			ReadAllBytesCache.Remove(path);
			ReadAllBytesCache.Add(path, bytes);
			return bytes;
		}
#else
		public static string GetFileData(string file)
        {
            StreamReader streamReader = File.OpenText(file);
			var str = streamReader.ReadToEnd();
			streamReader.Close();
			streamReader.Dispose();
			return str;

		}

        public static byte[] ReadAllBytes(string path)
        {
			return File.ReadAllBytes(path);
		}
#endif
		public delegate string Action<in T>(T obj);

		public static string HashDirectory(string srcDir, Action<string> Sha256)
		{
			DirectoryInfo source = new DirectoryInfo(srcDir);
			if (!source.Exists)
			{
				return "";
			}
			string hash = "";

			FileInfo[] files = source.GetFiles();
			files = files.OrderBy((x) => x.FullName).ToArray();
			for (int i = 0; i < files.Length; i++)
			{
				if (files[i].Extension == ".lua")
				{
					hash += Sha256(FileHelper.GetFileData(files[i].FullName));
				}
			}

			DirectoryInfo[] dirs = source.GetDirectories();
			dirs = dirs.OrderBy((x) => x.FullName).ToArray();
			for (int j = 0; j < dirs.Length; j++)
			{
				hash += HashDirectory(dirs[j].FullName, Sha256);
			}

			return Sha256(hash);
		}


	}
}
